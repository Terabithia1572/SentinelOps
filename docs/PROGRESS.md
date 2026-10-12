# SentinelOps Progress Tracker

## Phase 1 — Foundation
**Status:** ✅ Completed  
**Completed:** 2026-10-12

### Completed
- [x] Repository initialization & Git remote configuration
- [x] Clean Architecture solution structure (.slnx)
- [x] Domain project with core entities, value objects, and enums
- [x] Contracts project with DTOs, requests, and responses
- [x] Application project with CQRS, MediatR, FluentValidation, and interfaces
- [x] Infrastructure project with EF Core 10, Npgsql PostgreSQL, Redis, and Identity
- [x] API project with ASP.NET Core, Serilog, JWT auth setup, Swagger, and health endpoints
- [x] Worker project with hosted services and retention cleanup background service
- [x] Frontend React project with TypeScript, Vite, Tailwind CSS v4, TanStack Query, and Lucide icons
- [x] Docker Compose configuration (PostgreSQL 17, Redis 7, OpenTelemetry Collector, API, Worker, Web)
- [x] OpenTelemetry Collector configuration
- [x] GitHub Actions CI workflow (.github/workflows/ci.yml)
- [x] Initial documentation (README, Architecture Overview, ADRs, Progress Tracker)
- [x] Initial unit and integration test suites (Domain, Application, API, Web)
- [x] Initial EF Core database migration (InitialCreate)
- [x] Build verification (.NET 10 solution: 0 errors/0 warnings; Web: 0 errors/0 warnings)
- [x] First verified GitHub push

### Known Limitations
- Phase 1 delivers the structural foundation, domain model, and operational baseline.
- JWT authentication endpoints (login/register) will be fully wired in Phase 2.
- Telemetry ingestion pipelines (OTLP gRPC/HTTP endpoints and collector pipeline) will be activated in Phase 3.

### Next Steps
- Complete Phase 2: Identity and Application Management (Done)
- Proceed to Phase 3: Telemetry Ingestion (OTLP gRPC/HTTP endpoints and collector pipelines)

---

## Phase 2 — Identity and Application Management
**Status:** ✅ Completed  
**Completed:** 2026-10-12

### Completed
- [x] ASP.NET Core Identity integration with `ApplicationUser` (`FullName`, `CreatedAtUtc`, `IsActive`)
- [x] Role-based access control (RBAC): `Admin`, `Operator`, `Viewer` roles and policies
- [x] Secure registration and login endpoints with input validation (`RegisterCommand`, `LoginCommand`)
- [x] JWT token generation (HMAC-SHA256, 15m expiration) with claims
- [x] Refresh token rotation (RTR) with cryptographic random bytes and SHA-256 hash storage (`RefreshToken` entity)
- [x] Token revocation endpoint (`/api/auth/revoke`) and replay attack prevention
- [x] Application and environment management (full CRUD, environments, services)
- [x] API Key architecture: high-entropy format `sent_{env}_{secret}`, SHA-256 hashing, prefix indexing (`ApiKey` entity)
- [x] API Key lifecycle: generation, one-time raw secret exposure, rotation, and revocation (`ApiKeysController`)
- [x] EF Core database migration (`20261012190115_AddIdentityAndApiKeys`) applied to PostgreSQL
- [x] Frontend Auth Context (`AuthContext`, token persistence, automatic session restore & refresh)
- [x] Frontend Protected Routes (`ProtectedRoute` with role authorization enforcement)
- [x] Frontend Login page (`LoginPage`) with credential validation and redirect preservation
- [x] Frontend Registration page (`RegisterPage`) with password complexity requirements
- [x] Frontend Application Management page (`ApplicationsPage`) with environment/service modals
- [x] Frontend API Key Management page (`ApiKeysPage`) with secure one-time key revelation dialog and rotation
- [x] Sidebar user profile badge and logout integration
- [x] Comprehensive test coverage:
  - 40 backend tests (Domain, Application validators, Identity & API Key integration tests)
  - 10 frontend tests (Vitest + React Testing Library)
- [x] Build validation: .NET 10 (0 errors, 0 warnings), React/TypeScript (0 errors, 0 warnings)

### Next Steps
- Begin Phase 3: Telemetry Ingestion (OTLP gRPC/HTTP receivers, raw telemetry storage, Redis stream buffering, and worker ingestion pipeline).

---

## Phase 3 — Telemetry Ingestion
**Status:** ⏳ Pending

## Phase 4 — Observability Explorer
**Status:** ⏳ Pending

## Phase 5 — Alerting
**Status:** ⏳ Pending

## Phase 6 — Advanced Analytics
**Status:** ⏳ Pending

## Phase 7 — Production Hardening
**Status:** ⏳ Pending

## Phase 8 — Portfolio Presentation
**Status:** ⏳ Pending
