# SentinelOps — Kubernetes Platform Security & Hardening Audit

## 1. Compliance Baseline & Hardening Overview

Phase 10 applies the **NSA/CISA Kubernetes Hardening Guidance (v1.2)** and **CIS Kubernetes Benchmark** standards to all container images, Kubernetes manifests, and Helm deployment templates.

| Security Control | Implementation | Verification Status |
| :--- | :--- | :--- |
| **Non-Root Execution** | `runAsNonRoot: true`, UID/GID `1000` (API/Worker), UID/GID `101` (Nginx) | **PASS** — Enforced in PodSecurityContext |
| **Privilege Escalation** | `allowPrivilegeEscalation: false` | **PASS** — Enforced in ContainerSecurityContext |
| **Capability Dropping** | `capabilities: { drop: ["ALL"] }` | **PASS** — All Linux capabilities stripped |
| **Seccomp Profile** | `seccompProfile: { type: RuntimeDefault }` | **PASS** — Enforced across API, Worker, Web |
| **ServiceAccount Hardening** | `automountServiceAccountToken: false` | **PASS** — Prevents pod token extraction |
| **Network Isolation** | Strict Kubernetes `NetworkPolicy` | **PASS** — Tier-isolated ingress & egress |
| **Secret Management** | Kubernetes `Secret` / `SecretKeyRef` | **PASS** — Zero hardcoded credentials |
| **Reverse Proxy Security** | `UseForwardedHeaders` + IP rate limiting | **PASS** — Real client IP extraction |

---

## 2. Pod & Container Security Context Matrix

### 2.1 API Service (`sentinelops-api`)
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  runAsGroup: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault
containers:
  - name: api
    securityContext:
      allowPrivilegeEscalation: false
      capabilities:
        drop:
          - ALL
```

### 2.2 Worker Service (`sentinelops-worker`)
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  runAsGroup: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault
containers:
  - name: worker
    securityContext:
      allowPrivilegeEscalation: false
      capabilities:
        drop:
          - ALL
```

### 2.3 Web Dashboard (`sentinelops-web`)
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 101       # nginx unprivileged user
  runAsGroup: 101
  fsGroup: 101
  seccompProfile:
    type: RuntimeDefault
containers:
  - name: web
    securityContext:
      allowPrivilegeEscalation: false
      capabilities:
        drop:
          - ALL
```

---

## 3. Network Isolation & Micro-Segmentation (NetworkPolicy)

The deployment includes `deploy/helm/sentinelops/templates/networkpolicy.yaml` enforcing zero-trust traffic boundaries:

```
[ Ingress Controller ]
        |
        +──> sentinelops-web:80       (HTTP static bundle)
        +──> sentinelops-api:8080     (REST API & SignalR)
        +──> otel-collector:4318      (OTLP ingestion)

[ sentinelops-api ]
        |
        +──> PostgreSQL:5432          (Entity queries & mutations)
        +──> Redis:6379               (Streams buffer & SignalR pub/sub)
        +──> DNS:53 (UDP/TCP)

[ sentinelops-worker ]
        |
        +──> PostgreSQL:5432          (Telemetry bulk inserts & retention)
        +──> Redis:6379               (Streams consumer group & PEL)
        +──> DNS:53 (UDP/TCP)
        (Worker rejects all inbound traffic)

[ sentinelops-redis ]
        |
        +<── Allowed ONLY from sentinelops-api and sentinelops-worker
```

---

## 4. Secret Management & Rotation Playbooks

### 4.1 Production Secrets Separation
All database passwords, Redis credentials, and JWT signing keys are injected exclusively through Kubernetes `SecretKeyRef` objects.

```bash
# Example Kubernetes Secret provisioning
kubectl create secret generic sentinelops-secrets \
  --namespace sentinelops \
  --from-literal=postgres-password="$PROD_PG_PASS" \
  --from-literal=redis-password="$PROD_REDIS_PASS" \
  --from-literal=jwt-key="$PROD_JWT_SECRET_KEY"
```

### 4.2 Secret Rotation Procedures
1. **JWT Key Rotation**:
   - Update `JwtSettings:Key` in Secret.
   - Deploy new API pods via rolling update.
   - Existing active JWTs expire naturally within configured lifetime (`JwtSettings:ExpirationMinutes = 60`).
2. **Database Password Rotation**:
   - Create new database role password in PostgreSQL.
   - Update `sentinelops-secrets` in Kubernetes.
   - Execute rolling restart of API and Worker deployments (`kubectl rollout restart deployment/sentinelops-api -n sentinelops`).
   - Revoke old database credentials after all pods report healthy.

---

## 5. Reverse Proxy & Reverse-Proxy Header Hardening

In `src/SentinelOps.Api/Program.cs`:
- `ForwardedHeadersOptions` is configured with `KnownIPNetworks.Clear()` and `KnownProxies.Clear()` to safely trust Kubernetes Ingress reverse proxies in private cluster VPCs.
- `app.UseForwardedHeaders()` is placed at the head of the ASP.NET Core middleware pipeline.
- Rate limiters and audit loggers extract the true client source IP address (`context.Connection.RemoteIpAddress`) rather than internal proxy pod IPs.
- HTTPS redirection and HSTS headers are preserved across TLS-terminating Ingress boundaries.
