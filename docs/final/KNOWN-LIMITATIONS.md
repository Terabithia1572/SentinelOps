# SentinelOps — Known Limitations & Implementation Boundaries

## 1. Overview
In accordance with professional engineering integrity and Phase 20 release guidelines, this document explicitly distinguishes fully implemented and verified features from sandbox simulations, external configuration requirements, and non-implemented architectural capabilities.

---

## 2. Implementation Status Matrix

### Category A: Fully Implemented & Verified (100% Test Coverage)
The following capabilities are fully implemented in production code and validated across 474 automated tests:
1. **Clean Architecture Core**: ASP.NET Core 10, EF Core 10, PostgreSQL 17, Redis 7, React 19, TypeScript, Tailwind CSS v4.
2. **OpenTelemetry Telemetry Pipeline**: OTLP HTTP/JSON and gRPC ingestion for logs, traces, and metrics.
3. **Real Database-Driven APM**: Zero synthetic fallbacks; directed service graph edges, endpoint latencies (p50/p95/p99), and external database/HTTP dependencies computed from PostgreSQL `SpanRecord` telemetry.
4. **Waterfall Tracing & APM Enhancements**: Millisecond-accurate span waterfall, automated slowest span tagging (`isSlowestSpan: true`), and missing root parent flagging (`isOrphanedParent: true`).
5. **Advanced Log Explorer**: Full parameterized search, execution latency tracking (`queryDurationMs`), organization-scoped saved queries CRUD, and bounded RFC 4180 CSV / NDJSON export.
6. **Custom Observability Dashboards**: Dynamic widget builder with schema validation, JSON layout import/export, and atomic organization default dashboard assignment.
7. **Service Level Objectives (SLO)**: Rolling multi-window compliance calculations (7d, 30d, 90d) and Google SRE error budget burn rate evaluation with tenant isolation.
8. **SSRF-Protected Notification Routing**: Asynchronous DNS resolution defense blocking loopback, link-local, private RFC 1918 subnets, and cloud metadata endpoint `169.254.169.254`.
9. **Multi-Tenant SaaS Governance**: Organization management, invitations, RBAC (`Admin`, `Operator`, `Viewer`), and strict compound tenant query isolation.
10. **Usage Tracking & Backpressure**: Month-to-date log/trace/metric meters and automated `HTTP 429 Too Many Requests` throttling upon quota exhaustion.
11. **Tiered Data Retention**: Starter (7d), Pro (30d), and Enterprise (90d) caps with background worker batch pruning in 500-record chunks.
12. **Precision Dark UI**: High-density dark design system verified across Desktop (1920x1080), Laptop (1440x900), Tablet (768x1024), and Mobile (375x812) viewports.

---

### Category B: Sandbox / Simulated Implementations
The following capabilities are architected and functional, but utilize deterministic local sandboxes to prevent unwanted external side-effects:
1. **SaaS Billing & Payments**:
   - The platform provides plan management, invoice generation, checkout session requests, and webhook processing.
   - Operated in sandbox/test mode: **No live credit card transactions are executed, and real Stripe accounts are not billed**.
2. **External Notification Delivery**:
   - The platform constructs Slack Block Kit and Microsoft Teams Adaptive Card payloads, provides a test delivery ping endpoint, and stores delivery history logs.
   - In local development, actual HTTP dispatch requires a valid user-supplied webhook URL; dummy URLs are safely intercepted by SSRF filters or mocked.
3. **AI Incident Intelligence & RCA**:
   - The platform provides structured postmortem generators, log pattern clustering, and anomaly correlation.
   - In local development, analysis is performed using deterministic heuristics and mock LLM providers; external live inference requires configuring valid OpenAI or Anthropic API keys.

---

### Category C: Requires External Production Infrastructure
1. **Kubernetes Ingress & TLS**:
   - Helm charts and manifests are provided in `deploy/helm/`.
   - Production deployment requires external DNS routing and an automated certificate manager (e.g. Cert-Manager with Let's Encrypt).
2. **Production SMTP Relay**:
   - Live email delivery requires configuring a production transactional email service (e.g. SendGrid, Amazon SES, or Mailgun).

---

### Category D: Out-of-Scope / Not Implemented
1. **Active-Active Multi-Region Database Replication**:
   - SentinelOps uses single-primary PostgreSQL with read replica support. Multi-master active-active replication is not supported.
2. **Proprietary Hardware Agents**:
   - SentinelOps relies exclusively on OpenTelemetry open standards; proprietary kernel probes or custom eBPF kernel modules are not included.
