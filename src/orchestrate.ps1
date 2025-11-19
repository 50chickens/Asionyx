<#
Orchestration script to build solution, docker image and run integration tests.
This script is intended to live in the `src/` folder next to `Asionyx.sln`.
Usage: sudo -E pwsh ./orchestrate.ps1
#>
param()

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir


dotnet restore Asionyx.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet restore failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

dotnet build Asionyx.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Publish projects to a local `publish` folder so the Dockerfile can copy only published outputs.
Write-Host "Publishing projects to ./publish/..." -ForegroundColor Green
$publishRoot = Join-Path $scriptDir 'publish'
if (Test-Path $publishRoot) { Remove-Item $publishRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $publishRoot 'deployment') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $publishRoot 'systemd') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $publishRoot 'helloworld') | Out-Null

dotnet publish Asionyx.Services.Deployment -c Release -o "$publishRoot/deployment"
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet publish deployment failed" -ForegroundColor Red; exit $LASTEXITCODE }

dotnet publish Asionyx.Services.Deployment.SystemD -c Release -o "$publishRoot/systemd"
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet publish systemd failed" -ForegroundColor Red; exit $LASTEXITCODE }

dotnet publish Asionyx.Services.HelloWorld -c Release -o "$publishRoot/helloworld"
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet publish helloworld failed" -ForegroundColor Red; exit $LASTEXITCODE }

# Build docker image (explicitly target Linux platform)
Write-Host "Building docker image asionyx/deployment:local (linux)" -ForegroundColor Green
# Build the final application/integration image (published apps copied into image)
docker build -f Asionyx.Services.Deployment.Docker/Dockerfile -t asionyx/deployment:local .

# Run container
Write-Host "Starting container..." -ForegroundColor Green
# Generate an API key for the test run and export it to the container and test process
$apiKey = [guid]::NewGuid().ToString('N')
Write-Host "Generated API key for integration run" -ForegroundColor Yellow

# stop existing
if (docker ps -a --format '{{.Names}}' | Select-String -Pattern '^asionyx_local$') {
    docker rm -f asionyx_local | Out-Null
}

# Pass the API key into the container environment. Also export locally so dotnet test can read it.
$env:API_KEY = $apiKey
docker run -d --name asionyx_local -p 5000:5000 -e API_KEY=$apiKey -e ASIONYX_INSECURE_TESTING=1 asionyx/deployment:local | Out-Null

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
# Ensure tests have the API key environment variable available
$env:API_KEY = $apiKey
# Run integration tests (client tests expect endpoint at localhost:5000)
dotnet test Asionyx.Services.Deployment.Client.Tests -c Release --no-build
dotnet test Asionyx.Services.Deployment.Tests -c Release --no-build

# Tear down
Write-Host "Stopping container..." -ForegroundColor Green
docker rm -f asionyx_local | Out-Null

Write-Host "Orchestration complete." -ForegroundColor Green
