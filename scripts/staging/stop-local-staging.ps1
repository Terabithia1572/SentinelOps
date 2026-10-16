# SentinelOps — Local Kubernetes Staging Cluster Teardown Script
# Strictly cleans up the isolated "sentinelops-staging" kind cluster and its resources.
# Guaranteed ZERO impact on Docker Compose containers, named volumes, or host databases.

[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Ensure local tools are in PATH
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$binDir = Join-Path $repoRoot "tools\bin"
$dockerBin = "C:\Program Files\Docker\Docker\resources\bin"
$env:PATH = "$binDir;$dockerBin;" + $env:PATH

$clusterName = "sentinelops-staging"

Write-Host "=========================================================="
Write-Host "   SENTINELOPS — LOCAL KUBERNETES STAGING TEARDOWN        "
Write-Host "=========================================================="
Write-Host "Target Cluster: $clusterName"

if (-not $Force) {
    $confirm = Read-Host "Are you sure you want to delete the '$clusterName' kind cluster? (y/N)"
    if ($confirm -ne 'y' -and $confirm -ne 'Y') {
        Write-Host "Teardown aborted by user." -ForegroundColor Yellow
        exit 0
    }
}

$existingClusters = kind get clusters 2>$null
if ($existingClusters -contains $clusterName) {
    Write-Host "Deleting kind cluster '$clusterName'..."
    kind delete cluster --name $clusterName
    Write-Host "Cluster '$clusterName' deleted cleanly." -ForegroundColor Green
} else {
    Write-Host "Cluster '$clusterName' does not exist. Nothing to clean up." -ForegroundColor Cyan
}

Write-Host "`nVerifying that host Docker Compose containers and databases remain untouched:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
