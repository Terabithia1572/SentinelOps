$apiKey = Get-Content -Path "tools/perf-key.tmp" -Raw
$endpoint = "http://localhost:5100"
$totalRequests = 100

Write-Host "========================================================="
Write-Host "   SENTINELOPS PHASE 14 - CONTROLLED PERFORMANCE TEST    "
Write-Host "========================================================="
Write-Host "Endpoint: $endpoint"
Write-Host "Burst Requests: $totalRequests"

# Initial DB count
$initCounts = 'SELECT count(*) FROM "LogEntries";' | docker exec -i sentinelops-postgres psql -U sentinelops -d sentinelops -t
$initCount = [int]$initCounts.Trim()
Write-Host "Initial LogEntries count in PostgreSQL: $initCount"

# Record initial docker stats
Write-Host "Baseline container stats:"
docker stats --no-stream sentinelops-api sentinelops-worker sentinelops-redis sentinelops-postgres

# Setup HttpClient
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$client = New-Object System.Net.Http.HttpClient($handler)
$client.DefaultRequestHeaders.Add("X-API-Key", $apiKey)

$latencies = New-Object System.Collections.Generic.List[double]
$successCount = 0
$failCount = 0

Write-Host "Executing burst workload..."
$overallSw = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 1; $i -le $totalRequests; $i++) {
    $nowNano = ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000000).ToString()
    $traceId = [Guid]::NewGuid().ToString("N")
    $spanId = [Guid]::NewGuid().ToString("N").Substring(0, 16)
    
    $payloadObj = @{
        resourceLogs = @(
            @{
                resource = @{
                    attributes = @(
                        @{ key = "service.name"; value = @{ stringValue = "PerfTestService" } },
                        @{ key = "deployment.environment"; value = @{ stringValue = "Benchmark" } }
                    )
                }
                scopeLogs = @(
                    @{
                        scope = @{ name = "PerfTestScope"; version = "1.0.0" }
                        logRecords = @(
                            @{
                                timeUnixNano = $nowNano
                                severityNumber = 9
                                severityText = "Information"
                                body = @{ stringValue = ("Performance benchmark telemetry payload iteration " + $i) }
                                traceId = $traceId
                                spanId = $spanId
                                attributes = @(
                                    @{ key = "iteration"; value = @{ intValue = $i } },
                                    @{ key = "benchmark.run"; value = @{ stringValue = "phase14" } }
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
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $response = $client.PostAsync(($endpoint + "/v1/logs"), $content).GetAwaiter().GetResult()
    $sw.Stop()

    $latencies.Add($sw.Elapsed.TotalMilliseconds)
    if ($response.IsSuccessStatusCode) {
        $successCount++
    } else {
        $failCount++
    }

    if ($i % 25 -eq 0) {
        Write-Host ("Completed " + $i + " / " + $totalRequests + " requests...")
    }
}

$overallSw.Stop()

# Sort latencies for percentiles
$sorted = $latencies | Sort-Object
$min = $sorted[0]
$max = $sorted[$sorted.Count - 1]
$avg = ($latencies | Measure-Object -Average).Average
$p50 = $sorted[[Math]::Floor($sorted.Count * 0.50)]
$p90 = $sorted[[Math]::Floor($sorted.Count * 0.90)]
$p95 = $sorted[[Math]::Floor($sorted.Count * 0.95)]
$p99 = $sorted[[Math]::Floor($sorted.Count * 0.99)]
$throughput = [Math]::Round($totalRequests / ($overallSw.Elapsed.TotalSeconds), 2)

Write-Host "========================================================="
Write-Host "                    BENCHMARK RESULTS                    "
Write-Host "========================================================="
Write-Host ("Total Requests:   " + $totalRequests)
Write-Host ("Successful:       " + $successCount)
Write-Host ("Failed:           " + $failCount)
Write-Host ("Total Duration:   " + [Math]::Round($overallSw.Elapsed.TotalSeconds, 2) + " s")
Write-Host ("Throughput:       " + $throughput + " req/s")
Write-Host ("Latency Min:      " + [Math]::Round($min, 2) + " ms")
Write-Host ("Latency P50:      " + [Math]::Round($p50, 2) + " ms")
Write-Host ("Latency Mean:     " + [Math]::Round($avg, 2) + " ms")
Write-Host ("Latency P90:      " + [Math]::Round($p90, 2) + " ms")
Write-Host ("Latency P95:      " + [Math]::Round($p95, 2) + " ms")
Write-Host ("Latency P99:      " + [Math]::Round($p99, 2) + " ms")
Write-Host ("Latency Max:      " + [Math]::Round($max, 2) + " ms")

# Post-benchmark container stats
Write-Host "Container stats during/after workload:"
docker stats --no-stream sentinelops-api sentinelops-worker sentinelops-redis sentinelops-postgres

# Wait 3 seconds for worker to process queued Redis stream entries
Start-Sleep -Seconds 3

# Redis stream length and pending PEL
$xlen = docker exec sentinelops-redis redis-cli XLEN sentinelops:stream:logs
Write-Host ("Redis Stream Length (XLEN): " + $xlen)

# Check PostgreSQL final count
$finalCounts = 'SELECT count(*) FROM "LogEntries";' | docker exec -i sentinelops-postgres psql -U sentinelops -d sentinelops -t
$finalCount = [int]$finalCounts.Trim()
$persistedNew = $finalCount - $initCount
Write-Host ("PostgreSQL LogEntries Total: " + $finalCount)
Write-Host ("PostgreSQL Newly Persisted:  " + $persistedNew + " / " + $totalRequests)
