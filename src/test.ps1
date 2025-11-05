$commands = @(
    "dotnet clean .\AsioAudioRouter.sln",
    "dotnet build .\AsioAudioRouter.sln",
    "dotnet test -no-restore .\AsioAudioRouter.sln",
    "dotnet run .\AsioAudioRouter.sln"
)
##check for errors and if not found do the next part

$commands | ForEach-Object {
    Write-Host "Executing: $_"
    Invoke-Expression $_
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Command failed with exit code $LASTEXITCODE. Stopping execution."
        exit $LASTEXITCODE
    }
}
