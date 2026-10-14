# SentinelOps — Phase 9 Comprehensive Risk Register

**Version:** 9.0.0  
**Date:** 2026-10-14  
**Audit Lead:** SentinelOps Principal Security & Infrastructure Architect  
**Governing Standard:** ISO 31000 Risk Management / OWASP Top 10 / SOC 2 Trust Services Criteria  

---

## 1. Risk Classification Matrix

| Severity Level | Definition | Mandatory Action |
| :--- | :--- | :--- |
| **Critical** | Potential catastrophic failure: cross-tenant data breach, credential disclosure, unrecoverable data loss. | Immediate stop-the-line mitigation required before release. |
| **High** | Major infrastructure or durability impairment: container engine unavailable, unbuffered telemetry loss on crash. | Workaround or architectural isolation required; marked in readiness verdict. |
| **Medium** | Architectural risk requiring guardrails: SSRF vector, SignalR channel eavesdropping, quota bypass. | Automated unit/integration regression tests and defense-in-depth mitigation. |
| **Low** | Minor operational friction: connection pool tuning, client-side retry telemetry delays. | Monitoring alerts and runbook documentation. |

---

## 2. Active Risk Register

| Risk ID | Category | Severity | Description | Impact | Evidence | Mitigation Strategy | Owner | Status |
| :--- | :--- | :---: | :--- | :--- | :--- | :--- | :--- | :---: |
| **RSK-01** | Infrastructure | **High** | **Docker Desktop Linux Engine Daemon Offline:** The Windows service `com.docker.service` is active, but the underlying WSL2 VM (`docker-desktop`) is stopped and pipe `//./pipe/dockerDesktopLinuxEngine` is closed. | Automated container orchestration (`docker compose up`) fails until Docker Desktop GUI is launched. | `docker info` returns `open //./pipe/dockerDesktopLinuxEngine: Sistem belirtilen dosyayı bulamıyor`. | Non-destructive diagnosis; all native verification and test pipelines proceed independently; container tasks marked BLOCKED. | DevOps Lead | **DOCUMENTED / BLOCKED** |
| **RSK-02** | Durability | **High** | **Redis Streams Offline Fallback Data Loss Window:** Redis server on port 6379 is offline. Ingestion buffer falls back to in-memory `System.Threading.Channels.Channel<T>`. | If the API process terminates ungracefully before buffer flush, in-flight unpersisted telemetry held in volatile memory is lost. | Port 6379 TCP connection refused; `RedisTelemetryBuffer` log indicates in-memory fallback channels active. | In production, deploy Redis Sentinel / Cluster with AOF persistence. API implements graceful `IHostedService` shutdown flushing bounded channels. | Platform Lead | **MITIGATED (Graceful Drain)** |
| **RSK-03** | Security | **High** | **Cross-Tenant Data Leakage via Direct ID Manipulation:** Malicious tenant crafts requests with foreign tenant IDs or forged `X-Organization-Id` headers. | Unauthorized disclosure of application configurations, telemetry logs, traces, alerts, or audit trails. | Historical pattern in multi-tenant SaaS migrations without strict query-level tenant filters. | `ICurrentTenantService` validates active membership on every request. Queries strictly filter `OrganizationId`. Unauthorized requests return 403/404. | Security Architect | **CONTROLLED & TESTED** |
| **RSK-04** | Telemetry | **Medium** | **OpenTelemetry Collector Single-Key Tenant Collapsing:** If an OTel Collector forwards multi-tenant workloads using a single static API key, attribution collapses to one tenant. | Ingested spans and metrics from different organizations are merged under the key owner's organization. | OTLP HTTP/gRPC exporter configuration in `otel-collector-config.yaml`. | Direct SDK ingestion uses tenant-specific API keys; Collector deployments require per-tenant pipeline configs or dynamic auth headers. | Observability Lead | **MITIGATED** |
| **RSK-05** | Security | **Medium** | **Notification Channel SSRF & Internal IP Exfiltration:** Malicious or compromised operator registers notification webhook targeting internal RFC1918 subnets, cloud metadata, or loopback. | Internal network reconnaissance, cloud instance metadata extraction, unauthorized HTTP triggers. | Past vulnerability vectors in generic webhook dispatcher services. | `SsrfValidator` resolves IP literals, blocks loopback (`127.0.0.1`, `::1`), RFC1918 ranges, and cloud metadata (`169.254.169.254`). | Security Architect | **RESOLVED & VERIFIED** |
| **RSK-06** | Real-Time | **Medium** | **SignalR Unauthorized Group Subscription:** Authenticated user attempts to subscribe to another organization's real-time alert stream group (`org_{targetOrgId}`). | Real-time leakage of active alert firings, incident status transitions, and SLO breaches. | WebSocket connection query parameter authentication in `AlertsHub`. | Hub verifies user membership in the requested organization before adding connection to tenant-specific SignalR group. | Backend Lead | **CONTROLLED & TESTED** |
| **RSK-07** | Performance | **Low** | **Database Connection Pool Saturation under Ingestion Burst:** Concurrent bursts of telemetry ingestion exhausting PostgreSQL max connection pool. | HTTP 500/503 errors and elevated latency on user-facing dashboard queries. | High-concurrency benchmark stress tests. | Batching in worker buffer, pooling options tuned in `NpgsqlDataSource`, composite index `IX_LogEntries_ApplicationId_Severity_Timestamp`. | Database Admin | **MONITORED** |
| **RSK-08** | SaaS Quota | **Low** | **Quota Limit Telemetry Ingestion Throttling:** Rapid telemetry spikes exceeding organization monthly log or trace quota ceilings. | Telemetry discarded or delayed if strict hard caps are applied without burst buffering. | Live quota tracking in `OrganizationService.GetQuotaStatusAsync`. | Visual warning banners at 80% and 100% capacity; configurable overage buffer and administrative quota adjustment. | Product Lead | **CONTROLLED** |

---

## 3. Residual Risk & Mitigation Governance

1. **Zero-Tolerance Gates:** Zero unresolved Critical or High security vulnerabilities are permitted in production release artifacts.
2. **Regression Testing Assurance:** All tenant isolation controls, SSRF defenses, and authentication filters are backed by dedicated integration regression tests.
3. **Continuous Auditing:** Any credential, secret, or key detected in version control triggers an immediate security gate halt and credential rotation protocol.
