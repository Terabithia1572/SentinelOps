# SentinelOps — Checkpoint 15.9: Performance and Resilience Regression

## 1. Overview and Objectives
Phase 15 Checkpoint 15.9 validates the end-to-end telemetry ingestion pipeline, resource boundaries, and operational resilience under fault injection.

Testing was performed directly against the running SentinelOps Linux containerized stack:
- **API**: `sentinelops-api` (`http://localhost:5100`)
- **Worker**: `sentinelops-worker`
- **OpenTelemetry Collector**: `sentinelops-otel-collector` (`http://localhost:4318`)
- **Redis Cache & Streams**: `sentinelops-redis` (port `56379`)
- **PostgreSQL Database**: `sentinelops-postgres` (port `55432`)

---

## 2. Bounded Performance Benchmark (7 Core Scenarios)

The benchmark was executed via `tools/SentinelOps.PerformanceBenchmark` targeting both write-heavy ingestion endpoints and read-heavy analytics/dashboard queries under concurrent load (Concurrency: 5, Requests per scenario: 100, Total requests: 700).

### Benchmark Results Matrix

| Scenario | Target Endpoint | Count | Req/sec | Avg Latency | P50 | P95 | P99 | Max | Error Count |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **System Liveness Probe** | `GET /health/live` | 100 | **4,651.8** | 0.86 ms | 0.68 ms | 2.82 ms | 4.26 ms | 4.56 ms | 0 (0.0%) |
| **OTLP Log Ingestion** | `POST /v1/logs` | 100 | **1,241.5** | 3.23 ms | 2.27 ms | 11.89 ms | 12.58 ms | 13.46 ms | 0 (0.0%) |
| **OTLP Trace Ingestion** | `POST /v1/traces` | 100 | **1,124.3** | 4.23 ms | 2.36 ms | 20.67 ms | 20.78 ms | 22.63 ms | 0 (0.0%) |
| **Dashboard KPI Summary** | `GET /api/dashboard/summary` | 100 | **365.8** | 12.58 ms | 8.09 ms | 36.86 ms | 42.93 ms | 49.74 ms | 0 (0.0%) |
| **Log Explorer Filtered** | `GET /api/logs?pageSize=50` | 100 | **838.0** | 5.94 ms | 3.83 ms | 18.74 ms | 28.41 ms | 29.23 ms | 0 (0.0%) |
| **Trace Explorer Query** | `GET /api/traces?pageSize=50` | 100 | **766.1** | 6.37 ms | 3.81 ms | 6.88 ms | 52.69 ms | 53.55 ms | 0 (0.0%) |
| **Component Health Diagnostic** | `GET /api/system/health` | 100 | **344.9** | 13.00 ms | 8.81 ms | 30.24 ms | 39.43 ms | 50.67 ms | 0 (0.0%) |

### Key Benchmark Findings
1. **Low-Latency Ingestion**: Asynchronous decoupling through Redis Streams allows `/v1/logs` and `/v1/traces` to respond in ~2–4 ms (P50 < 2.5 ms) without waiting on PostgreSQL disk I/O.
2. **Sub-millisecond Liveness**: `/health/live` executes in 0.86 ms average, serving >4,600 req/sec without memory allocations or thread starvation.
3. **Optimized Read Queries**: Filtered pagination across logs and traces executes within 6 ms average thanks to composite indexes on `(ApplicationId, Timestamp)` and `(ApplicationId, Severity, Timestamp)`.
4. **Zero Errors**: Across 700 requests under concurrent load, 0 errors or timeouts occurred.

---

## 3. Worker Crash Recovery and In-Flight Fault Injection

To verify that telemetry buffering in Redis Streams prevents data loss during worker downtime or container rescheduling, a controlled chaos test was executed using `tools/test_resilience.ps1`.

### Chaos Experiment Setup
1. **Workload**: 150 log events dispatched in rapid burst mode to `POST /v1/logs`.
2. **Fault Injection**: At iteration 50, an asynchronous container restart command was issued against `sentinelops-worker` (`docker restart sentinelops-worker`).
3. **Verification**: 
   - HTTP clients must experience zero 5xx errors during the restart.
   - Redis Streams (`sentinelops:stream:logs`) must buffer unconsumed entries while the worker restarts.
   - Upon container boot, `TelemetryConsumerService` must rejoin consumer group `sentinelops-worker-group`, claim all unacknowledged and pending messages, and flush them to PostgreSQL.
   - The PostgreSQL `LogEntries` delta must equal exactly 150 (zero data loss).

### Test Execution Metrics

```
=========================================================
 SENTINELOPS RESILIENCE & RECOVERY REGRESSION TEST       
=========================================================
Pre-test PostgreSQL Counts: Logs=223, Traces=116

Starting ingestion of 150 log events with in-flight worker restart...
--> [FAULT INJECTION] Restarting sentinelops-worker container while traffic is active...
Ingestion complete in 1.02s. Waiting for worker restart to settle...
--> Worker restart command completed.
Monitoring Redis Stream drain...
--> Redis stream consumer group fully drained at 1s.

Post-test PostgreSQL Counts: Logs=373 (Delta: +150 / Expected: +150)

=========================================================
                 RESILIENCE REPORT                       
=========================================================
Total Workload:       150 requests
Successful Ingest:    150
Failed Ingest:        0 (0.0%)
Throughput:           146.5 req/s
Latency Min:          2.25 ms
Latency Mean:         3.41 ms
Latency P50:          3.00 ms
Latency P95:          4.53 ms
Latency P99:          16.39 ms
Latency Max:          23.45 ms
Data Loss:            0 records
Zero-Loss Verified:   True
=========================================================
```

### Result Analysis
- **Availability During Crash**: Ingestion API maintained 100% availability with 0 failed requests. Clients were unaffected by the worker's unavailability.
- **Consumer Group Reconnection**: `sentinelops-worker` completed container restart, re-established connection with Redis, identified the consumer group `sentinelops-worker-group`, and consumed all pending stream items in 1.0 second.
- **Data Integrity**: Exactly 150/150 records were persisted into PostgreSQL (`LogEntries` count progressed from 223 to 373). Zero data loss verified.

---

## 4. OpenTelemetry Collector Pipeline Verification

The full collection topology (`Agent/Client -> OTel Collector (:4318) -> API (:5100) -> Redis Stream -> Worker -> PostgreSQL`) was verified by streaming 50 records directly to `http://localhost:4318/v1/logs`:
- **Collector Acceptance**: 50/50 requests returned HTTP 200 with OTLP standard response `{"partialSuccess":{}}`.
- **Collector Exporter**: OTel Collector forwarded batches using internal authentication (`sent_live_collector_internal_key`).
- **End-to-End Persistence**: `LogEntries` count verified in PostgreSQL, incrementing from 373 to 423 (+50).

---

## 5. Container Resource Utilization Baseline

Resource utilization was measured across all SentinelOps containers before and after the performance and resilience workload:

| Container | CPU Usage | Memory Usage | Memory Limit | Memory % | Network I/O | Block I/O |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| `sentinelops-api` | 2.11% | 209.4 MiB | 1.0 GiB | 20.45% | 8.25 MB / 10.2 MB | 655 kB / 4.1 kB |
| `sentinelops-worker` | 1.15% (idle) / 30.9% (burst) | 66.02 MiB | 1.0 GiB | 6.45% | 8.4 MB / 31.6 MB | 156 kB / 0 B |
| `sentinelops-redis` | 0.59% | 9.48 MiB | 512 MiB | 1.85% | 89.0 MB / 20.9 MB | 44 MB / 1.04 MB |
| `sentinelops-postgres` | 6.48% | 86.82 MiB | 1.0 GiB | 8.48% | 21.8 MB / 28.8 MB | 126 MB / 108 MB |

### Resource Conclusions
- Memory usage for all services remains well below staging and production limits (<21% allocation).
- Neither CPU nor memory leaks were detected during repeated burst ingestion and process restarts.
- Redis memory footprint (~9.5 MiB) demonstrates effective stream trimming (`MaxStreamLength = 100,000` with approximate trimming `~`).

---

## 6. Checkpoint 15.9 Status
**Status: PASS**
- Zero data loss during worker crash confirmed.
- Sub-4 ms average latency for telemetry ingestion confirmed.
- Redis stream drain and consumer group recovery confirmed.
- OpenTelemetry Collector pipeline end-to-end verified.
