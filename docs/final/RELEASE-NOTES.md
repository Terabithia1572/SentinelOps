# SentinelOps — Release Notes (v1.0.0-rc1)

## 1. Overview
SentinelOps is a multi-tenant enterprise observability and Site Reliability Engineering (SRE) SaaS platform designed for high-scale distributed systems. Built using ASP.NET Core 10, Entity Framework Core 10, PostgreSQL 17, Redis Streams, OpenTelemetry, React 19, and Vite, SentinelOps unifies logs, distributed traces, metrics, APM dependency graphs, AI-assisted root cause analysis, SLO monitoring, and SaaS commercialization within the approved **Precision Dark** design system.

---

## 2. Key Release Highlights

### Multi-Tenant Telemetry Core
- **OpenTelemetry Standard Ingestion**: Native OTLP ingestion over HTTP/JSON and gRPC for traces, logs, and metrics.
- **Advanced Log Explorer**: High-speed parameterized log search with server-side query latency measurement (`⏱ {queryDurationMs}ms`), organization-scoped saved queries, and streaming bounded CSV / NDJSON export.
- **Trace Waterfall & APM Deep Dive**: Millisecond-precision span waterfall visualizer with automated slowest-span detection (`isSlowestSpan: true`), missing parent span flagging (`isOrphanedParent: true`), and directed service communication topology maps calculated directly from database records.
- **Custom Observability Dashboards**: Dynamic drag-and-drop dashboard builder supporting 12 customizable widget types, JSON layout import/export, and organization-scoped default dashboard assignment.

### SRE & Alert Routing Engine
- **Service Level Objectives (SLO)**: Google SRE-style multi-window error budget tracking with 1h/6h rolling burn rates and multi-tenant isolation.
- **Multi-Channel Alert Routing**: First-class alert delivery adapters for Slack, Microsoft Teams, generic HTTP webhooks, and email.
- **SSRF Network Protection**: Asynchronous DNS resolution defenses intercepting and denying webhook payloads targeting internal loopback, private RFC 1918 subnets, or cloud metadata endpoints (`169.254.169.254`).
- **Real-Time SignalR Streaming**: Low-latency incident dispatching via `/hubs/alerts` with automatic client reconnection and cache invalidation.

### SaaS Governance & Operational Hardening
- **Multi-Tenant Organizations**: Self-service organization provisioning, team member invitations with RBAC (`Admin`, `Operator`, `Viewer`), and scoped tenant isolation.
- **Usage-Based Quotas & Backpressure**: Month-to-date log, trace, and metric consumption tracking with automated ingestion throttling (`HTTP 429 Too Many Requests`) upon quota exhaustion.
- **Tiered Data Retention**: Starter (7 days), Pro (30 days), and Enterprise (90 days) retention caps enforced by background worker batch purges in 500-record chunks.
- **Precision Dark UI**: Cohesive dark design system crafted for dense telemetry analysis, verified across Desktop (1920x1080), Laptop (1440x900), Tablet (768x1024), and Mobile (375x812) viewports.

---

## 3. Verification & Quality Gates
- **Total Automated Tests**: 474 passing tests (309 .NET backend, 76 React unit, 89 Playwright E2E across 4 device viewports).
- **Compilation**: 0 compiler errors, 0 compiler warnings (`TreatWarningsAsErrors=true` enforced).
- **Containerization**: 100% reproducible multi-stage Docker builds for API, Web, and Worker services.
- **Data Safety**: Zero volume destruction, host PostgreSQL port 5432 strictly avoided.

---

## 4. Release Tag
- **Tag**: `v1.0.0-rc1` (Prerelease / Portfolio Release)
- **Status**: **PORTFOLIO RELEASE VERIFIED**
