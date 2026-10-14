# SentinelOps — Kubernetes Cloud-Native Architecture Specification

## 1. Overview & Cloud-Native Topology

Phase 10 transitions SentinelOps from single-host Docker Compose environments into an enterprise-ready, container-orchestrated architecture suitable for Kubernetes (EKS, GKE, AKS, or on-premises k8s/k3s).

The architecture adheres strictly to Twelve-Factor App principles, decoupled state, strict security contexts, and horizontal scalability.

```
                           +---------------------------------------+
                           |           Kubernetes Ingress          |
                           |   (Ingress-Nginx / AWS ALB / Traefik) |
                           +-------------------+-------------------+
                                               |
             +---------------------------------+---------------------------------+
             |                                 |                                 |
             v (HTTP / HTTPS)                  v (WebSocket / HTTP)              v (OTLP gRPC/HTTP)
   +-------------------+             +-------------------+             +-------------------+
   |  sentinelops-web  |             |  sentinelops-api  |             |   otel-collector  |
   | (React 19 SPA on  |             | (ASP.NET Core 10) |             |  (OTel Collector) |
   |    Nginx 1.27)    |             |    (HPA 2 - 10)   |             +---------+---------+
   +-------------------+             +---------+---------+                       |
                                               |                                 |
                                               | (Enqueue Telemetry)             |
                                               +----------------+                |
                                                                |                |
                                                                v                v
                                               +---------------------------------+
                                               |     Redis Cluster / Sentinel    |
                                               | (Streams Buffer + Pub/Sub Hub)  |
                                               +----------------+----------------+
                                                                |
                                                                v (Dequeue Telemetry Stream)
                                                       +-------------------+
                                                       | sentinelops-worker|
                                                       | (Background Ops & |
                                                       |  Retention / PEL) |
                                                       +---------+---------+
                                                                 |
                                                                 v (Batch Insert & Queries)
                                                       +-------------------+
                                                       |    PostgreSQL     |
                                                       | (Cloud RDS/Managed|
                                                       | or StatefulSet)   |
                                                       +-------------------+
```

---

## 2. Core Workloads & Component Specifications

### 2.1 API Service (`sentinelops-api`)
- **Workload Type**: `Deployment`
- **Replicas**: 2 (Base) with Horizontal Pod Autoscaler (`minReplicas: 2`, `maxReplicas: 10`).
- **Target Utilization**: 75% CPU utilization, 80% Memory utilization.
- **Pod Disruption Budget (PDB)**: `minAvailable: 1` ensures zero-downtime rolling updates.
- **Probes**:
  - `livenessProbe`: `GET /health/live` (initial delay: 15s, period: 10s, failure threshold: 3).
  - `readinessProbe`: `GET /health/ready` (initial delay: 10s, period: 5s, failure threshold: 2). Validates PostgreSQL, Redis, and durability buffer availability.
- **Scale-Out Strategy**: Fully stateless. Real-time SignalR notifications cross pod boundaries via Redis pub/sub backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`).

### 2.2 Background Telemetry Worker (`sentinelops-worker`)
- **Workload Type**: `Deployment`
- **Replicas**: 2 (High Availability competing consumers on Redis Streams).
- **Consumer Group Architecture**:
  - Stream: `telemetry:stream`
  - Consumer Group: `sentinelops:workers`
  - Competing consumer mechanics with idempotent batch persistence and pending entry list (PEL) auto-recovery.
- **Probes**:
  - `livenessProbe` & `readinessProbe` checking worker loop health and Redis stream responsiveness.

### 2.3 Web Dashboard (`sentinelops-web`)
- **Workload Type**: `Deployment`
- **Replicas**: 2
- **Image**: Nginx 1.27 Alpine serving compiled React 19 / Vite SPA bundle.
- **Probes**: `GET /` on port 80.

### 2.4 OpenTelemetry Collector (`sentinelops-collector`)
- **Workload Type**: `Deployment` (or `DaemonSet` in node-agent mode).
- **Receivers**: OTLP gRPC (`4317`), OTLP HTTP (`4318`).
- **Exporters**: OTLP HTTP forwarder to `sentinelops-api` ingestion endpoints (`/api/telemetry/v1/*`) or direct Redis pipeline.

### 2.5 In-Cluster Redis (`sentinelops-redis`)
- **Workload Type**: `StatefulSet` (when using in-cluster Redis for Dev/Staging).
- **Storage**: PersistentVolumeClaim (PVC) with `ReadWriteOnce`, `dir /data`, `appendonly yes` (AOF + RDB snapshotting for durable stream retention).
- **Production Recommendation**: External managed Redis (e.g., AWS ElastiCache, Azure Cache for Redis, GCP Memorystore) with TLS and Multi-AZ replication.

### 2.6 Database Migration Runner (`sentinelops-migration`)
- **Workload Type**: `Job`
- **Execution Hook**: Runs before new API deployment pods start (Helm `pre-install,pre-upgrade` hook with hook-weight 1).
- **Rationale**: Completely prevents migration race conditions and locks on PostgreSQL `__EFMigrationsHistory` that occur when multiple API replica pods start simultaneously.

---

## 3. Ingress, TLS & Traffic Routing

### Ingress Rules & Path Routing
| Host / Path | Target Service | Port | Protocol / Annotations |
| :--- | :--- | :--- | :--- |
| `sentinelops.example.com/api` | `sentinelops-api` | 8080 | HTTP REST API |
| `sentinelops.example.com/telemetry-hub` | `sentinelops-api` | 8080 | WebSocket (`nginx.ingress.kubernetes.io/proxy-read-timeout: 3600`) |
| `sentinelops.example.com/health` | `sentinelops-api` | 8080 | HTTP Health probes |
| `sentinelops.example.com/` | `sentinelops-web` | 80 | HTTP Static Web SPA |
| `collector.sentinelops.example.com/` | `sentinelops-collector` | 4318 | OTLP HTTP Ingestion |

---

## 4. Network Security & Isolation (NetworkPolicy)

Least-privilege network policies restrict pod communication:
- **`sentinelops-api`**:
  - Ingress: Only from Ingress Controller on port 8080.
  - Egress: Allowed to PostgreSQL (5432), Redis (6379), and DNS (53).
- **`sentinelops-worker`**:
  - Ingress: Denied all (workers only consume outbound streams).
  - Egress: Allowed to PostgreSQL (5432), Redis (6379), and DNS (53).
- **`sentinelops-redis`**:
  - Ingress: Only from `sentinelops-api` and `sentinelops-worker` pods on port 6379.
  - Egress: DNS (53) only.
- **`sentinelops-web`**:
  - Ingress: From Ingress Controller on port 80.
  - Egress: None (pure static asset server).

---

## 5. Security Contexts & Pod Hardening

All containers enforce strict Kubernetes security contexts:
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  runAsGroup: 1000
  fsGroup: 1000
containerSecurityContext:
  allowPrivilegeEscalation: false
  readOnlyRootFilesystem: false # Temporary logs/buffers in tmpfs
  capabilities:
    drop:
      - ALL
```
Service accounts set `automountServiceAccountToken: false` to prevent pod token theft.
