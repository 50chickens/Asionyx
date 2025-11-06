$commands = @(
    "dotnet clean .\Asionyx.sln",
    "dotnet build .\Asionyx.sln",
    "dotnet test .\Asionyx.sln --filter 'Category!=Integration' --verbosity normal --no-restore"
)

if ((pwd).Path -notlike "*\src") {
    cd .\src
}
## Run each command and stop on first failure
$commands | ForEach-Object {
    Write-Host "Executing: $_"
    Invoke-Expression $_
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Command failed with exit code $LASTEXITCODE. Stopping execution."
        exit $LASTEXITCODE
    }
}

Write-Host "Build script finished successfully."
