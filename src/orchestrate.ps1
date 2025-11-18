<#
Orchestration script to build solution, docker image and run integration tests.
This script is intended to live in the `src/` folder next to `Asionyx.sln`.
Usage: sudo -E pwsh ./orchestrate.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "Restoring and building solution..." -ForegroundColor Green
if (-not (Test-Path Asionyx.sln)) {
    dotnet new sln -n Asionyx | Out-Null
}
# Add projects to solution (idempotent)
Get-ChildItem -Path . -Filter *.csproj -Recurse | ForEach-Object { dotnet sln Asionyx.sln add $_.FullName | Out-Null }

dotnet restore Asionyx.sln
dotnet build Asionyx.sln -c Release

# Build docker image (explicitly target Linux platform)
Write-Host "Building docker image asionyx/deployment:local (linux)" -ForegroundColor Green
# Build the base runtime image stage and tag it
docker build --platform linux/amd64 -f Asionyx.Services.Deployment.Docker/Dockerfile --target base -t asionyx/base:local .

# Build the final application/integration image (published apps copied into image)
docker build --platform linux/amd64 -f Asionyx.Services.Deployment.Docker/Dockerfile -t asionyx/deployment:local .

# Run container
Write-Host "Starting container..." -ForegroundColor Green
# stop existing
if (docker ps -a --format '{{.Names}}' | Select-String -Pattern '^asionyx_local$') {
    docker rm -f asionyx_local | Out-Null
}
docker run --platform linux/amd64 -d --name asionyx_local -p 5000:5000 asionyx/deployment:local | Out-Null

# wait for readiness
$max = 60
$ok = $false
for ($i=0; $i -lt $max; $i++) {
    try {
        $r = Invoke-WebRequest -UseBasicParsing -Uri http://localhost:5000/info -TimeoutSec 2
        if ($r.StatusCode -eq 200) { $ok = $true; break }
    } catch { Start-Sleep -Seconds 1 }
}
if (-not $ok) { Write-Host "Container did not become ready" -ForegroundColor Red; docker logs asionyx_local; exit 2 }

Write-Host "Container ready, running tests..." -ForegroundColor Green
# Run integration tests (client tests expect endpoint at localhost:5000)
dotnet test Asionyx.Services.Deployment.Client.Tests -c Release --no-build

# Tear down
Write-Host "Stopping container..." -ForegroundColor Green
docker rm -f asionyx_local | Out-Null

Write-Host "Orchestration complete." -ForegroundColor Green
