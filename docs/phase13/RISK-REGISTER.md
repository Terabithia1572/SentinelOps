# SENTINELOPS — COMPREHENSIVE RISK REGISTER
**Phase 13 Final Enterprise Audit & Release Gate Assessment**  
**Repository**: `https://github.com/Terabithia1572/SentinelOps`  
**Baseline Commit**: `a8d09ce`  

---

## Risk Severity Definitions

- **P0 (Critical)**: Exploitable security vulnerabilities, tenant isolation bypasses, active data corruption, or catastrophic unavailability. Must be remediated before release.
- **P1 (High)**: Broken primary business workflows, billing integrity flaws, durability degradation, or migration risks.
- **P2 (Medium)**: Operational constraints, tooling gaps, performance degradation, or resiliency/fallback limitations.
- **P3 (Low)**: Minor UI/accessibility inconsistencies, cosmetic styling, or documentation clarifications.

---

## Active Risk Register

| Risk ID | Severity | Component | Summary | Current Status |
|:---|:---:|:---|:---|:---:|
| **P13-SEC-01** | **P0** | `CurrentTenantService.cs`, `ApplicationsController`, `LogsController`, `TracesController`, `MetricsController` | Unverified `X-Organization-Id` Request Header Leading to Cross-Tenant Data Access (IDOR) | **OPEN** |
| **P13-SEC-02** | **P0** | `GetApplicationsQueryHandler.cs`, Telemetry Queries | Missing Tenant Filter Fallback When Organization Context is Omitted Returns Cross-Tenant Data | **OPEN** |
| **P13-SEC-03** | **P1** | `EntitlementService.cs`, `BillingController.cs` | Unauthenticated Tenant Entitlement and Usage Inquiries Across Organizations | **OPEN** |
| **P13-SEC-04** | **P1** | `src/SentinelOps.Api/Program.cs` | Automatic Startup Legacy Migration Risk Reassigning Unassigned Entities to `default-org` | **OPEN** |
| **P13-INF-01** | **P1** | Infrastructure / Docker Desktop / WSL2 | Docker Linux Engine Daemon Stopped on Host Environment | **BLOCKED** |
| **P13-OPS-01** | **P2** | Local Host Environment / `docker-compose.yml` | Native PostgreSQL 18 Service Port Collision with Docker Compose Container on Port 5432 | **MITIGATED** |
| **P13-BLD-01** | **P2** | Deployment Tooling / Helm | Helm CLI Missing From Host PATH | **BLOCKED** |
| **P13-PRF-01** | **P2** | Frontend `src/SentinelOps.Web` | Monolithic Client JavaScript Bundle Exceeding 1.3 MB Minified | **OPEN** |
| **P13-DAT-01** | **P2** | PostgreSQL Persistence / Telemetry Queries | Missing Composite Indexes on High-Volume Telemetry Tables Under Multi-Tenant Filtering | **OPEN** |
| **P13-ACC-01** | **P3** | Frontend Accessibility (`ApmPage`, `DashboardsPage`) | Visual Observability Charts Lacking Explicit ARIA Descriptions and Keyboard Traversal | **OPEN** |

---

## Detailed Risk Assessments

### P13-SEC-01: Unverified `X-Organization-Id` Request Header (P0)
- **Affected Component**: `src/SentinelOps.Infrastructure/Services/CurrentTenantService.cs`, `ApplicationsController`, `ApmService`, `DashboardService`, `StatusPageService`.
- **Evidence**:
  ```csharp
  // CurrentTenantService.cs lines 34-38
  if (context.Request.Headers.TryGetValue(OrganizationHeaderKey, out var headerVal) &&
      Guid.TryParse(headerVal, out var parsedHeaderGuid) && parsedHeaderGuid != Guid.Empty)
  {
      return parsedHeaderGuid;
  }
  ```
  `CurrentTenantService` parses and returns `X-Organization-Id` directly without verifying whether `HttpContext.User` is an active member of that organization in `ApplicationDbContext.OrganizationMembers`.
- **Reproduction Steps**:
  1. Authenticate as User A belonging exclusively to Organization A (`Org-A-Guid`).
  2. Issue a request: `GET /api/applications` with header `X-Organization-Id: <Org-B-Guid>`.
  3. `GetApplicationsQueryHandler` reads `_tenantService.OrganizationId` (`Org-B-Guid`) and queries `_context.Applications.Where(a => a.OrganizationId == Org-B-Guid)`.
  4. Response returns Organization B's private applications to User A.
- **Proposed Remediation**:
  Enforce tenant verification. Either in a dedicated `TenantValidationMiddleware` or inside `CurrentTenantService`, query `OrganizationMembers` for `(OrganizationId == requestedOrg && UserId == currentUserId)`. If the user is not a member (and not a global Admin), reject the header or throw a 403 Forbidden exception.
- **Verification Method**:
  Create an automated regression test in `SentinelOps.Api.IntegrationTests` where User A attempts to access Organization B's applications, logs, and APM data using forged `X-Organization-Id`. Ensure 403 Forbidden or empty tenant isolation is returned.
- **Current Status**: **OPEN**

---

### P13-SEC-02: Missing Tenant Filter Fallback When Header is Omitted (P0)
- **Affected Component**: `src/SentinelOps.Application/Applications/Queries/GetApplications/GetApplicationsQueryHandler.cs`, `GetLogsQueryHandler.cs`.
- **Evidence**:
  ```csharp
  // GetApplicationsQueryHandler.cs lines 24-28
  if (_tenantService.OrganizationId.HasValue)
  {
      var orgId = _tenantService.OrganizationId.Value;
      query = query.Where(a => a.OrganizationId == orgId);
  }
  ```
  If `_tenantService.OrganizationId` is null (e.g. no header sent and no claim present), `query` is executed without any organization constraint, returning applications across all tenants.
- **Reproduction Steps**:
  1. Authenticate as a normal member.
  2. Send `GET /api/applications` with NO `X-Organization-Id` header.
  3. The handler returns all applications in the entire database.
- **Proposed Remediation**:
  If `_tenantService.OrganizationId` is null, resolve the calling user's primary/default organization from `OrganizationMembers`. If the user belongs to no organization (and is not a global Admin), return an empty list or 403 Forbidden. Never execute an unconstrained cross-tenant query for non-admin users.
- **Verification Method**:
  Add an integration test asserting that an authenticated user without an organization header only sees their own organization's resources.
- **Current Status**: **OPEN**

---

### P13-SEC-03: Unauthenticated Cross-Tenant Entitlements Inquiries (P1)
- **Affected Component**: `src/SentinelOps.Infrastructure/Services/EntitlementService.cs`, `src/SentinelOps.Api/Controllers/BillingController.cs`.
- **Evidence**:
  `BillingController.GetEntitlements` and `BillingController.GetUsage` accept `orgId` from the request header/query and forward it to `_entitlementService.GetEntitlementsAsync` and `_entitlementService.GetUsageSummaryAsync`. Neither method validates that `userId` is a member of `orgId`.
- **Reproduction Steps**:
  1. Authenticate as User A.
  2. Send `GET /api/billing/entitlements?organizationId=<Org-B-Guid>`.
  3. The endpoint returns Organization B's plan tier, feature flags, quota limits, and usage counts.
- **Proposed Remediation**:
  In `EntitlementService`, verify that `_context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.UserId == userId)` before generating the entitlement DTO. Return an unauthorized/forbidden response if the user is not a member.
- **Verification Method**:
  Integration test sending cross-tenant entitlement query and asserting 403 Forbidden.
- **Current Status**: **OPEN**

---

### P13-SEC-04: Startup Automatic Legacy Data Reassignment to Default Org (P1)
- **Affected Component**: `src/SentinelOps.Api/Program.cs` lines 293–354.
- **Evidence**:
  ```csharp
  var unassignedApps = await dbContext.Applications.Where(a => a.OrganizationId == null).ToListAsync();
  foreach (var appItem in unassignedApps) { appItem.OrganizationId = defaultOrg.Id; }
  var existingUsers = userManager.Users.Where(u => u.IsActive).ToList();
  foreach (var user in existingUsers) { ... isUserAdmin ? Owner : Member ... }
  ```
  On every application startup, any orphan application is automatically assigned to `default-org`, and all active users are added as members. In a production enterprise system, orphan data must not be automatically merged into a shared organization without administrative oversight.
- **Proposed Remediation**:
  Gate the automatic reconciliation behind a configuration flag `MigrationSettings:AutoReconcileLegacyTenants` which defaults to `false` in production. Provide a dedicated, idempotent CLI migration tool or administrative endpoint for controlled legacy migration.
- **Verification Method**:
  Verify that when `AutoReconcileLegacyTenants` is `false`, `Program.cs` does not reassign applications or memberships automatically.
- **Current Status**: **OPEN**

---

### P13-INF-01: Docker Linux Engine Daemon Stopped on Host (P1)
- **Affected Component**: Docker Desktop / WSL2 / Container Runtime.
- **Evidence**:
  `docker version` and `docker info` fail with:
  `Get "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/v1.51/version": open //./pipe/dockerDesktopLinuxEngine: Sistem belirtilen dosyayı bulamıyor.`
  WSL status confirms `docker-desktop` is `Stopped`.
- **Proposed Remediation**:
  1. Launch Docker Desktop on Windows host (`C:\Program Files\Docker\Docker\Docker Desktop.exe`).
  2. Await WSL2 `docker-desktop` transition from `Stopped` to `Running`.
  3. Verify daemon availability using `docker info` and `docker compose version`.
- **Verification Method**:
  Run `docker info` and execute `docker compose config` cleanly.
- **Current Status**: **BLOCKED** (Pending user launch or authorization)

---

### P13-OPS-01: Native PostgreSQL 18 Port Collision (P2)
- **Affected Component**: Host Port 5432 / `docker-compose.yml`.
- **Evidence**:
  `C:\Program Files\PostgreSQL\18\bin\postgres.exe` is actively running and listening on `0.0.0.0:5432` (PID: 6124).
  Starting Docker Compose with container port mapping `"5432:5432"` will result in an `address already in use` error.
- **Proposed Remediation**:
  Option A: When running containers via Docker Compose, set `POSTGRES_PORT=5433` in `.env` or adjust host port mapping.
  Option B: Point local services to the native PostgreSQL instance running on port 5432.
- **Verification Method**:
  Check port availability before starting containerized PostgreSQL.
- **Current Status**: **MITIGATED** (Documented in runbooks and baseline)

---

### P13-BLD-01: Helm Binary Missing From Host PATH (P2)
- **Affected Component**: `deploy/helm/sentinelops`.
- **Evidence**:
  `where.exe helm` returns no matches.
- **Proposed Remediation**:
  Install Helm on Windows via Chocolatey (`choco install kubernetes-helm`), Scoop (`scoop install helm`), or direct binary download from `https://helm.sh`.
- **Verification Method**:
  Run `helm lint deploy/helm/sentinelops`.
- **Current Status**: **BLOCKED**

---

### P13-PRF-01: Large Frontend JavaScript Bundle Chunk (P2)
- **Affected Component**: `src/SentinelOps.Web` (`dist/assets/index-DfSND7y9.js` 1,341 kB).
- **Evidence**:
  Vite production build output warns: `Some chunks are larger than 500 kB after minification`.
- **Proposed Remediation**:
  Implement dynamic imports (`React.lazy()`) for secondary routes (`/apm`, `/ai-observability`, `/reports`, `/billing`, `/audit-logs`) to enable code splitting.
- **Verification Method**:
  Re-run `npm run build` and confirm all chunk sizes are within recommended thresholds (< 500 kB).
- **Current Status**: **OPEN**

---

### P13-DAT-01: High-Volume Telemetry Table Composite Indexes (P2)
- **Affected Component**: `src/SentinelOps.Infrastructure/Persistence/Configurations`.
- **Evidence**:
  Multi-tenant telemetry filtering frequently queries `(ApplicationId, Timestamp)` or joins `Applications` on `OrganizationId`. High-throughput queries benefit from composite indexing on `(ApplicationId, Timestamp DESC)` and `(OrganizationId, IsActive)` on `Applications`.
- **Proposed Remediation**:
  Audit entity configurations and ensure composite indexes exist for all frequent query access paths.
- **Verification Method**:
  Inspect EF Core configuration classes and database migration history.
- **Current Status**: **OPEN**

---

### P13-ACC-01: Visual Chart Accessibility & Contrast (P3)
- **Affected Component**: `src/SentinelOps.Web/src/pages/apm.tsx`, `dashboards.tsx`.
- **Evidence**:
  Complex telemetry SVG diagrams do not have ARIA labels or table-based fallback data for screen readers.
- **Proposed Remediation**:
  Add `role="img"`, `aria-label`, and text summaries for visual widgets.
- **Verification Method**:
  Run Vitest and inspect DOM structure for accessibility attributes.
- **Current Status**: **OPEN**
