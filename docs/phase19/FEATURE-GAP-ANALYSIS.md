# SentinelOps — Phase 19 Feature Inventory and Gap Analysis

**Date:** 2026-10-17  
**Baseline Commit:** `7b34d41`  
**Architect:** Principal Software & Observability Platform Architect  

---

## 1. Executive Summary

This gap analysis establishes the baseline capability inventory of the SentinelOps platform following Phase 18.2. While the core Clean Architecture backend and Precision Dark UI provide a strong functional foundation (468 automated tests passing, 6 healthy Docker containers), key enterprise observability capabilities require architectural expansion to transition from a baseline system to an enterprise-grade observability SaaS product.

Each capability is classified according to the Phase 19 criteria:
- **`IMPLEMENTED AND VERIFIED`**
- **`IMPLEMENTED BUT INCOMPLETE`**
- **`NOT IMPLEMENTED`**
- **`EXTERNAL INTEGRATION REQUIRED`**

---

## 2. Comprehensive Capability Matrix

| Capability Area | Existing Architecture Components | Current Status | Identified Architectural Gaps | Phase 19 Action Plan |
| :--- | :--- | :--- | :--- | :--- |
| **1. Log APIs & Query Handlers** | `LogsController`, `GetLogsQuery`, `LogEntry` entity | **IMPLEMENTED BUT INCOMPLETE** | No saved searches entity; no structured JSON property filtering; no bounded export; no execution duration/scanned metrics; missing composite index on `(ApplicationId, Timestamp DESC)`. | Add `SavedLogQuery` entity, structured property filter parser, safe bounded export endpoint, and execution telemetry. |
| **2. Distributed Tracing & APM** | `TracesController`, `ApmController`, `ApmService`, `TraceRecord`, `SpanRecord` | **IMPLEMENTED BUT INCOMPLETE** | `ApmService` returns synthetic mock topologies (`GenerateBaselineServiceMap`) when span table is empty; `GetExternalDependenciesAsync` uses static mocks instead of span attributes (`db.system`, `peer.service`); partial trace / clock skew handling needs refinement. | Replace mock generators with zero-telemetry empty-states; extract external dependencies dynamically from span attributes; handle missing parent spans gracefully. |
| **3. Custom Dashboard Builder** | `DashboardsController`, `CustomDashboard` entity | **IMPLEMENTED BUT INCOMPLETE** | Layout stored as JSON but lacks default dashboard endpoint; no server-side widget schema validation; frontend lacks interactive drag/drop widget customizer and data-source binder. | Add `PUT /api/dashboards/{id}/default`, widget configuration validator, and interactive Precision Dark widget composer. |
| **4. SLO / SLI Engineering** | `SlosController`, `ServiceLevelObjective`, `SloEvaluation`, `SloEvaluationService` | **IMPLEMENTED BUT INCOMPLETE** | **Critical Security Gap**: `SlosController.GetSlos` omits tenant application scoping, allowing cross-tenant enumeration; multi-window burn rate (1h/6h) not calculated. | Enforce tenant isolation across all SLO queries; implement mathematically rigorous error budget and burn-rate calculations. |
| **5. Alert Routing & Notifications** | `NotificationChannelsController`, `NotificationDispatcherService`, `NotificationOutbox` | **IMPLEMENTED BUT INCOMPLETE** | Webhook SSRF validation is implemented, but Slack/Teams payload formatting is missing; no channel test endpoint (`POST /test`); delivery state history endpoint missing. | Add Slack & Teams payload adapters, channel test dispatch endpoint with real-time feedback, and delivery outbox history view. |
| **6. Usage Metering & Quotas** | `OrganizationUsage`, `EntitlementService`, `OrganizationsController` | **IMPLEMENTED BUT INCOMPLETE** | Monthly ingestion counters exist, but ingestion boundary does not reject payloads when quota is exceeded; no warning threshold notifications (80%, 95%). | Enforce quota at OTLP ingestion boundaries; add usage meter status endpoint with warning thresholds. |
| **7. Retention & Data Lifecycle** | `RetentionController`, `RetentionService`, `RetentionCleanupService` | **IMPLEMENTED BUT INCOMPLETE** | Global retention setting exists, but plan-tiered retention policies (e.g., 7d vs 30d vs 90d) are not enforced per tenant; batch chunking during deletion needs safety limits. | Implement tenant-aware retention constraints, batch-chunked safe deletion, and auditable dry-run preview. |
| **8. Metrics Aggregation** | `MetricRecord`, `MetricsController`, `OtlpIngestionController` | **IMPLEMENTED AND VERIFIED** | Operational telemetry and percentile calculation verified in prior phases. | Extend query support for custom dashboard metric widgets. |
| **9. Billing & Entitlements** | `BillingController`, `SubscriptionsController`, deterministic sandbox | **IMPLEMENTED AND VERIFIED** | Subscription tiers, feature gating, invoice line items, and sandbox simulation verified. | Connect tenant quota meters to billing overview UI. |
| **10. Security & Authorization** | JWT Bearer, ASP.NET Identity, `RequireAdmin`, `RequireOperator` | **IMPLEMENTED AND VERIFIED** | Server-side role policies and tenant validation verified. | Patch SLO cross-tenant access gap; audit all new Phase 19 endpoints. |
| **11. Database Indexes** | EF Core Configurations, Npgsql | **IMPLEMENTED BUT INCOMPLETE** | Primary indexes exist; missing compound indexes for high-throughput log filtering and span parent traversal. | Add migration for `IX_LogEntries_App_Timestamp`, `IX_SpanRecords_Trace_StartedAt`, and `IX_NotificationOutbox_Status_NextRetry`. |
| **12. Background Workers** | `AlertEvaluation`, `NotificationDispatcher`, `RetentionCleanup`, `SloEvaluation` | **IMPLEMENTED AND VERIFIED** | Four background workers operational in `SentinelOps.Worker`. | Maintain resilient exception handling and non-blocking loops. |
| **13. Container Reproducibility** | `deploy/docker/Dockerfile.web`, `docker-compose.yml` | **IMPLEMENTED AND VERIFIED** | Verified during Phase 19 kickoff via `docker build -f deploy/docker/Dockerfile.web -t sentinelops-web:test-repro .` (build succeeded in 13.9s with exact Precision Dark assets). | Keep production images reproducibly buildable from source. |

---

## 3. High-Priority Action Items for Phase 19

1. **Security & Tenant Isolation**: Secure `SlosController` and add tenant validation to all custom dashboard and notification endpoints.
2. **Advanced Log Explorer**: Introduce `SavedLogQuery` entity, structured property filtering, and safe export.
3. **Tracing & Real APM**: Eliminate synthetic mock topology fallbacks in `ApmService`; dynamically compute dependency graphs and service maps from actual span attributes.
4. **Custom Dashboard Engine**: Implement widget data-source binding, server-side validation, and default dashboard management.
5. **SLO Engine**: Formalize error budget calculations, multi-window burn rates, and tenant-scoped evaluations.
6. **Notification Delivery**: Add Slack/Teams webhook transformers, test send endpoint, and outbox delivery audit trail.
7. **Database Migration**: Deploy composite indexes for log search, trace waterfalls, and notification outbox polling.
