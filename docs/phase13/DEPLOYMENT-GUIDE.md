# SentinelOps Enterprise Deployment & Operations Guide (v1.0.0-rc1)

This comprehensive guide details the deployment options, configuration parameters, container orchestration, and security hardening procedures for SentinelOps `v1.0.0-rc1`.

---

## 1. Architecture Overview

SentinelOps comprises five primary functional tiers:
1. **API Tier (`SentinelOps.Api`)**: ASP.NET Core 10 web service exposing REST APIs, SignalR hubs, and OTLP ingestion.
2. **Worker Tier (`SentinelOps.Worker`)**: Background processor consuming Redis Streams for telemetry persistence and alert evaluation.
3. **Web Tier (`SentinelOps.Web`)**: React 19 single-page application served via Nginx with automated code-splitting and client-side routing.
4. **Data & Buffering Tier**: PostgreSQL 18 relational database and Redis 7.2 stream buffer.
5. **Observability Ingestion Tier**: OpenTelemetry Collector gateway receiving OTLP gRPC/HTTP payloads.

---

## 2. Deployment Topology Options

### Option A: Bare-Metal / Native Host Execution
For local development, diagnostic testing, or native on-premises environments.

#### Prerequisites:
- .NET 10 SDK (`dotnet --version` >= 10.0.100)
- Node.js 22+ (`node -v`)
- PostgreSQL 18 running on port 5432
- Redis running on port 6379

#### Execution Steps:
1. **Apply Database Migrations**:
   ```bash
   dotnet ef database update --project src/SentinelOps.Infrastructure --startup-project src/SentinelOps.Api
   ```
2. **Launch API Service**:
   ```bash
   dotnet run --project src/SentinelOps.Api --urls "http://localhost:5100"
   ```
3. **Launch Background Worker**:
   ```bash
   dotnet run --project src/SentinelOps.Worker
   ```
4. **Launch Web Client**:
   ```bash
   npm --prefix src/SentinelOps.Web install
   npm --prefix src/SentinelOps.Web run dev
   ```

---

### Option B: Docker Compose Multi-Container Stack
For containerized local environments, staging, and single-host production deployments.

#### Execution Steps:
1. **Validate Configuration**:
   ```bash
   docker compose config
   ```
2. **Build and Launch All Services**:
   ```bash
   docker compose up -d --build
   ```
3. **Verify Health**:
   ```bash
   docker compose ps
   curl -f http://localhost:5100/health/ready
   ```
4. **Access Endpoints**:
   - Web UI: `http://localhost:3000`
   - API & Swagger: `http://localhost:5100/swagger`
   - OTel Collector OTLP gRPC: `localhost:4317`
   - OTel Collector OTLP HTTP: `localhost:4318`

---

### Option C: Kubernetes & Helm Deployment
For cloud-native, high-availability, auto-scaling enterprise production clusters.

#### Directory Structure:
- Kubernetes Raw Manifests: `deploy/k8s/`
- Helm Chart: `deploy/helm/sentinelops/`

#### Step-by-Step Production Deployment:
1. **Create Namespace & Secrets**:
   ```bash
   kubectl create namespace sentinelops
   
   kubectl create secret generic sentinelops-secrets \
     --namespace sentinelops \
     --from-literal=ConnectionStrings__PostgreSql="Host=postgres-cluster;Port=5432;Database=sentinelops;Username=sentinelops_user;Password=SECURE_DB_PASSWORD;" \
     --from-literal=ConnectionStrings__Redis="redis-cluster:6379,password=SECURE_REDIS_PASSWORD" \
     --from-literal=JwtSettings__Secret="ENTERPRISE_MIN_32_CHAR_JWT_SIGNING_SECRET_KEY" \
     --from-literal=StripeSettings__WebhookSecret="whsec_..."
   ```

2. **Deploy via Helm**:
   ```bash
   helm upgrade --install sentinelops deploy/helm/sentinelops/ \
     --namespace sentinelops \
     --values deploy/helm/sentinelops/values.yaml \
     --set api.replicaCount=3 \
     --set worker.replicaCount=2 \
     --set web.replicaCount=2
   ```

3. **Verify Cluster Rollout**:
   ```bash
   kubectl rollout status deployment/sentinelops-api -n sentinelops
   kubectl rollout status deployment/sentinelops-worker -n sentinelops
   kubectl rollout status deployment/sentinelops-web -n sentinelops
   kubectl get pods -n sentinelops -o wide
   ```

---

## 3. Environment Variables Reference

| Environment Variable | Description | Default / Example | Required |
| :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Application runtime environment | `Production` | Yes |
| `ConnectionStrings__PostgreSql` | Npgsql database connection string | `Host=postgres;Port=5432;Database=sentinelops;Username=sentinelops;Password=...` | Yes |
| `ConnectionStrings__Redis` | StackExchange.Redis connection string | `redis:6379,abortConnect=false` | Yes |
| `JwtSettings__Secret` | HMAC-SHA256 signing key (min 256 bits) | Cryptographically random string (min 32 chars) | Yes |
| `JwtSettings__Issuer` | Token issuer domain | `SentinelOps` | Yes |
| `JwtSettings__Audience` | Token audience domain | `SentinelOpsClient` | Yes |
| `JwtSettings__ExpirationMinutes`| JWT access token lifetime | `60` | Yes |
| `MigrationSettings__AutoReconcileLegacyTenants` | Safety gate for legacy tenant data | `false` | No |
| `StripeSettings__WebhookSecret` | Stripe webhook signing secret | `whsec_...` | For SaaS billing |
| `OpenAiSettings__ApiKey` | External LLM API key | `sk-...` | Optional (local fallback) |

---

## 4. Security Hardening Checklist

- [x] **Non-Root Execution**: Container images run as dedicated non-root system user (`appuser:10001`).
- [x] **Network Policies**: Kubernetes NetworkPolicies restrict ingress/egress between worker, redis, and postgres pods.
- [x] **Zero Plaintext Secrets**: Secrets injected exclusively via Kubernetes Secrets or Docker Compose environment variables.
- [x] **HTTPS & TLS Termination**: Enforced at Ingress / Nginx reverse proxy with HSTS headers (`max-age=31536000; includeSubDomains`).
- [x] **Multi-Tenant Authorization**: All endpoints enforce caller membership validation before processing data.
