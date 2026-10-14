# SentinelOps — High Availability & Horizontal Scaling Architecture

## 1. Multi-Replica System Architecture

SentinelOps is architected for zero-downtime horizontal scaling across all workload layers:

```
                            +------------------------------------+
                            |         Kubernetes Ingress         |
                            +-----------------+------------------+
                                              |
                   +--------------------------+--------------------------+
                   | Round-Robin / Least Conn                            |
                   v                                                     v
       +-----------------------+                             +-----------------------+
       |   sentinelops-api-1   |                             |   sentinelops-api-2   |
       |  (WebSocket / Client) |                             |  (WebSocket / Client) |
       +-----------+-----------+                             +-----------+-----------+
                   |                                                     |
                   +--------------------------+--------------------------+
                                              | (Pub/Sub Sync via Backplane)
                                              v
                              +-------------------------------+
                              |    Redis Streams & Pub/Sub    |
                              |  - Channel: sentinelops:sig*  |
                              |  - Stream: logs/traces/metrics|
                              |  - Stream: dead-letter        |
                              +---------------+---------------+
                                              |
                      +-----------------------+-----------------------+
                      | Competing Consumers in Worker Consumer Group  |
                      v                                               v
          +-----------------------+                       +-----------------------+
          |  sentinelops-worker-1 |                       |  sentinelops-worker-2 |
          | (PEL Auto-Recovery)   |                       | (PEL Auto-Recovery)   |
          +-----------+-----------+                       +-----------+-----------+
                      |                                               |
                      +-----------------------+-----------------------+
                                              | (Batched Bulk Inserts)
                                              v
                                  +-----------------------+
                                  |      PostgreSQL       |
                                  | (Read/Write Replica)  |
                                  +-----------------------+
```

---

## 2. SignalR Real-Time Scale-Out via Redis Backplane

### The Problem
When the API layer scales to multiple replicas (e.g. 2–10 pods), clients connected to pod A via WebSockets would not receive alerts or metrics published by pod B if notifications were dispatched only in-memory.

### Solution: Redis Backplane
SentinelOps incorporates `Microsoft.AspNetCore.SignalR.StackExchangeRedis`:
- **Package**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` (v9.0.2 with secure `MessagePack` 3.1.12).
- **Configuration**:
  - `SignalR:UseRedisBackplane = true` (configured via `ConfigMap` or environment variable `SignalR__UseRedisBackplane=true`).
  - Channel prefix isolated to `sentinelops:signalr`.
  - `AbortOnConnectFail = false` guarantees the API server boots reliably even during Redis rolling restarts.
- **Verification**: Verified via `SignalR_RedisBackplaneConfiguration_CanBeEnabledViaConfig` integration test, ensuring `RedisHubLifetimeManager` dynamically binds to `AlertsHub`.

---

## 3. Worker Competing Consumers & PEL Recovery

### Competing Consumer Mechanics
- All worker pods register into the same consumer group: `sentinelops-worker-group`.
- Redis Streams distributes entries across active worker pods without duplicate delivery under normal conditions.
- Each worker pod identifies itself with a unique consumer ID (`consumer-{guid}`).

### Worker Crash & Abandoned Entry Recovery (PEL)
If a worker pod crashes or is terminated by Kubernetes eviction while holding unacknowledged messages:
1. Those entries remain in Redis's Pending Entry List (PEL).
2. Normal reads (`StreamPosition.NewMessages` / `>`) will never return them.
3. SentinelOps implements active PEL auto-recovery in `RedisTelemetryBuffer.ReadEntriesWithPelRecoveryAsync`:
   - Every consumer cycle checks `StreamPendingMessagesAsync` across the group.
   - Any message idle for longer than 30,000 ms (`minIdleTimeMs = 30_000`) is claimed using `StreamClaimAsync`.
   - The claiming worker deserializes the recovered entry, processes it, and acknowledges it (`StreamAcknowledgeAsync`).
4. **Poison Message Protection**: If a stream payload is corrupt or fails JSON deserialization:
   - The malformed payload is forwarded to the dead-letter stream (`sentinelops:stream:dead-letter`).
   - The message is acknowledged in the primary stream, preventing infinite crash loops or stuck consumer groups.
5. **Verification**: Verified via `RedisTelemetryBuffer_PelRecovery_ClaimsAbandonedPendingEntries` and `RedisTelemetryBuffer_MalformedPayload_IsRoutedToDeadLetterStreamAndAcknowledged`.

---

## 4. Multi-Replica Database Safety & Migrations

### The Problem
When multiple API replica pods start simultaneously during deployment, multiple instances attempt to run `DbContext.Database.Migrate()` concurrently, resulting in PostgreSQL transaction deadlocks and table locks on `__EFMigrationsHistory`.

### Solution: Decoupled Migration Job
1. In Kubernetes/Helm deployments, database migrations are removed from application startup and encapsulated in a dedicated Kubernetes `Job` (`deploy/helm/sentinelops/templates/job-migration.yaml`).
2. Configured with Helm pre-upgrade hooks (`helm.sh/hook: pre-install,pre-upgrade`, `hook-weight: "1"`).
3. Helm guarantees that migrations run to completion before API pods are scheduled for rolling update.

---

## 5. Zero-Downtime Deployment & Rolling Update Mechanics

| Mechanism | Configuration | Purpose |
| :--- | :--- | :--- |
| **MaxSurge** | `25%` | Boots new healthy pods before terminating old pods. |
| **MaxUnavailable** | `0` | Guarantees zero dropped traffic during deployments. |
| **PodDisruptionBudget** | `minAvailable: 1` | Prevents node drains or voluntary evictions from taking down all API pods. |
| **Liveness Probe** | `GET /health/live` (10s period) | Restarts deadlocked or unresponsive pods. |
| **Readiness Probe** | `GET /health/ready` (5s period) | Removes unready pods from the Ingress load balancer before they accept traffic. |
| **Termination Grace** | `30s` | Allows in-flight HTTP requests and SignalR broadcasts to drain cleanly. |
