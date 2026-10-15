# SentinelOps Phase 15 — Staging Deployment Hardening & Governance

## 1. Executive Summary

- **Status**: `PASS / READY FOR CONTROLLED STAGING`
- **Assessment Date**: October 15, 2026
- **Scope**: Hardening of deployment artifacts, separation of environment profiles (Development vs Staging vs Production), Kubernetes/Helm resource definitions, migration job lifecycle hooks, and ingress security.
- **Safety Directive**: No external staging cluster was provisioned or modified during this assessment.

---

## 2. Environment Profile Separation

| Feature | Local Development | Staging (`values-staging.example.yaml`) | Production (`values-production.example.yaml`) |
| :--- | :--- | :--- | :--- |
| **Orchestrator** | Docker Compose | Kubernetes / Helm | Kubernetes / Helm |
| **API Replicas** | 1 container | 2 replicas (HPA: 2–6) | 3 replicas (HPA: 3–15) |
| **Worker Replicas**| 1 container | 2 replicas | 3 replicas |
| **Web Replicas** | 1 container | 2 replicas | 3 replicas |
| **Migrations** | Startup auto-migrate | Pre-upgrade Kubernetes Job hook | Pre-upgrade Kubernetes Job hook |
| **Secrets** | Local `.env` / fallbacks | External Secret Manager / Vault | External Secret Manager / Vault |
| **TLS** | HTTP on localhost | `cert-manager` (Let's Encrypt Staging) | `cert-manager` (Let's Encrypt Prod) |
| **Logging Level** | `Debug` / `Information` | `Information` | `Information` / `Warning` |

---

## 3. Deployment Artifact Review & Hardening

### 3.1 Dockerfiles
- Multi-stage builds (`build` &rarr; `publish` &rarr; `final`).
- Non-root execution: Container processes run under unprivileged user `appuser` (UID 1000) rather than `root`.
- Minimal base images: .NET 10 ASP.NET Core chiseled/alpine runtime and Nginx unprivileged Alpine for web frontend.

### 3.2 Helm Chart & Kubernetes Architecture
The platform Helm chart in `deploy/helm/sentinelops/` includes enterprise production-grade manifests:
1. **Migration Job (`templates/job-migration.yaml`)**:
   - Executes `dotnet SentinelOps.Api.dll --migrate-database-only` as a Kubernetes Batch Job.
   - Annotated with `"helm.sh/hook": pre-install,pre-upgrade` and `"helm.sh/hook-weight": "1"`.
   - Guarantees migrations complete cleanly before new API pods are scheduled.
2. **High Availability & Autoscaling**:
   - `HorizontalPodAutoscaler` (`hpa-api.yaml`) scales API pods dynamically on CPU/Memory thresholds.
   - `PodDisruptionBudget` (`pdb-api.yaml`) prevents service disruption during cluster node maintenance.
3. **Network Isolation (`templates/networkpolicy.yaml`)**:
   - Enforces default-deny ingress within the namespace.
   - Explicitly allows traffic from Ingress controller to Web and API, and from API and Worker to PostgreSQL and Redis.

---

## 4. Tool Execution & Validation Status

Per instructions, local tool availability was evaluated:
- **`helm` CLI**: Not installed on host Windows PATH.
- **`kubectl` CLI**: Client version `v1.34.1` present; local cluster daemon inactive.
- **Reporting**:
  - Local Helm Chart Lint / Template Execution: **`NOT EXECUTED (Tool unavailable on local host)`**.
  - CI Helm Validation: Executed automatically in GitHub Actions CI via `azure/setup-helm@v4` (`helm lint` & `helm template`).

---

## 5. Controlled Staging Deployment Checklist

Prior to staging deployment, SREs must confirm:
1. [ ] Kubernetes secret `sentinelops-staging-secrets` provisioned with non-default database, redis, and JWT credentials.
2. [ ] Ingress DNS records mapped to staging cluster ingress controller IP.
3. [ ] Pre-migration logical database snapshot captured.
4. [ ] Migration Job hook verified to exit with code 0 before API rollout commences.
5. [ ] Health check probes (`/health/live`, `/health/ready`) responding `200 OK`.
