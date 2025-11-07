Param(
  [Parameter(Mandatory=$true)][string]$BranchName,
  [Parameter(Mandatory=$true)][string]$CommitMessage
)

if ([string]::IsNullOrWhiteSpace($BranchName)) { Write-Error "BranchName is required"; exit 2 }
if ([string]::IsNullOrWhiteSpace($CommitMessage)) { Write-Error "CommitMessage is required"; exit 2 }

# Enforce branch naming convention:
# - Caller must pass only the branch description (no slashes), e.g. "cleanup"
# - Script will prepend the "feature/" prefix producing "feature/cleanup"
if ($BranchName -match '/') {
  Write-Error "BranchName must not contain '/'. Pass only the branch description (e.g. 'cleanup' to create 'feature/cleanup')."
  exit 2
}

$Branch = "feature/$BranchName"

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
$pushExit = $LASTEXITCODE

# Verify GitHub Actions workflow run for this branch (single-shot check)
# This uses the local authenticated `gh` CLI; it does not poll repeatedly to avoid long-running operations.
try {
  $json = gh run list --branch $Branch --workflow build-test.yml --limit 5 --json workflowName,displayTitle,headBranch,status,conclusion,createdAt,htmlUrl 2>$null
} catch {
  $json = $null
}

if ($json) {
  $runs = $json | ConvertFrom-Json
  if ($runs -and $runs.Count -gt 0) {
    $runs | Format-Table workflowName,displayTitle,headBranch,status,conclusion,createdAt -AutoSize
  } else {
    Write-Output "No workflow runs found for branch $Branch"
  }
} else {
  Write-Output "gh command failed or returned no data. Ensure 'gh' is installed and authenticated and you have network access."
}

exit $pushExit