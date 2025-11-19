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
# Create host diagnostics directory and bind-mount it into container so diagnostics and logs survive container removal
$diagHost = Join-Path $scriptDir 'artifacts' 'diagnostics_host'
if (-not (Test-Path $diagHost)) { New-Item -ItemType Directory -Path $diagHost -Force | Out-Null }

# Run container with diagnostics bound into /var/asionyx/diagnostics and enable stdout diagnostics
$env:ASIONYX_DIAG_TO_STDOUT = "1"
Write-Host "Starting container with diagnostics bind mount -> $diagHost" -ForegroundColor Yellow
docker run -d --name asionyx_local -p 5000:5000 -e API_KEY=$apiKey -e ASIONYX_INSECURE_TESTING=1 -e ASIONYX_DIAG_TO_STDOUT=1 -v "${diagHost}:/var/asionyx/diagnostics" asionyx/deployment:local | Out-Null

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
# Attempt to copy diagnostics out of the running container so failures can be inspected.
$diagHost = Join-Path $scriptDir 'artifacts' 'diagnostics_' + (Get-Date -Format 'yyyyMMddHHmmss')
if (-not (Test-Path $diagHost)) { New-Item -ItemType Directory -Path $diagHost | Out-Null }
Write-Host "Attempting to copy diagnostics from container to $diagHost ..." -ForegroundColor Yellow
try {
    docker cp asionyx_local:/var/asionyx/diagnostics $diagHost 2>$null
    Write-Host "Diagnostics copied to $diagHost" -ForegroundColor Green
} catch {
    Write-Host "No diagnostics found or docker cp failed" -ForegroundColor Yellow
}
try {
    # Tear down container
    Write-Host "Stopping container..." -ForegroundColor Green
    docker rm -f asionyx_local | Out-Null

    # Remove the local image created for the test run to keep CI/host clean
    Write-Host "Removing local docker image asionyx/deployment:local..." -ForegroundColor Green
    docker rmi -f asionyx/deployment:local | Out-Null

    # Clean up publish artifacts
    if (Test-Path $publishRoot) {
        Write-Host "Cleaning up publish folder $publishRoot" -ForegroundColor Green
        Remove-Item $publishRoot -Recurse -Force
    }

    Write-Host "Orchestration complete." -ForegroundColor Green
}
catch {
    Write-Host "Cleanup failed: $_" -ForegroundColor Yellow
    exit 0
}
