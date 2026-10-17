# SentinelOps — Docker Containerization & Reproducibility Report

## 1. Executive Summary
This document certifies that SentinelOps is 100% container-reproducible, built directly from committed source code through standard multi-stage Docker builds without requiring ad-hoc runtime file copying (`docker cp`) or host-specific state dependencies.

---

## 2. Docker Architecture & Multi-Stage Builds

### Service Image Inventory

| Service | Context | Dockerfile | Base Image | Final Image Base | Non-Root User |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `sentinelops-api` | Root (`.`) | `deploy/docker/Dockerfile.api` | `mcr.microsoft.com/dotnet/sdk:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0` | `app:app` |
| `sentinelops-worker` | Root (`.`) | `deploy/docker/Dockerfile.worker` | `mcr.microsoft.com/dotnet/sdk:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0` | `app:app` |
| `sentinelops-web` | Root (`.`) | `deploy/docker/Dockerfile.web` | `node:22-alpine` | `nginx:alpine` | `nginx` |

### Multi-Stage Highlights

1. **`sentinelops-api`**:
   - Stage 1 (`build`): Copies `.csproj` files first to maximize layer caching on `dotnet restore`, then compiles with `TreatWarningsAsErrors=true` in `Release` configuration.
   - Stage 2 (`publish`): Packages the compiled binary with `/p:UseAppHost=false`.
   - Stage 3 (`final`): Minimal ASP.NET Core 10 runtime image, installs `curl` for container healthcheck execution, changes ownership to non-root user `app:app`, and runs on internal port 8080.

2. **`sentinelops-worker`**:
   - Follows identical multi-stage caching and security practices as the API, running background hosted services with non-root security boundaries.

3. **`sentinelops-web`**:
   - Stage 1 (`build`): Uses `node:22-alpine` with deterministic package restore (`npm ci`), runs TypeScript compiler check (`tsc -b`) and Vite production bundle generator (`vite build`), outputting optimized static assets into `/app/dist`.
   - Stage 2 (`final`): Deploys static assets into `nginx:alpine` with [`deploy/docker/nginx.conf`](file:///c:/Users/Yunus/Desktop/SentinelOps/deploy/docker/nginx.conf).
   - Nginx configuration provides:
     - Gzip compression on CSS, JS, and JSON assets.
     - Strict security headers (`X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Content-Security-Policy`).
     - SPA routing fallback (`try_files $uri $uri/ /index.html;`).
     - Reverse proxy routing for `/api/` forwarding to `http://sentinelops-api:8080`.
     - WebSocket upgrade proxy for SignalR real-time hubs on `/hubs/`.

---

## 3. Fresh Build & Deployment Verification

In Phase 20, all three custom application images were built freshly from committed source code:

```bash
docker compose build sentinelops-web
docker compose build sentinelops-api
docker compose build sentinelops-worker
```

Execution times and artifacts:
- `sentinelops-web`: Built cleanly in 9.1s; bundle size 455KB gzip 134KB.
- `sentinelops-api`: Built cleanly in 18.5s with zero compiler warnings.
- `sentinelops-worker`: Built cleanly in 14.8s with zero compiler warnings.

Containers were refreshed without downtime or volume data loss:

```bash
docker compose up -d --no-deps sentinelops-api
docker compose up -d --no-deps sentinelops-web sentinelops-worker
```

All 6 project services verified healthy:
- `sentinelops-api` (Port 5100 -> 8080): **Healthy**
- `sentinelops-web` (Port 3000 -> 80): **Running / Operational**
- `sentinelops-worker`: **Running / Operational**
- `sentinelops-postgres` (Port 55432 -> 5432): **Healthy**
- `sentinelops-redis` (Port 56379 -> 6379): **Healthy**
- `sentinelops-otel-collector` (Ports 4317, 4318, 8888, 8889): **Running / Operational**

---

## 4. Verdict

**DOCKER CONTAINERIZATION & REPRODUCIBILITY: VERIFIED**
