# Checkpoint 16.7 — Rolling Deployments, Health Probes & Automated Rollback

## 1. Executive Summary

This document records the empirical verification of zero-downtime rolling deployments, health probe admission gates, controlled failure containment, and automated rollback in the isolated local Kubernetes staging environment (`sentinelops-staging`).

Rolling deployments configured with `maxSurge: 1` and `maxUnavailable: 0` were tested under active HTTP traffic. Updating from `sentinelops-api:1.0.0-staging` to `1.0.1-staging` demonstrated 100% request continuity across 15 continuous HTTP probes with zero dropped packets. An intentional failure drill with a corrupted/nonexistent container image (`sentinelops-api:invalid-broken-tag-999`) proved that Kubernetes health gates prevented traffic interruption by keeping existing pods active. Automated rollback via `kubectl rollout undo` restored the cluster to a healthy state without manual container surgery.

---

## 2. Rolling Update Strategy & Parameterization

In `deploy/helm/sentinelops/templates/deployment-api.yaml`:
```yaml
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
```

- **`maxSurge: 1`:** Allows Kubernetes to provision one additional pod before terminating existing pods.
- **`maxUnavailable: 0`:** Guarantees that no existing healthy pods are terminated until the newly spawned replacement pod passes its startup probe, liveness probe, and readiness probe.

---

## 3. Zero-Downtime Rolling Update Drill (1.0.0-staging -> 1.0.1-staging)

### 3.1 Procedure
1. Local staging image was tagged and loaded into kind:
   ```bash
   docker tag sentinelops-api:1.0.0-staging sentinelops-api:1.0.1-staging
   kind load docker-image sentinelops-api:1.0.1-staging --name sentinelops-staging
   ```
2. Triggered rolling update:
   ```bash
   kubectl set image deployment/sentinelops-staging-api api=sentinelops-api:1.0.1-staging -n sentinelops-staging
   ```
3. Executed 15 continuous HTTP health check probes via the in-cluster service during the rollout.

### 3.2 Observed Results
- **Probe Success Rate:** 15 / 15 (100.0%).
- **Failures / Drops:** 0 (0.0%).
- **Replica Lifecycle:**
  1. Pod `sentinelops-staging-api-77bc8b4ccc-kr7hl` created.
  2. Startup probe completed after 5 seconds.
  3. Readiness probe marked pod `Ready` (`1/1 Running`).
  4. Kubernetes Service endpoints updated to include new pod IP.
  5. Old pod `5d8559b756-cb84k` gracefully terminated after connections drained.

---

## 4. Controlled Failure Injection & Automated Rollback Drill

### 4.1 Failure Scenario
Simulated a failed deployment by setting an invalid/nonexistent image tag:
```bash
kubectl set image deployment/sentinelops-staging-api api=sentinelops-api:invalid-broken-tag-999 -n sentinelops-staging
```

### 4.2 Traffic Protection Verification
- New pod `sentinelops-staging-api-7c59d7f97c-5f6q2` immediately entered `ErrImagePull` / `ImagePullBackOff`.
- Because `maxUnavailable: 0` was enforced, Kubernetes refused to terminate the existing active replica (`sentinelops-staging-api-77bc8b4ccc-kr7hl`).
- Active HTTP health checks to `http://sentinelops-staging-api:8080/health/live` returned `Healthy` without any downtime.

### 4.3 Automated Rollback
Executed rollback to the previous known good revision:
```bash
kubectl rollout undo deployment/sentinelops-staging-api -n sentinelops-staging
kubectl rollout status deployment/sentinelops-staging-api -n sentinelops-staging --timeout=60s
```
- Rollback succeeded within 4 seconds.
- The failing `ErrImagePull` pod was immediately evicted and terminated.
- Final deployment returned cleanly to `1/1 Running`.
- Image was restored to standard baseline `sentinelops-api:1.0.0-staging`.

---

## 5. Health Probe Gate Architecture

| Probe | Path | Port | Thresholds | Role in Lifecycle |
|-------|------|------|------------|-------------------|
| **Startup** | `/health/live` | 8080 | initialDelay: 5s, period: 3s, failureThreshold: 10 | Protects cold .NET JIT and DB connection pool initialization from premature liveness kills. |
| **Liveness** | `/health/live` | 8080 | initialDelay: 15s, period: 10s, failureThreshold: 3 | Automatically restarts hung or deadlocked threads. |
| **Readiness** | `/health/ready` | 8080 | initialDelay: 10s, period: 5s, failureThreshold: 3 | Removes pod from Service endpoints during DB reconnects or temporary degraded health without killing process. |

---

## 6. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) remained unaffected.
- [x] SentinelOps Docker Compose environment intact.
- [x] Zero external registry pushes or image pollution.
- [x] Zero downtime during rollout and failure containment drills.
