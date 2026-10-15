# SENTINELOPS — MULTI-TENANT SECURITY & AUTHORIZATION AUDIT
**Phase 13 Adversarial Security Testing & Tenant Isolation Audit**  
**Repository**: `https://github.com/Terabithia1572/SentinelOps`  
**Baseline Commit**: `31f6fba`  

---

## 1. Executive Summary & Audit Scope

A comprehensive security and adversarial authorization audit was conducted across all SentinelOps API surfaces, services, and middleware layers.

The audit evaluated 15 distinct attack scenarios spanning Insecure Direct Object References (IDOR), cross-tenant data leakage, forged request headers, privilege escalation, refresh token replay, ownership transfer race conditions, SignalR unauthorized group joins, and legacy tenant migration risks.

### Security Posture Scorecard

| Security Domain | Evaluated Surface | Findings / Vulnerabilities | Audit Verdict |
|:---|:---|:---:|:---:|
| **Authentication & Tokens** | JWT Bearer, Identity, Refresh Tokens | Family revocation on replay; Zero clock skew | **PASS** |
| **API Key Security** | OTLP Ingestion, Hash Verification | SHA-256 hash storage; Immediate revocation | **PASS** |
| **RBAC Policy Controls** | Admin, Operator, Viewer Policies | Strict route attribute enforcement | **PASS** |
| **Organization Governance** | Membership, Invites, Role Changes | Last-owner guardrail; Self-transfer blocked | **PASS** |
| **SignalR Broadcasts** | `/hubs/alerts` Real-time Groups | Application & Org membership checked | **PASS** |
| **Tenant Isolation (Direct)** | `OrganizationsController`, `BillingService` | Explicit database membership validation | **PASS** |
| **Header Trust Boundary** | `CurrentTenantService.cs` (`X-Organization-Id`) | **P13-SEC-01**: Unverified header parsed blindly | **FAIL (REMEDIATE)** |
| **Unscoped Fallback Queries**| `GetApplicationsQueryHandler.cs` | **P13-SEC-02**: Omitted header yields cross-tenant data | **FAIL (REMEDIATE)** |
| **Billing Entitlements** | `EntitlementService.cs` | **P13-SEC-03**: Cross-tenant entitlement disclosure | **FAIL (REMEDIATE)** |
| **Legacy Data Migration** | `Program.cs` startup reconciliation | **P13-SEC-04**: Automatic merge of orphan entities | **FAIL (REMEDIATE)** |

---

## 2. Adversarial Attack Scenario Evaluations

### Scenario 1: Insecure Direct Object References (IDOR) via `X-Organization-Id`
- **Attack Vector**: Attacker authenticates as User A (Organization Alpha) and issues requests to `/api/applications`, `/api/dashboards`, `/api/apm/overview`, or `/api/billing/entitlements` providing header `X-Organization-Id: <Organization-Beta-Guid>`.
- **Finding**: **CONFIRMED VULNERABILITY (P13-SEC-01)**. `CurrentTenantService.cs` parses `X-Organization-Id` directly from the request headers without validating whether `HttpContext.User` is registered in `OrganizationMembers` for that organization.
- **Impact**: Cross-tenant visibility into applications, dashboards, APM telemetry, and billing entitlement limits.
- **Remediation**: Enforce server-side membership validation inside `CurrentTenantService` or via a dedicated `TenantContextMiddleware` that confirms `OrganizationMembers.Any(m => m.OrganizationId == parsedOrgId && m.UserId == currentUserId)`.

### Scenario 2: Unscoped Telemetry and Application Listing (Omitted Header)
- **Attack Vector**: Attacker authenticates as a standard Viewer/Operator and issues `GET /api/applications` or `GET /api/logs?applicationId={appId}` omitting the `X-Organization-Id` header.
- **Finding**: **CONFIRMED VULNERABILITY (P13-SEC-02)**. When `_tenantService.OrganizationId` is null, `GetApplicationsQueryHandler` executes `_context.Applications.AsNoTracking()` without filtering by user organization, exposing all registered platform applications across all tenants.
- **Remediation**: When `_tenantService.OrganizationId` is null, query `OrganizationMembers` for the user's primary organization ID. If the user has no organization memberships, return an empty set (`[]`). Never execute an unconstrained database query for non-admin callers.

### Scenario 3: Cross-Tenant Entitlement and Usage Discovery
- **Attack Vector**: Attacker sends `GET /api/billing/entitlements?organizationId=<Target-Org-Guid>`.
- **Finding**: **CONFIRMED VULNERABILITY (P13-SEC-03)**. `BillingController.GetEntitlements` and `GetUsage` pass `(orgId, userId)` to `EntitlementService`, but `EntitlementService.GetEntitlementsAsync` does not check `OrganizationMembers` for `userId`.
- **Remediation**: Check `OrganizationMembers` in `EntitlementService` before returning plan quotas or usage summaries.

### Scenario 4: Privilege Escalation on User Registration
- **Attack Vector**: Caller attempts to supply `Roles: ["Admin", "Operator"]` during `POST /api/auth/register`.
- **Evaluation**: **BLOCKED (PASS)**. `RegisterCommandHandler` ignores incoming role claims and strictly assigns `Roles.Viewer` to newly registered users. Role promotion requires an authorized administrator via `UsersController`.

### Scenario 5: Refresh Token Replay & Token Family Invalidation
- **Attack Vector**: Attacker intercepts an already consumed refresh token and attempts to rotate it.
- **Evaluation**: **BLOCKED (PASS)**. The identity subsystem detects duplicate refresh token consumption, flags a token reuse breach, and immediately revokes all descendant tokens in the family (`SecurityHardeningIntegrationTests.RefreshTokenReplayAttack_ShouldDetectBreachAndInvalidateTokenFamily`).

### Scenario 6: Last-Owner Guardrail and Ownership Transfer Races
- **Attack Vector**: The sole Owner of an organization attempts to leave or demote themselves, or transfer ownership to a non-member or themselves.
- **Evaluation**: **BLOCKED (PASS)**.
  - Transferring to self returns `400 Bad Request` ("Cannot transfer ownership to yourself").
  - Transferring to a non-member returns `400 Bad Request` ("Target user is not a member of this organization").
  - Removing the sole Owner returns `400 Bad Request` ("Cannot remove the only Owner").

### Scenario 7: SignalR Unauthorized Group Joining
- **Attack Vector**: Client connects to `/hubs/alerts` and invokes `JoinOrganization(targetOrgId)` or `JoinApplication(targetAppId)` for a tenant they do not belong to.
- **Evaluation**: **BLOCKED (PASS)**. `AlertsHub.cs` queries `_context.OrganizationMembers` before adding the connection ID to the SignalR broadcast group. Non-members receive a `HubException("Access denied")`.

### Scenario 8: Ingestion Key Revocation Bypass
- **Attack Vector**: Telemetry producer continues streaming OTLP data after an API key is revoked.
- **Evaluation**: **BLOCKED (PASS)**. `ApiKeyAuthFilter` checks `apiKey.IsActive` and `apiKey.Application.IsActive` on every ingestion batch. Revoked keys return `401 Unauthorized` immediately.

---

## 3. Legacy Data & Startup Migration Audit

### Audit of Default Organization Reconciliation in `Program.cs`
- **Location**: `src/SentinelOps.Api/Program.cs` lines 293–354.
- **Current Logic**:
  ```csharp
  var defaultOrg = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Slug == "default-org");
  // If defaultOrg does not exist, creates it.
  var unassignedApps = await dbContext.Applications.Where(a => a.OrganizationId == null).ToListAsync();
  foreach (var appItem in unassignedApps) { appItem.OrganizationId = defaultOrg.Id; }
  var existingUsers = userManager.Users.Where(u => u.IsActive).ToList();
  foreach (var user in existingUsers) {
      // Adds every user as a member of default-org
  }
  ```
- **Finding (P13-SEC-04)**: In a true multi-tenant deployment, automatically assigning orphan applications and un-associated users to a shared `default-org` violates tenant isolation and risks accidental data leakage.
- **Non-Destructive Remediation**:
  1. Add a configuration setting `MigrationSettings:AutoReconcileLegacyTenants` (default `false`).
  2. In production, prevent automatic reassignment. Unassigned applications should remain isolated and accessible only by global cluster administrators until explicitly migrated via an administrative tool.

---

## 4. Required Security Remediations Summary

| Vulnerability ID | Target File | Corrective Action | Severity |
|:---|:---|:---|:---:|
| **P13-SEC-01** | `CurrentTenantService.cs` | Validate membership in `OrganizationMembers` for `X-Organization-Id` | **P0** |
| **P13-SEC-02** | `GetApplicationsQueryHandler.cs` | Fallback to user's primary org; block unconstrained cross-tenant queries | **P0** |
| **P13-SEC-03** | `EntitlementService.cs` | Verify membership before returning tenant entitlements or usage | **P1** |
| **P13-SEC-04** | `Program.cs` | Guard startup legacy auto-reconciliation behind an explicit config flag | **P1** |

---

## 5. Audit Verdict

**Checkpoint 13.3 Status**: **AUDIT COMPLETE — 4 DEFECTS CONFIRMED FOR REMEDIATION (P0: 2, P1: 2)**.
All confirmed defects will receive failing regression tests and minimal corrective patches under Checkpoint 13.9.
