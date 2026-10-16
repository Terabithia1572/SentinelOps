# SentinelOps — Local Kubernetes Staging Cluster Startup Script
# Targets strictly the isolated "sentinelops-staging" kind cluster and namespace.
# Preserves all existing host containers and Docker Compose environments.

[CmdletBinding()]
param(
    [switch]$SkipImageLoad
)

$ErrorActionPreference = "Stop"

# Ensure local tools are in PATH
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$binDir = Join-Path $repoRoot "tools\bin"
$dockerBin = "C:\Program Files\Docker\Docker\resources\bin"
$env:PATH = "$binDir;$dockerBin;" + $env:PATH

Write-Host "=========================================================="
Write-Host "   SENTINELOPS — LOCAL KUBERNETES STAGING STARTUP         "
Write-Host "=========================================================="

# 1. Verify Docker Engine
Write-Host "[1/5] Verifying Docker Engine connection..."
try {
    $null = docker version
    Write-Host "      Docker Engine is operational." -ForegroundColor Green
} catch {
    Write-Error "Docker Engine is offline. Start Docker Desktop and re-run."
}

# 2. Check kind cluster
$clusterName = "sentinelops-staging"
Write-Host "`n[2/5] Checking kind cluster '$clusterName'..."
$existingClusters = kind get clusters 2>$null
if ($existingClusters -contains $clusterName) {
    Write-Host "      Cluster '$clusterName' already exists. Reusing existing cluster." -ForegroundColor Cyan
} else {
    Write-Host "      Provisioning isolated kind cluster from deploy/kind/sentinelops-staging.yaml..."
    $configPath = Join-Path $repoRoot "deploy\kind\sentinelops-staging.yaml"
    kind create cluster --config $configPath
    Write-Host "      Cluster '$clusterName' provisioned successfully." -ForegroundColor Green
}

# 3. Verify context & create namespace
Write-Host "`n[3/5] Setting kubectl context and namespace..."
kubectl config use-context "kind-$clusterName"
$nsYaml = @"
apiVersion: v1
kind: Namespace
metadata:
  name: sentinelops-staging
  labels:
    environment: staging
    app.kubernetes.io/managed-by: sentinelops
"@
$nsYaml | kubectl apply -f -
Write-Host "      Namespace 'sentinelops-staging' ensured." -ForegroundColor Green

# 4. Build and load staging images
if (-not $SkipImageLoad) {
    Write-Host "`n[4/5] Building and loading deterministic staging container images..."
    Push-Location $repoRoot
    try {
        docker build -f deploy/docker/Dockerfile.api -t "sentinelops-api:1.0.0-staging" .
        docker build -f deploy/docker/Dockerfile.worker -t "sentinelops-worker:1.0.0-staging" .
        docker build -f deploy/docker/Dockerfile.web -t "sentinelops-web:1.0.0-staging" .

        Write-Host "      Loading images into kind cluster '$clusterName'..."
        kind load docker-image "sentinelops-api:1.0.0-staging" --name $clusterName
        kind load docker-image "sentinelops-worker:1.0.0-staging" --name $clusterName
        kind load docker-image "sentinelops-web:1.0.0-staging" --name $clusterName
        Write-Host "      Images loaded successfully into cluster nodes." -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host "`n[4/5] Skipping image build/load as requested." -ForegroundColor Yellow
}

# 5. Summary
Write-Host "`n[5/5] Staging cluster status:"
kubectl get nodes -o wide
Write-Host "`n=========================================================="
Write-Host "   STAGING CLUSTER READY: kind-$clusterName" -ForegroundColor Green
Write-Host "   Namespace: sentinelops-staging"
Write-Host "   Ingress HTTP Port: 8085 | HTTPS: 8443"
Write-Host "=========================================================="
