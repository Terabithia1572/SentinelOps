# SentinelOps Phase 15 — Privileged Access & Role Management Audit

## 1. Executive Summary

- **Status**: `PASS`
- **Assessment Date**: October 15, 2026
- **Scope**: Comprehensive audit of ASP.NET Core Identity roles, organization memberships, authorization policies, investigation of direct SQL insertion anti-patterns, and verification of auditable privilege assignment mechanisms.

---

## 2. Investigation of Phase 14 Direct SQL Role Assignment

### 2.1 Context & Root Cause Analysis
During Phase 14 Checkpoint 14.6 verification, the test team investigated why the initial user `admin@sentinelops.local` could not execute `RequireOperator` endpoints. Examination of the database revealed the account was provisioned with only the default `Viewer` role. To test the authorization policies without interrupting ongoing runs, a direct SQL command was executed:
```sql
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('b29af199-4f07-463b-b6df-c2497ed4a614', 'a4c5951c-e917-4875-b943-a69b26f46b2b')
ON CONFLICT DO NOTHING;
```

### 2.2 Security Risks of Direct Database Manipulation
1. **Bypasses Security Stamp Updates**: ASP.NET Core Identity maintains a `SecurityStamp` on `ApplicationUser`. Legitimate role changes invoke `UpdateSecurityStampAsync()`, which causes token validation handlers to reject previously issued JWT tokens. Direct SQL leaves stale tokens active.
2. **Bypasses Audit Logging**: Direct SQL insertions circumvent `IAuditService`, creating privileged accounts without any immutable entry in the `AuditEvents` table.
3. **Bypasses Guardrail Logic**: Bypasses the application-level "Final Administrator" check, which prevents accidental lockout if the last admin is removed.

---

## 3. Auditable Privileged Access Architecture

SentinelOps enforces a dual-layered privilege hierarchy:

```
Platform / System Scope (ASP.NET Core Identity):
├── Admin      (Full platform administration, user management, global audit logs)
├── Operator   (Service oversight, API key management, telemetry analysis)
└── Viewer     (Read-only telemetry exploration)

Tenant / Organization Scope (OrganizationMember):
├── Owner      (Organization governance, billing, member role assignment, deletion)
├── Admin      (Application onboarding, alerts, dashboards, member invites)
└── Member     (Telemetry ingestion, viewer within organization boundaries)
```

### 3.1 Audited Programmatic Role Assignment
Privileged roles must be assigned strictly through the audited API endpoint:
- **Endpoint**: `PUT /api/users/{id}/roles`
- **Authorization**: `[Authorize(Policy = "RequireAdmin")]` (enforces caller possesses `Roles.Admin`).
- **Handler**: `UpdateUserRoleCommandHandler` &rarr; `IdentityService.UpdateUserRolesAsync()`.
- **Enforced Controls**:
  1. **Final Admin Guardrail**: If target user is an Admin, verifies `otherActiveAdmins > 0` before allowing demotion.
  2. **Security Stamp Invalidation**: Calls `await _userManager.UpdateSecurityStampAsync(user)`.
  3. **Audit Trail**: Dispatches structured record to `AuditEvents`:
     ```json
     {
       "action": "User.RoleUpdated",
       "targetEntityType": "User",
       "targetEntityId": "{userId}",
       "outcome": "Success",
       "details": "Roles updated to: Admin, Viewer"
     }
     ```

### 3.2 Tenant-Level Role Management & Anti-Escalation
Organization roles are managed via `PUT /api/organizations/{orgId}/members/{targetUserId}/role`:
- **Caller Guardrail**: Caller must possess `OrganizationRole.Owner`. A tenant `Admin` or `Member` cannot alter member roles.
- **Sole Owner Protection**: Verified that `targetMember.Role == Owner` cannot be changed if `otherOwners == 0`.
- **Self-Escalation Defense**: A tenant `Admin` cannot promote themselves to `Owner`.

---

## 4. Verification & Negative Authorization Tests

A dedicated negative authorization test was implemented in `EnterpriseAdminAndSecurityIntegrationTests.cs`:
- **Test Case**: `UpdateRoles_WhenViewerOrOperator_ShouldReturnForbidden`
- **Scenario**: A user with `Roles.Viewer` submits `PUT /api/users/{viewerId}/roles` attempting to self-escalate to `Roles.Admin`.
- **Result**: `HTTP 403 Forbidden` (`PASS`).
- **Non-Existent User Test**: Submitting role updates for an invalid user returns `HTTP 400 Bad Request` ("User not found.").
- **Last Admin Demotion Test**: `UpdateRoles_WhenDemotingLastAdmin_ShouldEnforceGuardrailAndReject` verified returning `HTTP 400 Bad Request` ("Cannot remove the Administrator role from the last active Administrator.").

---

## 5. Production Admin Bootstrapping Policy

1. **No Hardcoded Production Admins**: Default development passwords (`Password123!`) and accounts (`admin@sentinelops.local`) must never be seeded into staging or production databases.
2. **Controlled First-Run Provisioning**:
   - Initial administrator creation in staging/production is performed through explicit environment-variable seeding (`INITIAL_ADMIN_EMAIL`, `INITIAL_ADMIN_PASSWORD`) during controlled deployment orchestration, or via an administrative CLI runner that invokes `IIdentityService` directly.
   - Direct database SQL modification is strictly prohibited across all operational environments.
