# SentinelOps — Phase 9 Exit Gaps & Architectural Dependency Map

**Document Version:** 1.0  
**Phase:** 10 (Infrastructure Recovery, Kubernetes, Horizontal Scaling & High Availability)  
**Audit Date:** 2026-10-14  
**Audit Scope:** Unresolved operational, architectural, and durability limitations inherited from Phase 9  

---

## 1. Executive Summary

Phase 9 established complete multi-tenant security enforcement, end-to-end alerting/incident integration, empirical performance benchmarks, and verified database disaster recovery (19 tables, 139 indexes with 100% parity). However, several critical infrastructure and cloud-native scaling gaps were identified during the exit audit. These gaps define the primary engineering roadmap for Phase 10.

---

## 2. Comprehensive Phase 9 Exit Gaps Audit

### Gap 1: Docker Linux Engine Offline (`BLOCKED`)
- **Evidence:** `docker info` exits with code 1; `open //./pipe/dockerDesktopLinuxEngine: Sistem belirtilen dosyayı bulamıyor`. `wsl --list --verbose` reports distribution `docker-desktop` in state `Stopped`.
- **Impact:** Live container execution (`docker compose up`) and live multi-container network testing remained unverified in Phase 9.
- **Phase 10 Requirement:** Attempt safe non-destructive recovery in Checkpoint 10.1. If engine activation requires GUI login or user action, clearly mark runtime container tests as BLOCKED while continuing static validation, code hardening, and independent test suites.

### Gap 2: Durable Telemetry Transport vs. In-Memory Fallback
- **Evidence:** `src/SentinelOps.Infrastructure/Services/RedisTelemetryBuffer.cs` diverts telemetry to volatile `System.Threading.Channels.Channel<T>` when Redis is offline or disconnected.
- **Impact:** In development and test fixtures, in-memory buffering prevents crashes. However, in production, if an API node crashes or is evicted by Kubernetes while holding unpersisted telemetry in its local RAM, data loss occurs without client notification.
- **Phase 10 Requirement:** Implement an explicit production durability policy:
  - When `ASPNETCORE_ENVIRONMENT=Production` and durable transport is required, fail-closed or return documented HTTP 503 (`Service Unavailable`) with `Retry-After`.
  - Expose durability state in health and readiness probes (`/health/ready`).
  - Do not acknowledge HTTP 200/202 to OTLP clients until durable transport is confirmed.

### Gap 3: SignalR Horizontal Scale-Out & Cross-Replica Group Sync
- **Evidence:** `AlertsHub.cs` and `RealtimeNotifier.cs` currently rely on local in-process `IHubContext<AlertsHub>`.
- **Impact:** When SentinelOps API scales to 2 or more replicas behind a load balancer, clients connected via WebSocket to Pod A will NOT receive real-time notifications (`AlertFired`, `IncidentCreated`) dispatched by background events processed on Pod B.
- **Phase 10 Requirement:** Integrate Redis Backplane for SignalR (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) with fallback logic, ensuring seamless real-time fan-out across multiple API pods.

### Gap 4: Competing Worker Consumers, Acknowledgment & Poison Messages
- **Evidence:** `SentinelOps.Worker` processes Redis Streams using consumer group `sentinelops-worker-group`.
- **Impact:** In horizontal scaling with multiple worker replicas, workers act as competing consumers. If a worker pod is killed midway through processing an unacknowledged batch, those entries remain in Pending Entries List (PEL).
- **Phase 10 Requirement:** Implement active pending entry claiming (`XAUTOCLAIM` / `XPENDING`), consumer dead-letter routing (`sentinelops:stream:dead-letter`), and idempotent message persistence.

### Gap 5: Database Migration Concurrency on Multi-Replica API Startup
- **Evidence:** If multiple API pods start simultaneously in Kubernetes with `context.Database.MigrateAsync()` in their startup path, they will race to apply EF Core migrations, creating lock contention or deadlock errors on PostgreSQL `__EFMigrationsHistory`.
- **Phase 10 Requirement:** Decouple database migrations from runtime API pods:
  - Provide a dedicated Kubernetes `Job` or Helm `pre-install`/`pre-upgrade` hook for schema migrations.
  - Runtime API pods must run with least-privilege credentials (`sentinelops_app`) without DDL permissions.

### Gap 6: Kubernetes Architecture, Helm Packaging & Resource Specs
- **Evidence:** No Kubernetes manifests, Helm charts, or cloud-native configurations existed prior to Phase 10.
- **Phase 10 Requirement:** Construct enterprise Helm chart (`deploy/helm/sentinelops/`) with full templates (API, Worker, Web, Collector, Redis, Ingress, HPA, PDB, NetworkPolicies, and ConfigMaps/Secrets).

---

## 3. Evidence-Based Architectural Dependency Map

```mermaid
flowchart TD
    subgraph S0["Prerequisites & Diagnostics"]
        C10_0["Checkpoint 10.0: Phase 9 Exit Audit & Gaps Map"] --> C10_1["Checkpoint 10.1: Docker & WSL2 Recovery Gate"]
    end

    subgraph S1["Core Infrastructure & Pipelines"]
        C10_1 -->|If Engine Recovers| C10_2["Checkpoint 10.2: Full Telemetry Pipeline Acceptance"]
        C10_1 -->|If Engine Blocked| C10_2B["Checkpoint 10.2: Document Blocked Container Status"]
    end

    subgraph S2["Cloud-Native & HA Architecture"]
        C10_0 --> C10_3["Checkpoint 10.3: Kubernetes Architecture & Helm Packaging"]
        C10_3 --> C10_4["Checkpoint 10.4: Horizontal Scaling, SignalR Backplane & Worker HA"]
        C10_4 --> C10_5["Checkpoint 10.5: Data Durability, Fail-Closed Policy & Resilience"]
    end

    subgraph S3["Security, GitOps & Acceptance"]
        C10_3 --> C10_6["Checkpoint 10.6: Kubernetes & Platform Security Hardening"]
        C10_6 --> C10_7["Checkpoint 10.7: GitOps, CI/CD Pipeline & Release Engineering"]
        C10_2 --> C10_8["Checkpoint 10.8: Performance, Failure Testing & Final Acceptance"]
        C10_5 --> C10_8
        C10_7 --> C10_8
    end

    classDef passed fill:#d4edda,stroke:#28a745,stroke-width:2px;
    classDef blocked fill:#fff3cd,stroke:#ffc107,stroke-width:2px;
    classDef pending fill:#e2e3e5,stroke:#6c757d,stroke-width:2px;

    class C10_0 passed;
    class C10_1,C10_2B blocked;
    class C10_2,C10_3,C10_4,C10_5,C10_6,C10_7,C10_8 pending;
```

---

## 4. Phase 10 Checkpoints & Execution Strategy

1. **Safety First**: Zero destructive actions (`wsl --unregister`, `docker system prune`, volume deletion) will be executed without explicit user authorization.
2. **Deterministic Durability**: Production configuration will eliminate silent fallback data loss.
3. **Enterprise HA**: SignalR Redis backplane and worker PEL recovery will guarantee horizontal scaling safety.
4. **Declarative Cloud-Native**: Helm charts will support dev, staging, and HA production topologies.
