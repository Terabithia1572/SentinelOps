Write-Host "========================================================="
Write-Host " SENTINELOPS RESILIENCE & RECOVERY REGRESSION TEST       "
Write-Host "========================================================="

$apiKey = "sent_live_collector_internal_key"
$endpoint = "http://localhost:5100"
$collectorEndpoint = "http://localhost:4318"

# 1. Baseline DB counts
$baselineSql = 'SELECT (SELECT count(*) FROM "LogEntries") as logs, (SELECT count(*) FROM "TraceRecords") as traces, (SELECT count(*) FROM "MetricRecords") as metrics;'
$preCounts = $baselineSql | docker exec -i sentinelops-postgres psql -U sentinelops -d sentinelops -t
$preLogCount = [int]($preCounts.Split('|')[0].Trim())
$preTraceCount = [int]($preCounts.Split('|')[1].Trim())
Write-Host "Pre-test PostgreSQL Counts: Logs=$preLogCount, Traces=$preTraceCount"

# 2. Baseline Container Stats
Write-Host "`nContainer Resource Baseline:"
docker stats --no-stream sentinelops-api sentinelops-worker sentinelops-redis sentinelops-postgres

# 3. Controlled Batch Workload with In-Flight Worker Restart
$totalLogs = 150
$latencies = New-Object System.Collections.Generic.List[double]
$success = 0
$failed = 0
$restarted = $false

Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$client = New-Object System.Net.Http.HttpClient($handler)
$client.DefaultRequestHeaders.Add("X-API-Key", $apiKey)

Write-Host "`nStarting ingestion of $totalLogs log events with in-flight worker restart..."
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 1; $i -le $totalLogs; $i++) {
    $nowNano = ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000000).ToString()
    $traceId = [Guid]::NewGuid().ToString("N")
    $spanId = [Guid]::NewGuid().ToString("N").Substring(0, 16)
    
    $payloadObj = @{
        resourceLogs = @(
            @{
                resource = @{
                    attributes = @(
                        @{ key = "service.name"; value = @{ stringValue = "ResilienceVerification" } },
                        @{ key = "deployment.environment"; value = @{ stringValue = "Staging" } }
                    )
                }
                scopeLogs = @(
                    @{
                        scope = @{ name = "ResilienceScope"; version = "1.0.0" }
                        logRecords = @(
                            @{
                                timeUnixNano = $nowNano
                                severityNumber = 9
                                severityText = "Information"
                                body = @{ stringValue = ("Resilience log entry " + $i) }
                                traceId = $traceId
                                spanId = $spanId
                                attributes = @(
                                    @{ key = "iteration"; value = @{ intValue = $i } },
                                    @{ key = "phase"; value = @{ stringValue = "15-resilience" } }
                                )
                            }
                        )
                    }
                )
            }
        )
    }

    $json = $payloadObj | ConvertTo-Json -Depth 10
    $content = New-Object System.Net.Http.StringContent($json, [System.Text.Encoding]::UTF8, "application/json")

    # Halfway through (at iteration 50), restart sentinelops-worker
    if ($i -eq 50 -and -not $restarted) {
        Write-Host "--> [FAULT INJECTION] Restarting sentinelops-worker container while traffic is active..."
        $restartJob = Start-Job -ScriptBlock { docker restart sentinelops-worker }
        $restarted = $true
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $resp = $client.PostAsync(($endpoint + "/v1/logs"), $content).GetAwaiter().GetResult()
    $sw.Stop()
    $latencies.Add($sw.Elapsed.TotalMilliseconds)

    if ($resp.IsSuccessStatusCode) {
        $success++
    } else {
        $failed++
    }
}

$swTotal.Stop()
Write-Host "Ingestion complete in $([Math]::Round($swTotal.Elapsed.TotalSeconds, 2))s. Waiting for worker restart to settle..."

if ($restartJob) {
    Wait-Job $restartJob | Out-Null
    Receive-Job $restartJob | Out-Null
    Remove-Job $restartJob
    Write-Host "--> Worker restart command completed."
}

# Allow up to 10 seconds for worker to process queued Redis stream entries
Write-Host "Monitoring Redis Stream drain..."
$drainWait = 0
while ($drainWait -lt 15) {
    Start-Sleep -Seconds 1
    $drainWait++
    $xinfo = docker exec sentinelops-redis redis-cli XINFO GROUPS sentinelops:stream:logs
    # Check pending
    $pending = (docker exec sentinelops-redis redis-cli XPENDING sentinelops:stream:logs sentinelops-worker-group)
    if ($pending -match "0 0 0" -or $pending -eq "0") {
        Write-Host "--> Redis stream consumer group fully drained at ${drainWait}s."
        break
    }
}

# 4. Final Verification
$postCounts = $baselineSql | docker exec -i sentinelops-postgres psql -U sentinelops -d sentinelops -t
$postLogCount = [int]($postCounts.Split('|')[0].Trim())
$newLogs = $postLogCount - $preLogCount

Write-Host "`nPost-test PostgreSQL Counts: Logs=$postLogCount (Delta: +$newLogs / Expected: +$totalLogs)"

# 5. Percentiles
$sorted = $latencies | Sort-Object
$min = $sorted[0]
$p50 = $sorted[[Math]::Floor($sorted.Count * 0.50)]
$p95 = $sorted[[Math]::Floor($sorted.Count * 0.95)]
$p99 = $sorted[[Math]::Floor($sorted.Count * 0.99)]
$max = $sorted[$sorted.Count - 1]
$avg = ($latencies | Measure-Object -Average).Average
$rps = [Math]::Round($totalLogs / $swTotal.Elapsed.TotalSeconds, 1)

Write-Host "`n========================================================="
Write-Host "                 RESILIENCE REPORT                       "
Write-Host "========================================================="
Write-Host "Total Workload:       $totalLogs requests"
Write-Host "Successful Ingest:    $success"
Write-Host "Failed Ingest:        $failed"
Write-Host "Throughput:           $rps req/s"
Write-Host "Latency Min:          $([Math]::Round($min, 2)) ms"
Write-Host "Latency Mean:         $([Math]::Round($avg, 2)) ms"
Write-Host "Latency P50:          $([Math]::Round($p50, 2)) ms"
Write-Host "Latency P95:          $([Math]::Round($p95, 2)) ms"
Write-Host "Latency P99:          $([Math]::Round($p99, 2)) ms"
Write-Host "Latency Max:          $([Math]::Round($max, 2)) ms"
Write-Host "Data Loss:            $($totalLogs - $newLogs) records"
Write-Host "Zero-Loss Verified:   $($newLogs -eq $totalLogs)"
Write-Host "========================================================="

Write-Host "`nFinal Container Resource Stats:"
docker stats --no-stream sentinelops-api sentinelops-worker sentinelops-redis sentinelops-postgres
