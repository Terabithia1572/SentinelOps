# SentinelOps Phase 14 — Controlled Local Performance & Resource Testing

## 1. Executive Summary

- **Status**: `PASS`
- **Date**: October 15, 2026
- **Test Objective**: Measure platform throughput, end-to-end ingestion latency percentiles (P50, P90, P95, P99), container CPU/memory utilization, and Redis stream consumer group backpressure under burst workloads.
- **Workload Profile**: Controlled burst of 100 structured OTLP log ingestion payloads dispatched via HTTP to `http://localhost:5100/v1/logs` authenticated with an active SHA-256 API key.
- **Results**:
  - **Success Rate**: 100 / 100 (100.0%)
  - **Failures / Drops**: 0 (0.0%)
  - **Throughput**: **246.24 req/s**
  - **Duration**: 0.41 seconds
  - **P50 Latency**: **2.52 ms**
  - **P95 Latency**: **4.04 ms**
  - **P99 Latency**: **38.87 ms**
  - **Redis Consumer Lag**: **0** (Pending entries: 0)
  - **PostgreSQL Persistence Rate**: **100%** (21 baseline &rarr; 121 records)

---

## 2. Ingestion Latency Percentiles

| Metric | Measured Value | Acceptance Threshold | Result |
| :--- | :--- | :--- | :--- |
| **Minimum** | **1.97 ms** | < 50 ms | **PASS** |
| **P50 (Median)** | **2.52 ms** | < 25 ms | **PASS** |
| **Mean** | **3.25 ms** | < 30 ms | **PASS** |
| **P90** | **3.01 ms** | < 50 ms | **PASS** |
| **P95** | **4.04 ms** | < 75 ms | **PASS** |
| **P99** | **38.87 ms** | < 200 ms | **PASS** |
| **Maximum** | **38.87 ms** | < 500 ms | **PASS** |

The sub-5ms P95 latency is achieved because the API tier validates the API key against the in-memory/DB cache and pushes the payload directly into Redis Streams (`sentinelops:stream:logs`) asynchronously without blocking on synchronous database writes.

---

## 3. Container Resource Utilization

Captured via `docker stats --no-stream` before, during, and after the burst workload:

| Container | Baseline CPU | Burst CPU | Baseline Memory | Burst Memory | Memory Limit | Memory % |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **sentinelops-api** | 0.87% | 2.98% | 220.5 MiB | 222.6 MiB | 1 GiB | 21.74% |
| **sentinelops-worker** | 0.64% | 14.25% | 103.7 MiB | 107.5 MiB | 1 GiB | 10.50% |
| **sentinelops-redis** | 0.40% | 0.44% | 8.84 MiB | 10.52 MiB | 512 MiB | 2.05% |
| **sentinelops-postgres** | 0.00% | 0.01% | 64.98 MiB | 70.50 MiB | 1 GiB | 6.89% |
| **sentinelops-otel-collector** | 0.64% | 0.64% | 29.12 MiB | 29.12 MiB | 512 MiB | 5.69% |
| **sentinelops-web** | 0.00% | 0.00% | 17.16 MiB | 17.16 MiB | 256 MiB | 6.70% |

### Key Observations:
1. **Total SentinelOps Memory Footprint**: Less than **460 MiB** across all 6 platform containers, comfortably within container caps and well below the 7.628 GiB host memory ceiling.
2. **Worker Concurrency**: Worker CPU scaled to 14.25% to consume the burst batch and immediately returned to idle baseline (< 1%) once the queue was drained.
3. **Database Footprint**: PostgreSQL memory remained stable at ~70 MiB with zero connection spikes.

---

## 4. Redis Stream Resilience & Backpressure Audit

Redis Stream Consumer Group inspection via `XINFO GROUPS sentinelops:stream:logs`:

```text
name:               sentinelops-worker-group
consumers:          9
pending (PEL):      0
last-delivered-id:  1792092844326-0
entries-read:       124
lag:                0
```

- **Stream Length (`XLEN`)**: 124 (24 baseline + 100 new entries)
- **Pending Entries (`XPENDING`)**: 0 (all 124 messages acknowledged via `XACK`)
- **Consumer Lag**: 0 (zero unconsumed backlog)

---

## 5. End-to-End Data Durability

```sql
SELECT count(*) FROM "LogEntries";
```
- **Pre-burst count**: 21
- **Post-burst count**: 121
- **Delta**: **+100 records persisted** (100% durability, 0 dropped records).
