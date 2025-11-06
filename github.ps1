Param(
  [Parameter(Mandatory=$true)][string]$Branch,
  [Parameter(Mandatory=$true)][string]$CommitMessage
)

if ([string]::IsNullOrWhiteSpace($Branch)) { throw "Branch parameter is required and cannot be empty." }
if ([string]::IsNullOrWhiteSpace($CommitMessage)) { throw "CommitMessage parameter is required and cannot be empty." }

Write-Host "Preparing to commit and push changes on branch $Branch"

# Show whether there are changes to commit
$changes = git status --porcelain
if (-not $changes) {
    Write-Host "No changes to commit."
    exit 0
}

# Create or switch to branch, stage, commit and push
git checkout -B $Branch
git add -A

$commitOutput = git commit -m "$CommitMessage" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "git commit returned exit code $LASTEXITCODE. Output:"
    Write-Host $commitOutput
} else {
    Write-Host "Committed changes."
}

Write-Host "Pushing branch $Branch to origin..."
$pushOutput = git push -u origin $Branch 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "git push failed with exit code $LASTEXITCODE. Output:"
    Write-Host $pushOutput
    exit $LASTEXITCODE
}

Write-Host "Push succeeded."
exit 0