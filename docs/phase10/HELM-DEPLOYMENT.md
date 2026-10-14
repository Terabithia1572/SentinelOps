# SentinelOps — Helm Chart Deployment & Operations Guide

## 1. Chart Structure & Components

The official Helm chart for SentinelOps is located in `deploy/helm/sentinelops/`.

```
deploy/helm/sentinelops/
├── Chart.yaml                          # Chart metadata (v1.0.0, App v1.0.0)
├── .helmignore                         # Packaging exclusion rules
├── values.yaml                         # Production baseline defaults
├── values-development.yaml             # Local / minikube lightweight profile
├── values-production.example.yaml      # High-availability cloud-ready profile
└── templates/
    ├── _helpers.tpl                    # Name templates and standard labels
    ├── serviceaccount.yaml             # Dedicated ServiceAccount
    ├── configmap.yaml                  # Application configuration variables
    ├── configmap-collector.yaml        # OpenTelemetry Collector configuration
    ├── job-migration.yaml              # EF Core database migration pre-deploy Job
    ├── deployment-api.yaml             # Stateless ASP.NET Core API deployment
    ├── service-api.yaml                # ClusterIP service for API
    ├── hpa-api.yaml                    # HorizontalPodAutoscaler for API pods
    ├── pdb-api.yaml                    # PodDisruptionBudget for high availability
    ├── deployment-worker.yaml          # Competing Redis Streams worker deployment
    ├── deployment-web.yaml             # React 19 SPA Nginx web frontend
    ├── service-web.yaml                # ClusterIP service for Web SPA
    ├── deployment-collector.yaml       # OpenTelemetry Collector deployment
    ├── service-collector.yaml          # ClusterIP service for OTLP gRPC/HTTP
    ├── statefulset-redis.yaml          # In-cluster Redis with AOF persistence
    ├── service-redis.yaml              # ClusterIP service for Redis
    ├── ingress.yaml                    # Ingress with TLS and path routing
    ├── networkpolicy.yaml              # Pod-to-pod least-privilege network isolation
    └── NOTES.txt                       # Post-installation instructions
```

---

## 2. Validation & Linting

### Linting the Helm Chart
```bash
helm lint deploy/helm/sentinelops
```

### Dry-Run Template Rendering (Static Schema Verification)
Render all Kubernetes manifests locally without requiring a connected Kubernetes cluster:
```bash
# Development profile
helm template sentinelops deploy/helm/sentinelops -f deploy/helm/sentinelops/values-development.yaml

# Production profile
helm template sentinelops deploy/helm/sentinelops -f deploy/helm/sentinelops/values-production.example.yaml
```

---

## 3. Installation Scenarios

### Scenario A: Local Development / Minikube
Installs in-cluster Redis, single API replica, and local ingress:
```bash
helm upgrade --install sentinelops ./deploy/helm/sentinelops \
  --namespace sentinelops \
  --create-namespace \
  -f ./deploy/helm/sentinelops/values-development.yaml
```

### Scenario B: Production Kubernetes (EKS / GKE / AKS)
Deploys with external managed PostgreSQL & Redis, multiple API replicas (HPA 2-10), multiple worker replicas, PodDisruptionBudget, and TLS Ingress:
```bash
helm upgrade --install sentinelops ./deploy/helm/sentinelops \
  --namespace sentinelops \
  --create-namespace \
  -f ./deploy/helm/sentinelops/values-production.example.yaml \
  --set secrets.postgresPassword="$PROD_PG_PASSWORD" \
  --set secrets.jwtSecret="$PROD_JWT_SECRET" \
  --set secrets.redisPassword="$PROD_REDIS_PASSWORD"
```

---

## 4. Key Configuration Parameters

| Parameter | Description | Default |
| :--- | :--- | :--- |
| `global.environment` | ASP.NET Core environment | `Production` |
| `api.replicaCount` | Base number of API replicas | `2` |
| `api.autoscaling.enabled` | Enable Horizontal Pod Autoscaler | `true` |
| `api.autoscaling.minReplicas` | Minimum API pods | `2` |
| `api.autoscaling.maxReplicas` | Maximum API pods under peak load | `10` |
| `api.autoscaling.targetCPUUtilizationPercentage` | HPA CPU trigger threshold | `75` |
| `api.podDisruptionBudget.minAvailable` | Guaranteed minimum available pods | `1` |
| `worker.replicaCount` | Number of background worker pods | `2` |
| `collector.enabled` | Deploy OpenTelemetry Collector | `true` |
| `redis.inCluster` | Deploy in-cluster Redis StatefulSet | `true` (false for prod) |
| `ingress.enabled` | Deploy Ingress controller routing | `true` |
| `networkPolicy.enabled` | Enforce zero-trust network isolation | `true` |
| `migration.enabled` | Execute pre-deploy EF Core migration job | `true` |

---

## 5. Lifecycle, Migrations & Zero-Downtime Rollouts

### Pre-Deployment Database Migrations
The chart includes `templates/job-migration.yaml` annotated with:
```yaml
helm.sh/hook: pre-install,pre-upgrade
helm.sh/hook-weight: "1"
helm.sh/hook-delete-policy: before-hook-creation,hook-succeeded
```
When running `helm upgrade`, Helm automatically executes the migration container first. Only when the migration succeeds does Helm trigger rolling updates on API and Worker pods. This eliminates race conditions on `__EFMigrationsHistory`.

### Rolling Updates & Zero-Downtime
API deployments specify `maxSurge: 25%` and `maxUnavailable: 0`. New pods must pass both `livenessProbe` and `readinessProbe` before old pods receive SIGTERM.

### Rollback Procedure
If a deployment fails health checks or metric alerts trigger:
```bash
# View deployment revision history
helm history sentinelops -n sentinelops

# Roll back to previous known-healthy revision
helm rollback sentinelops 0 -n sentinelops
```
