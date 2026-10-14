# SentinelOps — Release Engineering, GitOps & Deployment Playbooks

## 1. CI/CD Architecture Overview

SentinelOps enforces automated continuous integration and continuous delivery through GitHub Actions pipelines:

```
[ Developer / Feature Branch ]
               |
               v
      [ Pull Request / Push ]
               |
               +─────────────────────────────────────────────────────────────+
               |                    GitHub Actions CI                        |
               |                                                             |
               |  +─────────────────────────+  +──────────────────────────+  |
               |  |   Backend Build & Test  |  |   Frontend Build & Test  |  |
               |  |  (.NET 10, EF Core,     |  |  (React 19, Vitest,      |  |
               |  |   114 Integration Tests)|  |   TypeScript Typecheck)  |  |
               |  +────────────+────────────+  +────────────+─────────────+  |
               |               |                            |                |
               |               +─────────────+──────────────+                |
               |                             |                               |
               |                             v                               |
               |  +───────────────────────────────────────────────────────+  |
               |  |             Kubernetes & Helm Validation              |  |
               |  |      - `helm lint deploy/helm/sentinelops`            |  |
               |  |      - `helm template` (development & production)     |  |
               |  +──────────────────────────+────────────────────────────+  |
               |                             |                               |
               |                             v                               |
               |  +───────────────────────────────────────────────────────+  |
               |  |             Security & Vulnerability Scan             |  |
               |  |      - `dotnet list package --vulnerable`             |  |
               |  |      - `npm audit --audit-level=high`                 |  |
               |  +───────────────────────────────────────────────────────+  |
               +─────────────────────────────+───────────────────────────────+
                                             | (On Release Tag `v*.*.*`)
                                             v
                           [ Container Registry & Helm Repo ]
                                             |
                                             v
                       [ GitOps Engine: ArgoCD / Flux / Helm ]
                                             |
                           +─────────────────+─────────────────+
                           |                                   |
                           v                                   v
                  [ Staging Cluster ]                 [ Production Cluster ]
```

---

## 2. Environments & Release Matrix

| Dimension | Development / Local | Staging | Production |
| :--- | :--- | :--- | :--- |
| **Orchestrator** | Minikube / k3d / Docker Compose | Managed Kubernetes (EKS/GKE/AKS) | Multi-AZ Cloud Kubernetes |
| **Values File** | `values-development.yaml` | `values-staging.yaml` | `values-production.example.yaml` |
| **API Replicas** | 1 | 2 | 2 – 10 (HPA autoscale) |
| **Worker Replicas** | 1 | 2 | 2 (competing consumers) |
| **PostgreSQL** | In-cluster / Local container | Cloud RDS (Single-AZ) | Cloud RDS Multi-AZ + Replicas |
| **Redis** | In-cluster StatefulSet | Managed Redis | Managed Redis Cluster / Sentinel |
| **Durability Policy** | Fail-open (fallback in-memory) | Fail-closed (`RequireDurableStorage: true`) | Fail-closed (`RequireDurableStorage: true`) |
| **Ingress TLS** | HTTP / Self-Signed | Let's Encrypt Staging | Production TLS via cert-manager |

---

## 3. Deployment Playbooks

### 3.1 Pre-Deployment Migration Strategy
Database migrations are automatically managed via Helm pre-upgrade hooks (`deploy/helm/sentinelops/templates/job-migration.yaml`).
- Helm deploys and monitors the migration container until completion.
- If the migration job fails, Helm terminates the release process before any API or Worker pods are updated.

### 3.2 Helm Deployment Execution
```bash
# 1. Verify chart syntax and render manifests
helm lint deploy/helm/sentinelops
helm template sentinelops deploy/helm/sentinelops -f deploy/helm/sentinelops/values-production.example.yaml

# 2. Deploy to Kubernetes
helm upgrade --install sentinelops ./deploy/helm/sentinelops \
  --namespace sentinelops \
  --create-namespace \
  -f ./deploy/helm/sentinelops/values-production.example.yaml \
  --set secrets.postgresPassword="$PROD_PG_PASSWORD" \
  --set secrets.redisPassword="$PROD_REDIS_PASSWORD" \
  --set secrets.jwtSecret="$PROD_JWT_SECRET" \
  --atomic \
  --timeout 10m
```

The `--atomic` flag ensures automatic rollback if any pod fails readiness probes within 10 minutes.

---

## 4. Rollback & Disaster Recovery Playbooks

### 4.1 Helm Revision Rollback
If application telemetry indicates elevated error rates or degraded performance after a deployment:
```bash
# Check revision history
helm history sentinelops -n sentinelops

# Roll back to the immediately preceding revision
helm rollback sentinelops 0 -n sentinelops

# Or roll back to an explicit known-good revision
helm rollback sentinelops 4 -n sentinelops
```

### 4.2 Kubernetes Workload Rollback
```bash
# Instant deployment rollback
kubectl rollout undo deployment/sentinelops-api -n sentinelops
kubectl rollout undo deployment/sentinelops-worker -n sentinelops
kubectl rollout undo deployment/sentinelops-web -n sentinelops

# Monitor rollout status
kubectl rollout status deployment/sentinelops-api -n sentinelops
```
