# SentinelOps — Linux Docker Build & Container Infrastructure Remediation Report

## Executive Summary

During initial Docker Engine verification on the host, the API and Worker Docker builds failed with error:
`MSB4018: ResolvePackageAssets failed unexpectedly`
`NuGet.Packaging.Core.PackagingException: Unable to find fallback package folder 'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'`
Additionally, Docker build contexts were unexpectedly bloated (~380 MB for API/Worker and ~272 MB for Web).

This report documents the root-cause analysis, exact architectural remediations applied, Docker build metrics, container runtime health, and regression test results. All SentinelOps containers are verified healthy and operational, while existing unrelated host workloads remain untouched.

---

## 1. Root Cause Analysis

### 1.1. Windows-Specific NuGet Assets Leakage
- **Mechanism**: The repository root lacked a `.dockerignore` file. When Docker executed `docker compose build` with `context: .`, the entire workspace was packaged into the build context, including `src/**/obj/` and `src/**/bin/`.
- **Trigger**: In `Dockerfile.api` and `Dockerfile.worker`, `dotnet restore` initially executed cleanly on Linux. However, a subsequent instruction `COPY src/ src/` merged the host's filesystem into `/src/src/`, **overwriting the Linux-generated `obj/` directories with the Windows host's `obj/` directories**.
- **Impact**: The host's `project.assets.json` and `*.nuget.g.props` files contained absolute Windows paths:
  `"C:\\Program Files (x86)\\Microsoft Visual Studio\\Shared\\NuGetPackages"`
  When `RUN dotnet build ... --no-restore` executed, MSBuild attempted to locate packages in that Windows directory within the Linux container filesystem, immediately failing with `MSB4018`.

### 1.2. Bloated Docker Build Context
- In the absence of `.dockerignore`, Docker transferred:
  - Git repository objects (`.git/`): ~80 MB
  - Web client dependencies (`src/SentinelOps.Web/node_modules/`): ~200+ MB
  - Compiled binaries and symbol packages (`bin/`, `obj/`): ~60+ MB
  - Visual screenshots, logs, and test results: ~40 MB
  Total context transferred per build was **379.77 MB** for API/Worker and **272.57 MB** for Web.

### 1.3. Local Configuration File Override & Connection String Shadowing
- `src/SentinelOps.Api/appsettings.Local.json` and `src/SentinelOps.Worker/appsettings.Local.json` existed on the host containing `Host=localhost;Port=5432`.
- Because `.dockerignore` was absent, these local files were copied into the images.
- In `Program.cs`, `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)` was called without re-adding environment variables, causing `appsettings.Local.json` to override Docker Compose environment variables (`ConnectionStrings__DefaultConnection=Host=postgres`), making the containerized API attempt to reach `localhost:5432` rather than the `postgres` service container.

### 1.4. Ubuntu 24.04 (Noble) Base Image & Non-Root User
- The Dockerfiles were using `10.0-preview` base images and attempted:
  `RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app`
- In Ubuntu 24.04, `adduser` is not installed by default, returning exit code 127 (`adduser: not found`).
- Modern .NET 8/9/10 container images from Microsoft already include a built-in non-root user `app` (UID: 1654, GID: 1654).

### 1.5. OpenTelemetry Collector Environment Variable Syntax
- In `deploy/otel-collector/otel-collector-config.yaml`, the API key header was configured as `${SENTINELOPS_API_KEY:-...}`.
- OpenTelemetry Collector v0.120+ strictly requires the `${env:VAR_NAME}` syntax for environment variable expansion; omitting `env:` resulted in `invalid uri` configuration parse errors.

---

## 2. Exact Remediation Steps Applied

### 2.1. Repository-Root `.dockerignore` Creation
Created `.dockerignore` at the repository root explicitly excluding:
- `**/bin/` and `**/obj/` (completely eliminates Windows NuGet assets).
- `**/node_modules/` and `**/dist/` (eliminates host frontend artifacts).
- `.git/` and version control metadata.
- `**/appsettings.Local.json` and `**/.env*` (prevents host credential leakage).
- `docs/`, `tests/`, `tools/`, `samples/`, `deploy/k8s/`, and `deploy/helm/` (optimizes context size).
- Preserved `deploy/docker/` to ensure `nginx.conf` remains available for the web container build.

### 2.2. Stable .NET 10 Images & Built-In Non-Root User
- Updated `Dockerfile.api` and `Dockerfile.worker`:
  - SDK: `mcr.microsoft.com/dotnet/sdk:10.0`
  - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Replaced custom `adduser` commands with Microsoft's standard non-root pattern:
  `COPY --from=publish --chown=app:app /app/publish .`
  `USER app`
- Explicitly specified `SentinelOps.Api.csproj` and `SentinelOps.Worker.csproj` in `dotnet build` and `dotnet publish` commands.

### 2.3. Configuration Precedence & Startup Database Migration
- In `src/SentinelOps.Api/Program.cs` and `src/SentinelOps.Worker/Program.cs`:
  Added `builder.Configuration.AddEnvironmentVariables();` immediately after loading `appsettings.Local.json` so Docker Compose and Kubernetes environment variables always take precedence.
- Added automated startup database migrations in `Program.cs`:
  Executes `await dbContext.Database.MigrateAsync()` when connected to a relational database (PostgreSQL), and falls back to `await dbContext.Database.EnsureCreatedAsync()` for in-memory testing providers.

### 2.4. OpenTelemetry Collector Configuration Correction
- Updated `deploy/otel-collector/otel-collector-config.yaml`:
  `X-API-Key: ${env:SENTINELOPS_API_KEY}`

---

## 3. Docker Build Results & Context Optimization

| Component | Pre-Fix Context Size | Post-Fix Context Size | Reduction | Build Status |
| :--- | :--- | :--- | :--- | :--- |
| **`sentinelops-api`** | ~379.77 MB | **49.61 kB** | **99.98%** | **BUILT (371 MB image)** |
| **`sentinelops-worker`** | ~379.77 MB | **49.61 kB** | **99.98%** | **BUILT (357 MB image)** |
| **`sentinelops-web`** | ~272.57 MB | **5.95 kB** | **99.99%** | **BUILT (104 MB image)** |

---

## 4. Runtime Container Health & Endpoint Verification

### 4.1. Container Status Matrix (`docker compose ps`)
```
NAME                         IMAGE                                          COMMAND                  SERVICE              STATUS                    PORTS
sentinelops-api              sentinelops-sentinelops-api                    "dotnet SentinelOps.…"   sentinelops-api      Up 5 minutes (healthy)    0.0.0.0:5100->8080/tcp
sentinelops-otel-collector   otel/opentelemetry-collector-contrib:0.120.0   "/otelcol-contrib --…"   otel-collector       Up 5 minutes              0.0.0.0:4317-4318, 8888-8889
sentinelops-postgres         postgres:17-alpine                             "docker-entrypoint.s…"   postgres             Up 9 minutes (healthy)    127.0.0.1:55432->5432/tcp
sentinelops-redis            redis:7-alpine                                 "docker-entrypoint.s…"   redis                Up 9 minutes (healthy)    127.0.0.1:56379->6379/tcp
sentinelops-web              sentinelops-sentinelops-web                    "/docker-entrypoint.…"   sentinelops-web      Up 5 minutes              0.0.0.0:3000->80/tcp
sentinelops-worker           sentinelops-sentinelops-worker                 "dotnet SentinelOps.…"   sentinelops-worker   Up 7 minutes
```

### 4.2. Host Infrastructure Isolation Verification
Unrelated existing containers on the host were verified running continuously and undisturbed:
- `BasketDB` (redis on `0.0.0.0:6379`) — **Up 58 minutes (Unchanged)**
- `OrderDB` (mssql on port 1440) — **Up 58 minutes (Unchanged)**
- `CommentDB` (mssql on port 1442) — **Up 58 minutes (Unchanged)**
- `CargoDB` (mssql on port 1441) — **Up 58 minutes (Unchanged)**
- `IdentityDB` (mssql on port 1435) — **Up 58 minutes (Unchanged)**
- `portainer` (ports 8000, 9000) — **Up 58 minutes (Unchanged)**

SentinelOps PostgreSQL and Redis host bindings (`127.0.0.1:55432:5432` and `127.0.0.1:56379:6379`) prevent any host port collisions.

### 4.3. Live Endpoint Verification
- `GET http://localhost:5100/health/live` -> **200 OK (`Healthy`)**
- `GET http://localhost:5100/health/ready` -> **200 OK (`Healthy`)**
- `GET http://localhost:5100/api/health` -> **200 OK (`{ "status": "Healthy", "version": "1.0.0" }`)**
- `GET http://localhost:3000` -> **200 OK (Nginx serving React 19 SPA with security headers)**
- `POST http://localhost:4318/v1/logs` -> **200 OK (OTel Collector successfully ingesting telemetry)**

---

## 5. Regression Test Results

Full test suites were executed following all code and configuration changes:

- **Backend Test Suite (`dotnet test SentinelOps.slnx`)**:
  - `SentinelOps.Domain.Tests`: 49 passed, 0 failed, 0 skipped
  - `SentinelOps.Application.Tests`: 101 passed, 0 failed, 0 skipped
  - `SentinelOps.Api.IntegrationTests`: 152 passed, 0 failed, 0 skipped
  - **Total Backend Tests: 302 / 302 PASSED (100% Pass Rate)**
- **Frontend Test Suite (`vitest run`)**:
  - **Total Frontend Tests: 54 / 54 PASSED across 15 suites (100% Pass Rate)**
- **Frontend Typecheck (`tsc -b --noEmit`)**:
  - **0 Errors**
- **Full Solution Build (`dotnet build --warnaserror`)**:
  - **0 Warnings, 0 Errors**

---

## 6. Conclusion

The Linux Docker build failure has been completely resolved. The root causes (Windows-generated NuGet assets leakage, bloated build context, base image non-root user incompatibilities, configuration precedence shadowing, and OTel collector URI syntax) were systematically remediated. All SentinelOps services are verified healthy, durable, isolated, and operational in live containers.
