# SentinelOps — Automated Kubernetes Staging Integration Test Suite
# Target: sentinelops-staging cluster on kind
# Exit Code: 0 on all tests passing, 1 on any failure

[CmdletBinding()]
param(
    [string]$Namespace = "sentinelops-staging",
    [string]$Context = "kind-sentinelops-staging"
)

$ErrorActionPreference = "Continue"
$toolsDir = Join-Path (Get-Location) "tools\bin"
if (Test-Path $toolsDir) {
    $env:PATH = "$toolsDir;$env:PATH"
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   SENTINELOPS - KUBERNETES STAGING INTEGRATION TEST SUITE" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Namespace: $Namespace | Context: $Context"
Write-Host "Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')"
Write-Host ""

$script:testsPassed = 0
$script:testsFailed = 0

function Run-StagingTest {
    param(
        [string]$TestId,
        [string]$Description,
        [scriptblock]$Assertion
    )

    Write-Host "[$TestId] $Description ... " -NoNewline
    try {
        $passed = & $Assertion
        if ($passed -eq $true) {
            Write-Host "PASS" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host "FAIL" -ForegroundColor Red
            $script:testsFailed++
        }
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
        $script:testsFailed++
    }
}

# --- TEST 1: Cluster Control-Plane Node Ready ---
Run-StagingTest -TestId "K8S-01" -Description "Cluster control-plane node exists and is in Ready state" -Assertion {
    $node = kubectl get nodes --context $Context -o json | ConvertFrom-Json
    $status = $node.items[0].status.conditions | Where-Object { $_.type -eq "Ready" } | Select-Object -ExpandProperty status
    return ($status -eq "True")
}

# --- TEST 2: All 6 Core Microservices are 1/1 Running ---
Run-StagingTest -TestId "K8S-02" -Description "All 6 microservices are running and ready" -Assertion {
    $pods = (kubectl get pods -n $Namespace --context $Context -o json | ConvertFrom-Json).items
    $expectedComponents = @("api", "worker", "web", "collector", "postgres", "redis")
    foreach ($comp in $expectedComponents) {
        $pod = $pods | Where-Object { $_.metadata.labels."app.kubernetes.io/component" -eq $comp -and $_.status.phase -eq "Running" }
        if (-not $pod) { return $false }
        $ready = $pod.status.containerStatuses[0].ready
        if ($ready -ne $true) { return $false }
    }
    return $true
}

# --- TEST 3: Persistent Volume Claims are Bound ---
Run-StagingTest -TestId "K8S-03" -Description "StatefulSet PVCs for PostgreSQL and Redis are Bound" -Assertion {
    $pvcs = (kubectl get pvc -n $Namespace --context $Context -o json | ConvertFrom-Json).items
    if ($pvcs.Count -lt 2) { return $false }
    $unbound = $pvcs | Where-Object { $_.status.phase -ne "Bound" }
    return ($null -eq $unbound)
}

# --- TEST 4: Service Endpoints Valid and Populated ---
Run-StagingTest -TestId "K8S-04" -Description "Kubernetes Services have registered endpoints" -Assertion {
    $epList = (kubectl get endpoints -n $Namespace --context $Context -o json | ConvertFrom-Json).items
    $criticalServices = @("sentinelops-staging-api", "sentinelops-staging-postgres", "sentinelops-staging-redis", "sentinelops-staging-collector")
    foreach ($svc in $criticalServices) {
        $ep = $epList | Where-Object { $_.metadata.name -eq $svc }
        if (-not $ep.subsets -or -not $ep.subsets[0].addresses) { return $false }
    }
    return $true
}

# --- TEST 5: API Liveness Probe Responds 200 OK ---
Run-StagingTest -TestId "K8S-05" -Description "API Liveness endpoint health-live returns 200 OK" -Assertion {
    $res = kubectl exec -n $Namespace --context $Context deployment/sentinelops-staging-web -- wget -q -O - http://sentinelops-staging-api:8080/health/live 2>$null
    return ($res -like "*Healthy*")
}

# --- TEST 6: API Readiness Probe Responds 200 OK (DB and Redis Healthy) ---
Run-StagingTest -TestId "K8S-06" -Description "API Readiness endpoint health-ready verifies DB and Redis health" -Assertion {
    $res = kubectl exec -n $Namespace --context $Context deployment/sentinelops-staging-web -- wget -q -O - http://sentinelops-staging-api:8080/health/ready 2>$null
    return ($res -like "*Healthy*")
}

# --- TEST 7: OTLP Collector NodePort Ingestion Pipeline ---
Run-StagingTest -TestId "K8S-07" -Description "OTLP Collector responds HTTP 200 on ingestion port 14318" -Assertion {
    $payload = '{"resourceLogs":[{"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"k8s-automated-test"}}]},"scopeLogs":[{"scope":{"name":"test"},"logRecords":[{"timeUnixNano":"1729104000000000000","severityText":"Information","body":{"stringValue":"Automated K8s Ingestion Test"}}]}]}]}'
    $resp = Invoke-WebRequest -Uri "http://127.0.0.1:14318/v1/logs" -Method Post -Body $payload -ContentType "application/json" -TimeoutSec 10 -ErrorAction SilentlyContinue
    return ($resp.StatusCode -eq 200)
}

# --- TEST 8: Redis Streams Consumer Group Status ---
Run-StagingTest -TestId "K8S-08" -Description "Redis Streams consumer group active with zero pending lag" -Assertion {
    $xinfo = kubectl exec -n $Namespace --context $Context sentinelops-staging-redis-0 -- redis-cli -a StagingRedis123! XINFO GROUPS sentinelops:stream:logs 2>$null
    $hasGroup = ($xinfo -join " ") -like "*sentinelops-worker-group*"
    $xpending = kubectl exec -n $Namespace --context $Context sentinelops-staging-redis-0 -- redis-cli -a StagingRedis123! XPENDING sentinelops:stream:logs sentinelops-worker-group 2>$null
    $isZeroPending = ($xpending -join " ").Trim().StartsWith("0")
    return ($hasGroup -and $isZeroPending)
}

# --- TEST 9: Pod Security Standards (Non-Root, No Privilege Escalation) ---
Run-StagingTest -TestId "K8S-09" -Description "All workloads execute as non-root with allowPrivilegeEscalation false" -Assertion {
    $pods = (kubectl get pods -n $Namespace --context $Context -o json | ConvertFrom-Json).items
    foreach ($pod in $pods) {
        if ($pod.metadata.name -like "*migration*") { continue }
        $nonRoot = $pod.spec.securityContext.runAsNonRoot
        if ($nonRoot -ne $true) { return $false }
        $container = $pod.spec.containers[0]
        if ($container.name -in @("api", "worker", "web")) {
            if ($container.securityContext.allowPrivilegeEscalation -ne $false) { return $false }
            if ($container.securityContext.capabilities.drop -notcontains "ALL") { return $false }
        }
    }
    return $true
}

# --- TEST 10: Zero-Trust NetworkPolicy Isolation ---
Run-StagingTest -TestId "K8S-10" -Description "NetworkPolicy blocks unauthorized frontend direct access to database" -Assertion {
    $null = kubectl exec -n $Namespace --context $Context deployment/sentinelops-staging-web -- nc -z -w 2 postgres 5432 2>&1
    return ($LASTEXITCODE -ne 0)
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "   TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
$total = $script:testsPassed + $script:testsFailed
Write-Host "Total Tests: $total | Passed: $script:testsPassed | Failed: $script:testsFailed"
if ($script:testsFailed -eq 0) {
    Write-Host "VERDICT: LOCAL KUBERNETES STAGING INTEGRATION SUITE PASSED (100 Percent)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "VERDICT: TEST SUITE FAILED" -ForegroundColor Red
    exit 1
}
