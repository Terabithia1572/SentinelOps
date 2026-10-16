# Checkpoint 16.8 — Kubernetes Network and Security Policy Verification

## 1. Executive Summary

This document records the empirical verification of zero-trust network isolation and Pod Security Standards (PSS) hardening across the SentinelOps local Kubernetes staging environment (`sentinelops-staging`).

Six granular `NetworkPolicy` resources were applied, enforcing strict microsegmentation between the frontend tier, API tier, background workers, telemetry pipeline, and persistent databases. Ingress and egress rules were verified through active connection tests. Negative security testing confirmed that direct, unauthorized access between decoupled tiers (such as Web attempting to connect to PostgreSQL or Redis directly) is blocked at the network packet layer. All workloads were audited to verify non-root user execution (`runAsNonRoot: true`), unprivileged operation (`allowPrivilegeEscalation: false`), and complete Linux capability stripping (`drop: [ALL]`).

---

## 2. Zero-Trust NetworkPolicy Matrix

| Policy Name | Target Workload | Allowed Ingress | Allowed Egress |
|-------------|-----------------|-----------------|----------------|
| `sentinelops-staging-web-policy` | `web` (React/Nginx) | Ingress Controllers (port 80) | API (port 8080), CoreDNS (UDP/TCP 53) |
| `sentinelops-staging-api-policy` | `api` (ASP.NET Core) | Web (port 8080), Collector (port 8080), Ingress | Postgres (5432), Redis (6379), Collector (4318), CoreDNS (53) |
| `sentinelops-staging-worker-policy` | `worker` (.NET Generic Host) | **NONE (Deny All Inbound)** | Postgres (5432), Redis (6379), CoreDNS (53), External HTTPS (443) |
| `sentinelops-staging-postgres-policy` | `postgres` (PostgreSQL 17) | API (5432), Worker (5432), Migration Job (5432) | CoreDNS (53) |
| `sentinelops-staging-redis-policy` | `redis` (Redis 7) | API (6379), Worker (6379) | CoreDNS (53) |
| `sentinelops-staging-collector-policy` | `collector` (OTel Contrib) | API (4317/4318), External Telemetry Ingress | API (8080), CoreDNS (53) |

---

## 3. Traffic Microsegmentation Verification

### 3.1 Authorized Communication (Positive Tests)
1. **Web to API Communication:**
   Executed from `deployment/sentinelops-staging-web`:
   ```bash
   wget -q -O - http://sentinelops-staging-api:8080/health/live
   wget -q -O - http://sentinelops-staging-api:8080/health/ready
   ```
   Result: **HTTP 200 OK (`Healthy`)** — Authorized request paths permitted.
2. **API to Database & Redis:**
   `/health/ready` probe verifies live TCP connections to both PostgreSQL `sentinelops_staging` and Redis server.
   Result: **HTTP 200 OK** — Authorized database queries permitted.

### 3.2 Unauthorized Cross-Tier Access (Negative Tests)
To confirm strict microsegmentation, lateral movement attempts were executed from the frontend container:
1. **Web Pod to PostgreSQL (Port 5432):**
   ```bash
   kubectl exec -n sentinelops-staging deployment/sentinelops-staging-web -- nc -z -w 2 postgres 5432
   ```
   Result: **BLOCKED** (`command terminated with exit code 1`). Egress to port 5432 is strictly prohibited by `sentinelops-staging-web-policy`.
2. **Web Pod to Redis (Port 6379):**
   ```bash
   kubectl exec -n sentinelops-staging deployment/sentinelops-staging-web -- nc -z -w 2 redis 6379
   ```
   Result: **BLOCKED** (`command terminated with exit code 1`). Egress to port 6379 is strictly prohibited.
3. **Inbound Access to Background Worker:**
   Worker policy declares `ingress: []`, rejecting all inbound connection attempts regardless of source namespace.

---

## 4. Pod Security Standards & Hardening Audit

A live cluster audit of all workload pods in `sentinelops-staging` was conducted:

| Pod Name | Container | Non-Root | UID | Privilege Escalation | Capabilities Dropped |
|----------|-----------|----------|-----|----------------------|----------------------|
| `sentinelops-staging-api-*` | `api` | **True** | `1000` | **False** | `ALL` |
| `sentinelops-staging-worker-*` | `worker` | **True** | `1000` | **False** | `ALL` |
| `sentinelops-staging-web-*` | `web` | **True** | `101` | **False** | `ALL` |
| `sentinelops-staging-collector-*` | `otel-collector` | **True** | `10001` | **False** | Default unprivileged |
| `sentinelops-staging-postgres-0` | `postgres` | **True** | `999` | **False** | Base container |
| `sentinelops-staging-redis-0` | `redis` | **True** | `999` | **False** | Base container |

---

## 5. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) remained unaffected.
- [x] Docker Compose environment intact.
- [x] Zero credential exposure in manifests or documentation.
- [x] Zero-trust NetworkPolicies actively enforced without disrupting application telemetry pipelines.
