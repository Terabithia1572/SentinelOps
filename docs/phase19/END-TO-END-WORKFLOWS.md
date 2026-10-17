# SentinelOps — Phase 19 End-to-End Workflows & Verification

## 1. Overview
This document specifies the validation procedures and execution outcomes for the 7 primary enterprise observability and multi-tenant SaaS workflows implemented during Phase 19.

---

## 2. Validated End-to-End Workflows

### Workflow 1: Distributed Tracing & APM Dependency Analysis
1. **Telemetry Ingestion**: Ingest OpenTelemetry OTLP spans via HTTP/Protobuf through the OTEL collector (`sentinelops-otel-collector:4318`) or API directly.
2. **Trace Exploration**: Query traces via `GET /api/traces/{traceId}`.
3. **Slowest Span Identification**: The system computes span durations across all nodes in the trace tree, identifying and annotating the maximum duration span with `isSlowestSpan: true` and highlighting it in the UI with a distinct amber badge (`Slowest`).
4. **Orphaned Parent Detection**: Spans referencing non-existent parent IDs or unindexed roots are flagged with `isOrphanedParent: true`, rendering an inline notice and banner (`Missing Parents Detected`).
5. **Dynamic Service Map**: Real database-driven service dependency graph computed via `GET /api/apm/service-map`, rendering directed communication edges without synthetic mock fallbacks.

### Workflow 2: Advanced Log Exploration & Bounded Export
1. **Search & Severity Filtering**: Query logs via `GET /api/logs` with multi-predicate filtering (`applicationId`, `severity`, `timeRange`, `search`).
2. **Execution Latency Tracking**: The API computes and returns server-side query processing duration in `queryDurationMs`, rendered in the UI with an execution stopwatch badge.
3. **Saved Log Queries**:
   - `GET /api/saved-log-queries` retrieves organization-scoped query templates.
   - `POST /api/saved-log-queries` saves frequently executed queries with custom names and tags.
   - `DELETE /api/saved-log-queries/{id}` cleanly purges queries with tenant boundary enforcement.
4. **Bounded Export**:
   - `GET /api/logs/export/csv` exports up to 10,000 log records in RFC 4180 format.
   - `GET /api/logs/export/json` exports NDJSON formatted telemetry for external ingestion.

### Workflow 3: Custom Observability Dashboards & Default Provisioning
1. **Dashboard Lifecycle**: Multi-tenant CRUD operations via `/api/dashboards`.
2. **Layout Sanitization**: Inbound layout schemas are validated against strict widget type and boundary rules (`MetricChart`, `LogStream`, `ServiceHealth`, `ApmSummary`, `SloBurnRate`).
3. **Default Dashboard**:
   - `POST /api/dashboards/{id}/default` assigns the organization's primary dashboard.
   - Clears prior defaults in an atomic transaction.
   - UI reflects default designation with a persistent gold `Default` star indicator.

### Workflow 4: SLO Engineering & Multi-Window Burn Rate Alerting
1. **SLO Configuration**: Create and monitor service level objectives with rolling evaluation windows (7d, 30d, 90d).
2. **Tenant Isolation Enforcement**: `SlosController` requires organization validation for all create, update, delete, and evaluation queries.
3. **Burn Rate Computation**: Real-time evaluation of error budget burn rates (1h and 6h short/long windows) according to Google SRE alerting standards.

### Workflow 5: Enterprise Alert Routing & SSRF-Protected Webhooks
1. **Multi-Channel Routing**: Configure Slack, Microsoft Teams, Webhook, and Email notification channels.
2. **SSRF Defenses**: All outbound URLs are validated by `SsrfProtectionValidator`, rejecting loopback (`127.0.0.1`, `localhost`), link-local (`169.254.169.254`), and private RFC 1918 IPv4/IPv6 ranges.
3. **Channel Test & Verification**:
   - `POST /api/notification-channels/{id}/test` dispatches a synthetic alert verification ping.
   - Records delivery attempt in `NotificationDeliveryLog`.
4. **Delivery Audit History**: `GET /api/notification-channels/{id}/history` provides inspection of HTTP status codes, response latencies, and error payloads.

### Workflow 6: Usage Tracking, Quotas & Ingestion Throttling
1. **Usage Aggregation**: `GET /api/organizations/current/usage` returns real-time ingested log, trace, and metric volumes against plan limits.
2. **Dynamic Quota Badging**: Organizations UI displays visual threshold badges at 80% (Warning), 95% (Critical), and 100% (Throttled).
3. **OTLP Ingestion Throttling**: When usage exceeds 100% of plan allocation, the API rejects OTLP ingestion requests with `HTTP 429 Too Many Requests` and a standard `Retry-After: 3600` header.

### Workflow 7: Data Retention Lifecycle & Background Purging
1. **Plan Retention Caps**:
   - Starter Tier: 7 days retention limit.
   - Pro Tier: 30 days retention limit.
   - Enterprise Tier: 90 days retention limit.
2. **Chunked Deletion**: `RetentionCleanupService` executes in the background worker every 60 minutes, batching deletions in chunks of 500 records to prevent PostgreSQL transaction log lockups and table locks.
