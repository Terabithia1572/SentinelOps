# Checkpoint 16.12 — Automated Kubernetes Integration Test Suite

## 1. Executive Summary

This document describes the automated Kubernetes integration test suite developed for the SentinelOps platform in local Kubernetes staging (`sentinelops-staging` cluster on `kind`).

The test suite is encapsulated in an idempotent, automated test runner (`tests/k8s/test-staging-cluster.ps1`). It queries the live Kubernetes API, pod specifications, PersistentVolumeClaims, service endpoints, in-cluster network probes, external NodePort ingestion, Redis Streams consumer group state, Pod Security Standards compliance, and zero-trust NetworkPolicy enforcement.

All 10 integration test cases passed with a 100% success rate (10/10 PASS, 0 failures, exit code 0).

---

## 2. Test Architecture and Test Cases

The test script operates non-destructively against the active Kubernetes cluster context `kind-sentinelops-staging` in namespace `sentinelops-staging`.

| Test ID | Category | Description | Verification Method | Result |
| :--- | :--- | :--- | :--- | :---: |
| **K8S-01** | Control-Plane | Cluster node exists and is in Ready state | Live k8s API query (`kubectl get nodes`) checking condition `Ready: True` | **PASS** |
| **K8S-02** | Workload Health | All 6 microservices running and ready | Evaluates container status `ready: true` across `api`, `worker`, `web`, `collector`, `postgres`, `redis` | **PASS** |
| **K8S-03** | Persistent Storage | StatefulSet PVCs are Bound | Inspects volume claim status `phase: Bound` for PostgreSQL and Redis volume claims | **PASS** |
| **K8S-04** | Service Discovery | Kubernetes Services have registered endpoints | Validates that endpoints for API, DB, Redis, and Collector contain active pod IPs | **PASS** |
| **K8S-05** | Liveness Probing | API Liveness endpoint returns 200 OK | In-cluster HTTP GET to `http://sentinelops-staging-api:8080/health/live` returning `Healthy` | **PASS** |
| **K8S-06** | Readiness Probing | API Readiness endpoint verifies dependencies | In-cluster HTTP GET to `http://sentinelops-staging-api:8080/health/ready` confirming DB and Redis | **PASS** |
| **K8S-07** | Ingestion Pipeline | OTLP Collector responds HTTP 200 on port 14318 | External HTTP POST of valid OTLP JSON log payload to NodePort mapped host port `14318` | **PASS** |
| **K8S-08** | Stream Buffering | Redis consumer group active with 0 pending lag | Queries `XINFO GROUPS` and `XPENDING` on `sentinelops:stream:logs` verifying `sentinelops-worker-group` | **PASS** |
| **K8S-09** | Pod Security | Non-root execution and privilege drop | Validates `runAsNonRoot: true`, `allowPrivilegeEscalation: false`, and `capabilities: drop: [ALL]` | **PASS** |
| **K8S-10** | Zero-Trust Security | NetworkPolicy blocks unauthorized direct DB access | In-cluster negative test: `sentinelops-staging-web` attempting direct TCP connect to PostgreSQL fails | **PASS** |

---

## 3. Execution Log and Raw Evidence

Execution captured on 2026-10-16:

```text
================================================================
   SENTINELOPS - KUBERNETES STAGING INTEGRATION TEST SUITE
================================================================
Namespace: sentinelops-staging | Context: kind-sentinelops-staging
Execution Time: 2026-10-16 22:05:48 UTC

[K8S-01] Cluster control-plane node exists and is in Ready state ... PASS
[K8S-02] All 6 microservices are running and ready ... PASS
[K8S-03] StatefulSet PVCs for PostgreSQL and Redis are Bound ... PASS
[K8S-04] Kubernetes Services have registered endpoints ... PASS
[K8S-05] API Liveness endpoint health-live returns 200 OK ... PASS
[K8S-06] API Readiness endpoint health-ready verifies DB and Redis health ... PASS
[K8S-07] OTLP Collector responds HTTP 200 on ingestion port 14318 ... PASS
[K8S-08] Redis Streams consumer group active with zero pending lag ... PASS
[K8S-09] All workloads execute as non-root with allowPrivilegeEscalation false ... PASS
[K8S-10] NetworkPolicy blocks unauthorized frontend direct access to database ... PASS

================================================================
   TEST RESULTS SUMMARY
================================================================
Total Tests: 10 | Passed: 10 | Failed: 0
VERDICT: LOCAL KUBERNETES STAGING INTEGRATION SUITE PASSED (100 Percent)
```

---

## 4. Verification and Compliance

- **Reproducibility:** The script can be executed at any time via:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\tests\k8s\test-staging-cluster.ps1
  ```
- **Exit Code Integration:** Returns code `0` on 100% pass, enabling integration into automated CI/CD staging verification pipelines.
- **Isolation Preserved:** All tests operated exclusively against the staging cluster without affecting the Docker Compose environment or host workloads.
