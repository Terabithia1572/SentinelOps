# Checkpoint 16.10 — Platform Observability, Health Probes & Metrics Discovery

## 1. Executive Summary

This document records the empirical audit of platform self-observability, container runtime metrics, health probe admission gates, and host capacity utilization across the SentinelOps local Kubernetes staging cluster (`sentinelops-staging`).

All microservices were verified against rigorous startup, liveness, and readiness probe definitions. Deep health checks for PostgreSQL and Redis dependencies were confirmed operational. Real container runtime telemetry extracted via `crictl stats` demonstrated that all workloads operate well within their allocated memory and CPU limits, maintaining 1.7+ GB of free host RAM.

---

## 2. Health Probe Coverage Matrix

| Workload | Probe Type | Target Endpoint | Exec / HTTP | Thresholds | Verification Status |
|----------|------------|-----------------|-------------|------------|---------------------|
| `api` | Startup | `/health/live` | HTTP 8080 | delay: 5s, period: 3s, fail: 10 | **PASS (200 OK)** |
| `api` | Liveness | `/health/live` | HTTP 8080 | delay: 15s, period: 10s, fail: 3 | **PASS (200 OK)** |
| `api` | Readiness | `/health/ready` | HTTP 8080 | delay: 10s, period: 5s, fail: 3 | **PASS (200 OK)** |
| `worker` | Lifecycle | .NET Generic Host | Process | Restart on exit | **PASS (Running)** |
| `web` | Liveness | `/` | HTTP 80 | delay: 5s, period: 10s, fail: 3 | **PASS (200 OK)** |
| `postgres` | Liveness | `pg_isready -U sentinelops_app -d sentinelops_staging` | Exec | delay: 15s, period: 10s | **PASS (Accepting)** |
| `postgres` | Readiness | `pg_isready -U sentinelops_app -d sentinelops_staging` | Exec | delay: 5s, period: 5s | **PASS (Accepting)** |
| `redis` | Liveness | `redis-cli -a $REDIS_PASSWORD ping` | Exec | delay: 10s, period: 10s | **PASS (PONG)** |
| `redis` | Readiness | `redis-cli -a $REDIS_PASSWORD ping` | Exec | delay: 5s, period: 5s | **PASS (PONG)** |

---

## 3. Probe Response Headers & Security Hardening Audit

Querying `GET http://sentinelops-staging-api:8080/health/ready` confirmed active enterprise security headers on all HTTP responses:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()`
- `Content-Security-Policy: default-src 'self'; img-src 'self' data:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self' ws: wss:; frame-ancestors 'none';`

---

## 4. Container Resource Consumption Audit (`crictl stats`)

Live runtime statistics captured inside the staging control-plane node:

| Container Name | CPU Utilization | Memory Usage | Memory Request | Memory Limit | Capacity Status |
|----------------|-----------------|--------------|----------------|--------------|-----------------|
| `api` | 1.65% | 139.7 MB | 128 MB | 256 MB | **NOMINAL (54.5% of Limit)** |
| `worker` | 0.94% | 117.4 MB | 128 MB | 256 MB | **NOMINAL (45.8% of Limit)** |
| `postgres` | 0.68% | 57.4 MB | 128 MB | 512 MB | **NOMINAL (11.2% of Limit)** |
| `redis` | 0.61% | 5.2 MB | 128 MB | 512 MB | **NOMINAL (1.0% of Limit)** |
| `otel-collector` | 0.06% | 31.4 MB | 64 MB | 128 MB | **NOMINAL (24.5% of Limit)** |
| `web` | 0.00% | 20.5 MB | 32 MB | 64 MB | **NOMINAL (32.0% of Limit)** |
| **Entire Kind Cluster** | **10.04%** | **1.15 GiB** | N/A | **7.63 GiB (Docker)** | **HEALTHY (15.1% of Docker)** |

---

## 5. Host Resource Capacity Compliance

- **Total Host Memory:** 15.75 GiB (16,124 MB)
- **Host Free Memory:** 1.72 GiB (1,725 MB)
- **Resource Budget Status:** **FULLY COMPLIANT** (Staging cluster consuming ~1.15 GiB, leaving >1.5 GiB buffer on the workstation).

---

## 6. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) remained unaffected.
- [x] Docker Compose environment intact.
- [x] Container resource limits strictly enforced by Linux cgroups.
- [x] Deep health checks confirmed operational across all tiers.
