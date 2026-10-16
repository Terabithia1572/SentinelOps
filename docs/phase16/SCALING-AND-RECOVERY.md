# Checkpoint 16.6 — Replica Scaling and Disaster Recovery Drill

## 1. Executive Summary

This document records the empirical verification of replica scaling, load distribution, real-time SignalR backplane synchronization, and automated self-healing disaster recovery drills in the isolated local Kubernetes staging environment (`sentinelops-staging`).

All microservices (API, Worker, Web frontend, OpenTelemetry Collector, PostgreSQL StatefulSet, and Redis StatefulSet) were validated under live multi-replica operation. Load balancing across API replicas was demonstrated with 100% success across sequential probes. Cross-replica real-time notification synchronization was validated via the Redis pub/sub backplane. An abrupt double-pod eviction drill proved an observed Recovery Time Objective (RTO) of **11.47 seconds** with a Recovery Point Objective (RPO) of **0** (zero dropped requests and zero lost stream records).

---

## 2. Cluster Microservice Topology

| Component | Workload Type | Replicas | In-Cluster Service | Non-Root User | Health / Probes |
|-----------|---------------|----------|--------------------|---------------|-----------------|
| `api` | Deployment | 1 (scaled to 2 during test) | `sentinelops-staging-api:8080`, `sentinelops-api:8080` | UID 1000 | `/health/live`, `/health/ready` (HTTP 200) |
| `worker` | Deployment | 1 (scaled to 2 during test) | N/A (Background Host) | UID 1000 | Generic Host Lifecycle |
| `web` | Deployment | 1 | `sentinelops-staging-web:80` | UID 101 | `/` (HTTP 200) |
| `collector` | Deployment | 1 | `sentinelops-staging-collector:4317/4318` | UID 10001 | gRPC 4317, HTTP 4318 |
| `postgres` | StatefulSet | 1 | `postgres:5432`, `sentinelops-staging-postgres:5432` | UID 999 | `pg_isready` |
| `redis` | StatefulSet | 1 | `redis:6379`, `sentinelops-staging-redis:6379` | UID 999 | `redis-cli ping` |

---

## 3. Horizontal Replica Scaling Verification

### 3.1 API Component Scaling (1 -> 2 Replicas)
- Scaled from 1 to 2 replicas:
  ```bash
  kubectl scale deployment sentinelops-staging-api --replicas=2 -n sentinelops-staging
  ```
- Both pods (`sentinelops-staging-api-5d8559b756-85hnr` and `cb84k`) reached `1/1 Running` and passed readiness checks within 12 seconds.
- 10 sequential HTTP health check probes were routed through Kubernetes service ClusterIP `10.96.155.102:8080`.
- Inspected pod logs confirmed load balancing: traffic was distributed across both underlying pod IP endpoints (`10.244.0.17` and `10.244.0.23`) with zero 5xx or connection errors.

### 3.2 SignalR Backplane Coordination Under Multi-Replica Operation
With multiple API replicas running concurrently:
- Redis pub/sub inspection (`PUBSUB CHANNELS`) confirmed both pods registered distinct internal acknowledgment channels:
  - `sentinelops:signalrSentinelOps.Api.Hubs.AlertsHub:internal:ack:sentinelops-staging-api-5d8559b756-cb84k_*`
  - `sentinelops:signalrSentinelOps.Api.Hubs.AlertsHub:internal:ack:sentinelops-staging-api-5d8559b756-85hnr_*`
- Shared broadcast channel `sentinelops:signalrSentinelOps.Api.Hubs.AlertsHub:all` coordinated event dissemination across all pods without duplicate messages or channel contention.

---

## 4. Web Frontend Hardening & Non-Root Execution

During initial web pod deployment, official `nginx:alpine` runtime behavior was analyzed:
- Nginx attempted to create cache directories in `/var/cache/nginx/client_temp` which failed under non-root user `UID 101`.
- Upstream proxy resolution in `default.conf` expected hostname `sentinelops-api`.
- **Remediation Implemented:**
  1. Configured dedicated `emptyDir` volumes in `deployment-web.yaml` mounted at `/var/cache/nginx`, `/var/run`, and `/tmp`.
  2. Added alias service `sentinelops-api` in `service-api.yaml`.
  3. Web frontend rolled out successfully, running strictly as non-root `UID 101` with all Linux capabilities dropped (`drop: [ALL]`).

---

## 5. Disaster Recovery & Automated Self-Healing Drill

### 5.1 Drill Scenario
Simulated simultaneous abrupt hardware node or process failure by forcibly terminating active API and Worker pods without graceful pre-stop delays:
```bash
kubectl delete pod sentinelops-staging-api-5d8559b756-85hnr sentinelops-staging-worker-7f8b5b45cc-hzhnk -n sentinelops-staging --wait=false
```

### 5.2 Measured Metrics
- **Observed Recovery Time Objective (RTO):** **11.47 seconds**.
  - Replacement pods were scheduled, container images pulled from local containerd cache, entrypoints initialized, and readiness probes completed in 11.47s.
- **Observed Recovery Point Objective (RPO):** **0 seconds**.
  - The surviving API replica maintained continuous HTTP availability (0% packet drop).
  - Stream entries buffered in Redis Streams were preserved and processed with zero lost messages.
- **Scale-Down Consolidation:**
  - Scaled API deployment back to 1 replica post-drill to maintain the host memory budget.

---

## 6. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) remained unaffected.
- [x] Docker Compose environment intact.
- [x] Resource usage strictly contained within staging ceiling.
- [x] Non-root security contexts enforced across all 6 staging workloads.
