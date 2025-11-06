# Robust build script that exits non-zero on failure and validates build/tests automatically.
# Run from repository root: .\src\build.ps1

# Fail fast on unhandled errors
$ErrorActionPreference = 'Stop'

# Determine script directory and solution path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrEmpty($scriptDir)) { $scriptDir = (Get-Location).ProviderPath }
$solutionPath = Join-Path $scriptDir 'Asionyx.sln'

# If the script was invoked from repository root using ./src/build.ps1, ensure we run commands in the src folder
if ((Get-Location).ProviderPath -notlike "*\src") {
    Push-Location $scriptDir
}

# Commands to run (use solutionPath to be explicit)
$commands = @(
    "& dotnet clean `"$solutionPath`"",
    "& dotnet build `"$solutionPath`" --no-restore --verbosity minimal",
    "& dotnet test `"$solutionPath`" --filter 'Category!=Integration' --verbosity normal --no-restore"
)

# Execute commands sequentially and stop on first failure
foreach ($cmd in $commands) {
    Write-Host "Executing: $cmd"
    try {
        Invoke-Expression $cmd
    }
    catch {
        $code = $LASTEXITCODE
        if (-not $code) { $code = 1 }
        Write-Host "Command failed with exit code $code. Stopping execution."
        if ($scriptDir -ne (Get-Location).ProviderPath) { Pop-Location } else { Try { Pop-Location } Catch { } }
        exit $code
    }
}

# Restore location and exit successfully
if ($scriptDir -ne (Get-Location).ProviderPath) { Pop-Location } else { Try { Pop-Location } Catch { } }
Write-Host "Build script finished successfully."
exit 0
