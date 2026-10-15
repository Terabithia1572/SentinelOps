# SentinelOps Phase 15 — Baseline & Security Inventory

## 1. Executive Summary

- **Assessment Date**: October 15, 2026
- **Repository**: `C:\Users\Yunus\Desktop\SentinelOps` (`https://github.com/Terabithia1572/SentinelOps`)
- **Baseline Git Commit**: `be44d18` (`chore(release): finalize phase 14 release assessment and documentation`)
- **Git Branch**: `main` (clean working tree, synced with remote)
- **Runtime Environment**: Docker Desktop WSL2 Linux Engine v28.5.1 on Windows 11 host
- **Platform Architecture**: 6 containers running locally via Docker Compose (.NET 10 Web API, .NET 10 Background Worker, React 19 SPA on Nginx, PostgreSQL 17, Redis 7, OpenTelemetry Collector v0.120.0).

---

## 2. Container Runtime State & Port Inventory

All six platform containers are verified running, healthy, and isolated:

| Container Name | Image / Base | Status | Internal Port | Host Port Binding | Purpose |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `sentinelops-api` | .NET 10 ASP.NET Core | Up (healthy) | `8080/tcp` | `0.0.0.0:5100->8080` | REST API, Ingestion, Hubs |
| `sentinelops-worker` | .NET 10 Worker Host | Up | N/A | None (Internal) | Stream Consumer, Background Tasks |
| `sentinelops-web` | Nginx Alpine / React 19 | Up | `80/tcp` | `0.0.0.0:3000->80` | Web UI Dashboard |
| `sentinelops-otel-collector` | OTel Collector Contrib 0.120.0 | Up | `4317, 4318, 8888, 8889` | `0.0.0.0:4317-4318, 8888-8889` | OTLP Ingestion Collector |
| `sentinelops-redis` | Redis 7 Alpine | Up (healthy) | `6379/tcp` | `127.0.0.1:56379->6379` | Telemetry Buffer & Streams |
| `sentinelops-postgres` | PostgreSQL 17 Alpine | Up (healthy) | `5432/tcp` | `127.0.0.1:55432->5432` | Relational Storage |

*Unrelated host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) were audited and confirmed running independently without port collisions.*

---

## 3. Test Suite Baseline Inventory

The existing automated test suites from Phase 14 were inventoried:

| Test Suite / Layer | Location | Engine | Verified Count |
| :--- | :--- | :--- | :--- |
| **Domain Tests** | `tests/SentinelOps.Domain.Tests/` | xUnit (.NET 10) | 49 passed |
| **Application Tests** | `tests/SentinelOps.Application.Tests/` | xUnit (.NET 10) | 101 passed |
| **API Integration Tests** | `tests/SentinelOps.Api.IntegrationTests/` | xUnit (.NET 10) | 152 passed |
| **Frontend Unit / Components** | `src/SentinelOps.Web/src/pages/__tests__/` | Vitest / JSDOM | 54 passed (15 suites) |
| **Live Browser E2E** | `src/SentinelOps.Web/e2e/live-browser.spec.ts` | Playwright / Chromium | 16 passed (4 viewports) |
| **Total Baseline Tests** | | | **372 passed (100% pass rate)** |

---

## 4. Security & Credential Inventory

A comprehensive audit was performed across environment variables, Docker Compose configuration, application configuration files, database tables, test scripts, and documentation.

*(Note: In accordance with Checkpoint 15.0 requirements, raw secret values are redacted and represented by metadata/hash references).*

### 4.1 Identified Development & Test Credentials

| Credential Type | Scope / Location | Identity / Role / Purpose | Security Risk & Classification |
| :--- | :--- | :--- | :--- |
| **Admin User Account** | Database (`AspNetUsers`), `live-browser.spec.ts` | `admin@sentinelops.local` (Roles: `Admin`, `Viewer`) | Predictable default development credentials; hardcoded in Playwright test scripts. |
| **Tenant Alpha Admin** | Database (`AspNetUsers`), Integration Tests | `alpha.admin@sentinelops.local` (Role: `Viewer`) | Multi-tenant test fixture account; used in cross-tenant isolation testing. |
| **Tenant Beta Admin** | Database (`AspNetUsers`), Integration Tests | `beta.admin@sentinelops.local` (Role: `Viewer`) | Multi-tenant test fixture account; used in cross-tenant isolation testing. |
| **Collector Internal API Key** | `docker-compose.yml`, `otel-collector-config.yaml`, DB | Collector &rarr; API ingestion bridge (`sent_live_collector...`) | Hardcoded in `docker-compose.yml` environment fallback. SHA-256 hash stored in DB. |
| **Default Ingestion Keys** | Database (`ApiKeys`) | 3 tenant application initial keys (`sent_live_...`) | High entropy keys generated during tenant seeding; hashes stored in PostgreSQL. |
| **Temporary Perf Key** | Database (`ApiKeys`), `tools/perf_benchmark.ps1` | Benchmark API key (`sent_live_b79158aa...`) | Active key created during Phase 14 performance testing; file artifact removed, DB record remains active. |
| **Database Credentials** | `docker-compose.yml`, `appsettings.json` | `sentinelops` user for local PostgreSQL container | Development default passwords embedded as Compose fallbacks; safe for local isolated container, prohibited in staging/prod. |
| **JWT Secret Key** | `docker-compose.yml`, `src/SentinelOps.Api/appsettings.json` | Token signing secret key | Fallback string provided for local development. `Program.cs` already blocks placeholder in `Production`, but must be strictly externalized for Staging. |

### 4.2 Security Vulnerabilities & Operational Risk Findings

1. **Hardcoded Test Credentials in Playwright Specs**:
   - `src/SentinelOps.Web/e2e/live-browser.spec.ts` contains raw string literals for the administrator test account (`admin@sentinelops.local`). While standard for local mock environments, staging/production testing requires dynamic credentials via environment variables (`E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`).
2. **Direct Role Manipulation Trace**:
   - During Phase 14 troubleshooting, the `Admin` role was assigned to `admin@sentinelops.local` via raw SQL `INSERT INTO "AspNetUserRoles"`.
   - **Risk**: Bypasses audit logging (`AuditEvents`), domain event dispatchers, and tenant authorization safeguards. A formal, audited programmatic mechanism is required.
3. **Database Startup Migrations (`MigrateAsync()`)**:
   - `Program.cs` invokes `dbContext.Database.MigrateAsync()` directly on API web process boot.
   - **Risk**: In staging and multi-replica production deployments, concurrent pods boot simultaneously and race to apply database schema migrations, risking deadlocks or failed startup states.
4. **Active Ephemeral Performance Test Key**:
   - Key ID `b641b8c2-8d04-4d25-b812-9b9f49c7370f` ("Perf Test Key") remains active in `ApiKeys` table post-testing. Should be revoked.
5. **Collector Secret Exposure in Compose Fallbacks**:
   - `docker-compose.yml` provides a static fallback for `SENTINELOPS_COLLECTOR_API_KEY` (`sent_live_collector_internal_key`).
6. **Premature Production Release Claims**:
   - `README.md` and Phase 14 summaries claimed `Production Ready` based solely on local single-node Docker Compose validation. Formal staging deployment, external secret management, and staging validation gates must precede any production claim.
