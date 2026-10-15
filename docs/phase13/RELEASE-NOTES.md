# SentinelOps Release Notes — Version 1.0.0-rc1

**Release Version**: `v1.0.0-rc1`  
**Release Date**: October 15, 2026  
**Target Runtime**: .NET 10 (`net10.0`), React 19, TypeScript 5.8, PostgreSQL 18, Redis 7.2  

---

## Overview

SentinelOps `v1.0.0-rc1` is the culmination of a thirteen-phase engineering initiative transforming a concept into a secure, multi-tenant, cloud-native enterprise observability and site reliability platform.

SentinelOps unifies high-throughput telemetry ingestion (logs, metrics, distributed traces), real-time application performance monitoring (APM), AI-assisted deterministic incident intelligence, public status page communication, and multi-tenant SaaS subscription billing into a single, cohesive architecture.

---

## Architecture & Evolution Summary (Phases 1 – 13)

### Phase 1: Core Clean Architecture & Domain Foundation
- Established Clean/Onion Architecture across `SentinelOps.Domain`, `SentinelOps.Contracts`, `SentinelOps.Application`, `SentinelOps.Infrastructure`, and `SentinelOps.Api`.
- Defined base domain models: `Application`, `Environment`, `Service`, `LogEntry`, `MetricRecord`, `TraceRecord`, `SpanRecord`, `AlertRule`, `AlertIncident`, and `AuditEvent`.
- Configured EF Core 10 with PostgreSQL 18 and initial database schema migrations.

### Phase 2: Ingestion Pipeline & Durable Buffering
- Implemented OTLP HTTP ingestion endpoint (`/api/v1/traces`, `/api/v1/metrics`, `/api/v1/logs`).
- Designed resilient Redis Streams producer and consumer worker pipeline (`TelemetryProcessorWorker`).
- Enforced strict fail-closed buffering semantics (`DurableBufferUnavailableException`) to prevent silent telemetry loss.

### Phase 3: Telemetry Processing & Real-Time Streaming
- Built MediatR CQRS handlers for aggregated metric rollups, trace waterfall queries, and log searching with structured JSON parsing.
- Integrated SignalR hubs for low-latency live telemetry broadcasting to connected browsers.

### Phase 4: Alerting Engine & Incident Management
- Developed background alerting evaluation loop (`AlertEvaluationWorker`) supporting threshold conditions (`GreaterThan`, `LessThan`, `Equals`), evaluation windows, and cooldown periods.
- Implemented alert state machines (Normal -> Pending -> Firing -> Acknowledged -> Resolved) with audit trails.

### Phase 5: Dynamic Dashboards & Visualization
- Added customizable multi-widget dashboard subsystem supporting time-series line charts, bar charts, gauge meters, and tabular logs.
- Engineered responsive grid layout persistence and customizable refresh intervals.

### Phase 6: Application Performance Monitoring (APM)
- Constructed automatic Service Dependency Map generation from distributed trace spans (`parentSpanId` / `spanId` graph traversal).
- Calculated P50, P90, P95, and P99 latency percentiles, error rates, and external dependency health.

### Phase 7: Public Status Pages & Incident Communication
- Implemented tenant-isolated and publicly accessible status pages (`/status/{slug}`).
- Added real-time incident timeline communication, component status tracking (Operational, Degraded, Partial Outage, Major Outage), and markdown support.

### Phase 8: Multi-Tenancy Architecture & Dynamic Quotas
- Implemented organization-level tenancy (`Organization`, `OrganizationMember`, `OrganizationRole`).
- Added tenant isolation boundaries with scoped query filters and role-based access control (Admin, Operator, Member, Viewer).
- Implemented dynamic quota tracking and tier limits (Starter, Pro, Enterprise).

### Phase 9: System Reliability & Fault Tolerance
- Embedded Polly resilience pipelines with exponential backoff and circuit breaker policies.
- Implemented Outbox Pattern for transactional event reliability.
- Added comprehensive health checks (`/health/live`, `/health/ready`, `/health/startup`).

### Phase 10: Infrastructure, Containerization & Kubernetes
- Authored production-ready multi-stage Dockerfiles utilizing non-root security contexts (`appuser:10001`).
- Configured multi-service `docker-compose.yml` stack (API, Worker, Web, PostgreSQL, Redis, OTel Collector).
- Developed Kubernetes manifests and Helm chart (`deploy/helm/sentinelops/`) supporting Horizontal Pod Autoscaling (HPA), Pod Disruption Budgets (PDB), and Network Policies.

### Phase 11: Explainable AI & Incident Intelligence
- Developed statistical z-score and IQR anomaly detection engine.
- Implemented deterministic Root Cause Analysis (RCA) engine with explainable evidence provenance.
- Built log template clustering (DBSCAN/regex tokenization) with HTML/script tag sanitization.
- Implemented ordinary least squares (OLS) linear telemetry forecasting.
- Enforced mandatory human approval gates (`requiresHumanApproval == true`) for all suggested remediations.

### Phase 12: SaaS Commercialization & Subscription Billing
- Built full subscription lifecycle management supporting Starter, Pro, and Enterprise plans.
- Integrated payment webhook ingestion with HMAC-SHA256 signature verification and 300-second timestamp tolerance.
- Enforced database-backed idempotent webhook deduplication (`WebhookEvent` table).
- Implemented zero-card-data storage architecture (zero PAN/CVV).

### Phase 13: Final Enterprise Audit, Security Hardening & Release Candidate
- Executed 15 adversarial security attack scenarios.
- Remediated 4 multi-tenant authorization defects (`P13-SEC-01` to `P13-SEC-04`): enforced strict organization membership validation on all queries, services, and billing endpoints.
- Implemented frontend dynamic code-splitting via `React.lazy()` and `<Suspense>`, reducing initial JavaScript chunk size by 67% (from 1,341 kB to 432 kB).
- Certified 302 backend tests and 54 frontend tests with 100% pass rates under `--warnaserror`.
- Delivered comprehensive deployment guides, disaster recovery runbooks, and rollback procedures.

---

## Breaking Changes & Migration Notices

1. **Strict Organization Membership Validation**:
   - In previous releases, requests providing an `X-Organization-Id` header were evaluated directly without verifying if the authenticated user belonged to that organization. In `v1.0.0-rc1`, if the caller is not an active member of the specified organization (or platform Admin), the request returns empty lists (queries) or `404 Not Found` (direct resources), preventing cross-tenant IDOR.
2. **Legacy Migration Configuration Gate**:
   - The startup automatic reassignment of orphan applications and active users to `default-org` is now disabled by default. Environments requiring legacy single-tenant data reconciliation must explicitly set `MigrationSettings:AutoReconcileLegacyTenants=true` in `appsettings.json`.
3. **Frontend Lazy Loading**:
   - Route pages are now split into discrete lazy-loaded JavaScript chunks. Fast, seamless client-side navigation is maintained with an accessible animated loading indicator (`RouteFallback`).

---

## Technical Specifications & Requirements

- **Backend**: .NET 10 SDK (`10.0.100`), C# 13, ASP.NET Core 10.
- **Frontend**: Node.js 22+, React 19, TypeScript 5.8, Vite 8.3, Tailwind CSS v4.
- **Database**: PostgreSQL 18 with Entity Framework Core 10 (Npgsql 10.0.0).
- **Caching & Buffering**: Redis 7.2+ / Redis Streams.
- **Containerization**: Docker CLI 28+, Docker Compose v2.33+, OCI-compliant runtime.
- **Orchestration**: Kubernetes 1.29+, Helm 3.14+.
