# SentinelOps — Data Durability, Fail-Closed Policy & Resilience Specification

## 1. Executive Summary & Policy Overview

SentinelOps provides enterprise-grade data durability guarantees for high-throughput observability streams (logs, traces, and metrics).

To satisfy both developer velocity in local environments and strict zero-data-loss compliance in enterprise production clusters, SentinelOps implements a **configurable fail-closed vs. fail-open durability policy**.

| Mode | Target Environment | `RequireDurableStorage` | Behavior When Redis Disconnected / Buffer Full | Data Safety Guarantee |
| :--- | :--- | :--- | :--- | :--- |
| **Fail-Closed** | **Production & Staging** | `true` | **Throws `DurableBufferUnavailableException`** -> Returns **HTTP 503 (`Service Unavailable`)** with `Retry-After: 5` | **Zero silent data loss**. OpenTelemetry Collector buffers in local disk queue and retries with backoff. |
| **Fail-Open (Fallback)** | **Local Dev & Testing** | `false` | Buffers in bounded in-memory channels (`Channel<T>`) | High developer velocity; frictionless standalone operation without Redis. |

---

## 2. Architecture & Request Pipeline

```
  OTel SDK / App
        |
        | 1. HTTP POST /v1/logs, /v1/traces, /v1/metrics (with X-API-Key)
        v
+---------------------------------------------------------------------------------+
|                                SentinelOps API                                  |
|                                                                                 |
|   +--------------------------+                                                  |
|   | OtlpIngestionController  |                                                  |
|   +-------------+------------+                                                  |
|                 | 2. PushLogsAsync()                                            |
|                 v                                                               |
|   +-------------------------------------------------------------------------+   |
|   |                        RedisTelemetryBuffer                             |   |
|   |                                                                         |   |
|   |   Is Redis connected?                                                   |   |
|   |       YES ──> XADD sentinelops:stream:logs (Durable AOF Buffer)         |   |
|   |       NO  ──> Is RequireDurableStorage == true?                         |   |
|   |                   YES ──> THROW DurableBufferUnavailableException       |   |
|   |                   NO  ──> Write to in-memory Channel<T> (Fallback)      |   |
|   +------------------------------------+------------------------------------+   |
|                                        |                                        |
|   +------------------------------------+------------------------------------+   |
|   |                    ExceptionHandlingMiddleware                          |   |
|   |                                                                         |   |
|   |   Catches DurableBufferUnavailableException:                            |   |
|   |   - Sets HTTP 503 Service Unavailable                                   |   |
|   |   - Injects Header "Retry-After: 5"                                     |   |
|   |   - Returns RFC 7807 ProblemDetails: "Telemetry Ingestion Unavailable"  |   |
|   +------------------------------------+------------------------------------+   |
+----------------------------------------+----------------------------------------+
                                         | 3. HTTP 503 + Retry-After: 5
                                         v
                                OpenTelemetry Collector
                         (Retries ingestion with backoff)
```

---

## 3. Configuration & Deployment

### Enabling Fail-Closed Policy
In Kubernetes or Docker environments, set the configuration in `appsettings.json`, environment variables, or Helm `ConfigMap`:

#### Helm / Kubernetes (`configmap.yaml`):
```yaml
data:
  TelemetryBuffer__RequireDurableStorage: "true"
```

#### ASP.NET Core `appsettings.Production.json`:
```json
{
  "TelemetryBuffer": {
    "RequireDurableStorage": true
  }
}
```

---

## 4. Kubernetes Readiness Probe Integration

Kubernetes endpoints must not route traffic to API pods that cannot durably persist incoming telemetry.

SentinelOps registers `TelemetryBufferHealthCheck` into `/health/ready`:
- When `RequireDurableStorage == true` and Redis is disconnected:
  - Reports `HealthStatus.Unhealthy` with description `"Durable Redis stream telemetry buffer is disconnected while production fail-closed policy is active."`.
  - Kubernetes automatically removes the pod from Ingress and Service endpoints.
- When `RequireDurableStorage == true` and Redis is connected:
  - Reports `HealthStatus.Healthy` (`"Durable Redis stream telemetry buffer is connected and operational."`).
- When `RequireDurableStorage == false`:
  - Reports `HealthStatus.Healthy` with fallback notification.

---

## 5. Storage Durability & Stream Retention in Redis

To protect against Redis container restarts or host failures:
- **Append-Only File (AOF)**: Configured with `appendonly yes` and `appendfsync everysec` in `statefulset-redis.yaml`.
- **RDB Snapshots**: Snapshotting enabled (`save 900 1`, `save 300 10`).
- **Bounded Retention**: Streams enforce `MAXLEN ~100,000` to prevent memory exhaustion while retaining several hours of peak ingestion.
- **Dead-Letter Stream**: Corrupted or unparseable payloads are routed to `sentinelops:stream:dead-letter` with `type` and raw `data` attributes, avoiding consumer poison loops.

---

## 6. Automated Verification

Durability and resilience behaviors are verified in `tests/SentinelOps.Api.IntegrationTests/DataDurabilityAndResilienceTests.cs`:
1. `RedisTelemetryBuffer_WhenDurableStorageRequiredAndRedisDisconnected_FailsClosedWithException`: Verified `DurableBufferUnavailableException` thrown on disconnect.
2. `RedisTelemetryBuffer_WhenDurableStorageNotRequiredAndRedisDisconnected_AllowsInMemoryFallback`: Verified in-memory buffer continues working in dev mode.
3. `TelemetryBufferHealthCheck_WhenDurableStorageRequiredAndRedisDisconnected_ReportsUnhealthy`: Verified unready status when Redis is down.
4. `TelemetryBufferHealthCheck_WhenDurableStorageRequiredAndRedisConnected_ReportsHealthy`: Verified ready status when Redis is healthy.
5. `TelemetryBufferHealthCheck_WhenDurableStorageNotRequired_ReportsHealthyWithFallbackNotice`: Verified fallback notice.
6. `IngestionEndpoint_WhenBufferThrowsDurableBufferUnavailableException_Returns503WithRetryAfter`: Verified end-to-end HTTP 503 response and `Retry-After: 5` header.
