# SentinelOps Phase 19: Data Retention Policies, Plan Caps & Chunked Lifecycle Management

## 1. Overview & Architecture

SentinelOps enforces automated, lifecycle-driven telemetry pruning to keep storage footprints bounded, optimize database indexes, and satisfy compliance and tier SLAs.

```mermaid
flowchart TD
    Schedule[Worker 24h Cron / API Trigger] --> Scope[Retention Scope Evaluation]
    Scope --> Policy[Read Policy & Plan Caps]
    Policy --> Calc[Compute Cutoff Timestamps]
    Calc --> ChunkLoop{Batch Loop (500 items)}
    ChunkLoop -->|Query Batch <= 500| Purge[Execute Chunked Delete]
    Purge --> WAL[Low-overhead WAL Commit]
    Purge --> ChunkLoop
    ChunkLoop -->|Batch < 500 or Empty| Audit[Write Audit Event & Update Execution Metadata]
```

---

## 2. Plan-Constrained Retention Caps

Tenant retention settings are strictly capped by subscription plan tier to ensure operational fairness and protect disk I/O:

| Tier | Maximum Retention Allowed | Default Retention | Enforcement Point |
| :--- | :--- | :--- | :--- |
| **Starter** | **7 Days** | 7 Days | `OrganizationService.UpdateSettingsAsync` |
| **Pro** | **30 Days** | 30 Days | `OrganizationService.UpdateSettingsAsync` |
| **Enterprise** | **90 Days** | 90 Days | `OrganizationService.UpdateSettingsAsync` |

Any attempt by a tenant on a lower tier to configure retention periods exceeding their tier cap is clamped to the maximum allowed:
```csharp
var maxRetentionDays = RetentionService.GetPlanRetentionCap(settings.PlanTier);
settings.DefaultRetentionDays = Math.Clamp(request.DefaultRetentionDays, 1, maxRetentionDays);
```

---

## 3. High-Performance Batch Deletion Chunking (500 Records/Batch)

Unbounded `DELETE FROM ... WHERE timestamp < cutoff` queries on multi-million row tables can:
- Acquire long-lived exclusive table locks, blocking telemetry ingestion.
- Trigger severe Write-Ahead Log (WAL) spikes.
- Cause replication lag and API timeouts.

SentinelOps eliminates these risks by enforcing **500-record batch chunking**:
```csharp
const int BatchSize = 500;
while (!ct.IsCancellationRequested)
{
    var batchDeleted = await query.Take(BatchSize).ExecuteDeleteAsync(ct);
    totalDeleted += batchDeleted;
    if (batchDeleted < BatchSize) break;
}
```
If the underlying database provider does not support `Take().ExecuteDeleteAsync()`, the engine falls back to transactional entity chunking (`RemoveRange(batch)` in 500-item chunks), guaranteeing zero runtime exceptions across PostgreSQL and in-memory test environments.

---

## 4. Retained Data Categories & Operational Endpoints

| Table / Entity | Metric Timestamp | Default Retention Window |
| :--- | :--- | :--- |
| `LogEntries` | `Timestamp` | System Policy / Tenant Setting (1–90d) |
| `SpanRecords` | `StartedAtUtc` | System Policy / Tenant Setting (1–90d) |
| `TraceRecords` | `StartedAtUtc` | System Policy / Tenant Setting (1–90d) |
| `MetricRecords` | `Timestamp` | System Policy / Tenant Setting (1–90d) |
| `AlertEvaluations` | `EvaluatedAtUtc` | 30 Days |
| `NotificationOutboxes` | `CreatedAtUtc` | 30 Days |
| `InAppNotifications` | `CreatedAtUtc` | 30 Days |
| `AuditEvents` | `Timestamp` | 90 Days (immutable compliance minimum 30d) |

### API Management Endpoints
- **Dry Run Estimation**: `GET /api/retention/dry-run` — returns record counts and oldest timestamp per table without deleting data.
- **Manual Execution**: `POST /api/retention/cleanup` — executes chunked purge with mandatory confirmation and creates a permanent `Retention.CleanupExecuted` audit log.
