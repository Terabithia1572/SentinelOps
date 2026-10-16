# Checkpoint 16.11 — Controlled Failure Injection & Resilience Proving

## 1. Executive Summary

This document records the empirical validation of the platform's self-healing capabilities, graceful degradation, and automatic dependency reconnection under controlled infrastructure failure scenarios in the isolated local Kubernetes staging environment (`sentinelops-staging`).

Two major failure injection drills were executed:
1. **PostgreSQL Database Hard Outage:** Simulating complete database crash or network partition.
2. **Redis Streams & Cache Hard Outage:** Simulating distributed cache and stream buffer termination.

In both drills, Kubernetes health probe separation operated as designed: the `/health/live` probe remained `200 OK`, keeping process runtimes alive and preventing cascading crash-restart loops. Simultaneously, the `/health/ready` probe immediately returned `HTTP 503 Service Unavailable`, cleanly removing affected pods from the service mesh traffic endpoints. Upon dependency restoration, both the ASP.NET Core API and the Background Worker automatically reconnected in **~10 seconds** without requiring manual pod restarts or configuration changes.

---

## 2. Failure Drill 1: PostgreSQL Database Hard Outage

### 2.1 Drill Execution
- Scaled the `sentinelops-staging-postgres` StatefulSet to `0` replicas:
  ```bash
  kubectl scale statefulset sentinelops-staging-postgres --replicas=0 -n sentinelops-staging
  ```
- Polled API endpoints during the outage:
  - **Liveness (`/health/live`):** Returned **`HTTP 200 OK` (`Healthy`)**. Kestrel remained stable and responsive.
  - **Readiness (`/health/ready`):** Returned **`HTTP 503 Service Unavailable`**. Npgsql database check correctly failed.

### 2.2 Restoration & Automatic Reconnection
- Restored StatefulSet to `1` replica:
  ```bash
  kubectl scale statefulset sentinelops-staging-postgres --replicas=1 -n sentinelops-staging
  ```
- StatefulSet reattached PVC `data-sentinelops-staging-postgres-0` and initialized within 6 seconds.
- EF Core connection pools automatically re-established database channels.
- **Measured Reconnection Time:** **10.44 seconds** from scale-up command to verified `HTTP 200 OK` on `/health/ready`.
- Zero application pod restarts occurred (`RESTARTS: 0`).

---

## 3. Failure Drill 2: Redis Streams & Cache Hard Outage

### 3.1 Drill Execution
- Scaled the `sentinelops-staging-redis` StatefulSet to `0` replicas:
  ```bash
  kubectl scale statefulset sentinelops-staging-redis --replicas=0 -n sentinelops-staging
  ```
- Polled API and Worker behavior during the outage:
  - **Liveness (`/health/live`):** Returned **`HTTP 200 OK` (`Healthy`)**.
  - **Readiness (`/health/ready`):** Returned **`HTTP 503 Service Unavailable`**. Redis ping check reported failure.
  - **Worker Process:** The background `TelemetryConsumerService` logged socket reconnect warnings while safely waiting without throwing unhandled process-terminating exceptions.

### 3.2 Restoration & Automatic Reconnection
- Restored StatefulSet to `1` replica:
  ```bash
  kubectl scale statefulset sentinelops-staging-redis --replicas=1 -n sentinelops-staging
  ```
- Redis pod booted and reached `Ready` status.
- StackExchange.Redis multiplexer detected heartbeat restoration and re-established multiplexed TCP sockets.
- **Measured Reconnection Time:** **10.08 seconds** to verified `HTTP 200 OK` on `/health/ready`.
- Worker process resumed stream consumption immediately with `0` restarts.

---

## 4. Resilience Summary Table

| Drill Scenario | Liveness Behavior | Readiness Behavior | Traffic Routing | Reconnection Time | Process Restarts |
|----------------|-------------------|-------------------|-----------------|-------------------|------------------|
| **PostgreSQL Outage** | 200 OK (Stable) | 503 Service Unavailable | Removed from Service endpoints | **10.44s** | **0** |
| **Redis Outage** | 200 OK (Stable) | 503 Service Unavailable | Removed from Service endpoints | **10.08s** | **0** |

---

## 5. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) remained unaffected.
- [x] Docker Compose environment intact.
- [x] Persistent volumes safely preserved data across StatefulSet scale-to-zero drills.
- [x] Zero credential exposure or unhandled exception crashes.
