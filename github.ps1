Param(
  [Parameter(Mandatory=$true)][string]$BranchName,
  [Parameter(Mandatory=$true)][string]$CommitMessage
)

if ([string]::IsNullOrWhiteSpace($BranchName)) { Write-Error "BranchName is required"; exit 2 }
if ([string]::IsNullOrWhiteSpace($CommitMessage)) { Write-Error "CommitMessage is required"; exit 2 }

# Normalize branch prefix if caller provided a simple name
if ($BranchName -notmatch '^(feature/|decommission/|fix/|chore/|hotfix/|release/)') {
  $Branch = "feature/$BranchName"
} else {
  $Branch = $BranchName
}

# Create or checkout the branch
try {
  git rev-parse --verify $Branch > $null 2>&1
  $exists = ($LASTEXITCODE -eq 0)
} catch {
  $exists = $false
}

if ($exists) {
  git checkout $Branch
} else {
  git checkout -b $Branch
}

# Stage, commit and push
git add -A

# Commit (will return non-zero if there are no staged changes)
git commit -m $CommitMessage
$commitExit = $LASTEXITCODE
if ($commitExit -ne 0) {
  Write-Output "git commit returned exit code $commitExit (no changes committed or an error occurred)."
}

git push -u origin $Branch
exit $LASTEXITCODE