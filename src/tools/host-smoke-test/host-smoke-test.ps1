<#
Host smoke-test script for a real target host.

This script performs a minimal end-to-end check on a real host:
- verifies SSH access with a private key
- uploads a published deployment folder (zips it first)
- extracts the publish on the remote host into a target directory (default /opt/...)
- optionally installs a systemd unit file and starts the service
- checks service status and queries the /status endpoint via remote curl

Usage (example):
pwsh .\host-smoke-test.ps1 -Host myhost -User ubuntu -KeyPath C:\keys\id_rsa -PublishDir C:\projects\Asionyx\publish\deployment -RemoteDir /opt/Asionyx.Service.Deployment.Linux -ServiceName deployment-service -ApiPort 5001 -ApiKey the-api-key
#>

param(
    [Parameter(Mandatory=$true)] [string] $Host,
    [Parameter(Mandatory=$true)] [string] $PublishDir,
    [string] $RemoteDir = "/opt/Asionyx.Service.Deployment.Linux",
    [string] $ServiceName = "deployment-service",
    [int] $ApiPort = 5001,
    [string] $ApiKey = $null,
    [switch] $InstallUnitFile, # if set, looks for a unit file in PublishDir and installs it
    [string] $UnitFileLocalPath = $null
)

function Exit-With($code, $msg) 
{
    if ($msg) { Write-Host $msg -ForegroundColor Red }
    exit $code
}

function New-DeploymentContent {
    param(
        [string] $ScriptDir,
        [string] $Host,
        [string] $PublishDir,
        [string] $RemoteDir,
        [string] $ServiceName,
        [int] $ApiPort,
        [string] $ApiKey,
        [switch] $InstallUnitFile,
        [string] $UnitFileLocalPath
    )

    return [PSCustomObject]@{
        ScriptDir = $ScriptDir
        Host = $Host
        PublishDir = $PublishDir
        RemoteDir = $RemoteDir
        ServiceName = $ServiceName
        ApiPort = $ApiPort
        ApiKey = $ApiKey
        InstallUnitFile = $InstallUnitFile.IsPresent
        UnitFileLocalPath = $UnitFileLocalPath
        ZipLocal = $null
        RemoteTmp = $null
    }
}

function Ensure-ToolsAvailable {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    if (-not (Get-Command ssh -ErrorAction SilentlyContinue)) { Exit-With 3 "ssh is not available in PATH" }
    if (-not (Get-Command scp -ErrorAction SilentlyContinue)) { Exit-With 3 "scp is not available in PATH" }
}

function Test-SshConnectivity {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    # Runs: ssh <host> 'ls -l' and returns $true if it succeeds (exit code 0)
    $host = $deploymentContent.Host
    Write-Host "Testing SSH connectivity (ssh $host 'ls -l')..."
    $out = & ssh $host 'ls -l' 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SSH preflight OK" -ForegroundColor Green
        return $true
    } else {
        Write-Host "SSH preflight failed. Output:" -ForegroundColor Yellow
        Write-Host $out
        return $false
    }
}

function Prepare-Zip {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    $guid = [guid]::NewGuid().ToString('N')
    $zipLocal = Join-Path $env:TEMP "asionyx_publish_$guid.zip"
    Write-Host "Creating zip $zipLocal from $($deploymentContent.PublishDir)"
    try {
        if (Test-Path $zipLocal) { Remove-Item $zipLocal -Force }
        Compress-Archive -Path (Join-Path $deploymentContent.PublishDir '*') -DestinationPath $zipLocal -Force
        $deploymentContent.ZipLocal = $zipLocal
        $deploymentContent.RemoteTmp = "/tmp/" + (Split-Path $zipLocal -Leaf)
    } catch {
        Exit-With 5 "Failed to create zip: $_"
    }
}

function Upload-Zip {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    Write-Host "Uploading zip to $($deploymentContent.Host):$($deploymentContent.RemoteTmp)"
    & scp $deploymentContent.ZipLocal "$($deploymentContent.Host):$($deploymentContent.RemoteTmp)"
    if ($LASTEXITCODE -ne 0) { Exit-With 6 "scp upload failed with exit code $LASTEXITCODE" }
}

function Remote-Extract {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    Write-Host "Extracting on remote host to $($deploymentContent.RemoteDir)"
    $host = $deploymentContent.Host
    $remoteTmp = $deploymentContent.RemoteTmp
    $remoteDir = $deploymentContent.RemoteDir
    $remoteCmd = 'sudo mkdir -p ' + $remoteDir + ' && sudo unzip -o ' + $remoteTmp + ' -d ' + $remoteDir + ' && sudo chown -R $(whoami):$(whoami) ' + $remoteDir + ' && sudo chmod -R 755 ' + $remoteDir
    & ssh $host $remoteCmd
    if ($LASTEXITCODE -ne 0) { Exit-With 7 "Remote extract failed (exit $LASTEXITCODE)" }
}

function Install-UnitFile {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    if (-not $deploymentContent.InstallUnitFile) { return }
    $unitSource = $deploymentContent.UnitFileLocalPath
    if (-not $unitSource) {
        $svc = Get-ChildItem -Path $deploymentContent.PublishDir -Filter '*.service' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($svc) { $unitSource = $svc.FullName }
    }
    if ($unitSource -and (Test-Path $unitSource)) {
        Write-Host "Installing unit file $unitSource to /etc/systemd/system/$($deploymentContent.ServiceName).service"
        & scp $unitSource "$($deploymentContent.Host):/tmp/$($deploymentContent.ServiceName).service"
        if ($LASTEXITCODE -ne 0) { Exit-With 8 "scp unit file upload failed" }
        $installCmd = 'sudo mv /tmp/' + $deploymentContent.ServiceName + '.service /etc/systemd/system/' + $deploymentContent.ServiceName + '.service && sudo systemctl daemon-reload && sudo systemctl enable --now ' + $deploymentContent.ServiceName
        & ssh $deploymentContent.Host $installCmd
        if ($LASTEXITCODE -ne 0) { Exit-With 9 "Failed to install/start unit $($deploymentContent.ServiceName)" }
    } else {
        Write-Host "No unit file provided or found in publish dir; skipping unit install." -ForegroundColor Yellow
    }
}

function Check-ServiceStatus {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    Write-Host "Checking service status for $($deploymentContent.ServiceName)"
    & ssh $deploymentContent.Host "sudo systemctl is-active $($deploymentContent.ServiceName) || true"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "systemctl returned non-zero exit code; continue to API check to gather info" -ForegroundColor Yellow
    }
}

function Query-RemoteStatus {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    Write-Host "Querying /status endpoint on remote host"
    if ($deploymentContent.ApiKey) {
        $curlCmd = "curl -sS -H 'X-API-KEY: $($deploymentContent.ApiKey)' http://localhost:$($deploymentContent.ApiPort)/status || true"
    } else {
        $curlCmd = "curl -sS http://localhost:$($deploymentContent.ApiPort)/status || true"
    }
    $curlRes = & ssh $deploymentContent.Host $curlCmd
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($curlRes)) {
        Write-Host "Remote curl returned empty or failed. Trying pwsh Invoke-RestMethod remotely..." -ForegroundColor Yellow
        $pwcmd = "pwsh -NoProfile -NonInteractive -Command \"try { Invoke-RestMethod -Uri 'http://localhost:$($deploymentContent.ApiPort)/status' -Headers @{'X-API-KEY'='$($deploymentContent.ApiKey)'} -ErrorAction Stop | ConvertTo-Json -Compress } catch { Write-Output 'invoke-failed' ; exit 4 }\""
        $pwOut = & ssh $deploymentContent.Host $pwcmd
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($pwOut)) {
            Write-Host "Remote status query failed or empty. Last output:" -ForegroundColor Red
            Write-Host $pwOut
            Exit-With 10 "Failed to query /status on remote host"
        } else {
            Write-Host "Remote /status response:" -ForegroundColor Green
            Write-Host $pwOut
        }
    } else {
        Write-Host "Remote /status response:" -ForegroundColor Green
        Write-Host $curlRes
    }
}

function Run-SmokeTest {
    param(
        [Parameter(Mandatory=$true)] $deploymentContent
    )
    Ensure-ToolsAvailable -deploymentContent $deploymentContent
    $ok = Test-SshConnectivity -deploymentContent $deploymentContent
    if (-not $ok) { Exit-With 4 "SSH preflight check failed" }
    Prepare-Zip -deploymentContent $deploymentContent
    Upload-Zip -deploymentContent $deploymentContent
    Remote-Extract -deploymentContent $deploymentContent
    Install-UnitFile -deploymentContent $deploymentContent
    Check-ServiceStatus -deploymentContent $deploymentContent
    Query-RemoteStatus -deploymentContent $deploymentContent
    Write-Host "Smoke test completed successfully." -ForegroundColor Green
}

# Build deploymentContent object and run
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deploymentContent = New-DeploymentContent -ScriptDir $scriptDir -Host $Host -PublishDir $PublishDir -RemoteDir $RemoteDir -ServiceName $ServiceName -ApiPort $ApiPort -ApiKey $ApiKey -InstallUnitFile:$InstallUnitFile -UnitFileLocalPath $UnitFileLocalPath
Run-SmokeTest -deploymentContent $deploymentContent
