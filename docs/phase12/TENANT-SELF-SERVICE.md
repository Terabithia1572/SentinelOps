# Phase 12 Checkpoint 12.6 — Tenant Self-Service, Member Management & Account Security Workflows

## 1. Overview
Checkpoint 12.6 establishes secure customer administration, team member lifecycle, ownership transfer, GDPR/tenant data portability export, and soft account deletion safeguards for the SentinelOps multi-tenant SaaS platform.

---

## 2. Architecture & RBAC Safeguards

### 2.1 Role Hierarchy & Permissions
Within each organization, roles are strictly enforced at the API controller and service layers:
- **Owner**: Full administrative control over organization settings, billing, subscriptions, quota limits, member invitations, role promotions/demotions, ownership transfer, and account deletion.
- **Admin**: Can invite members, remove regular members, update non-critical settings, view billing and quota dashboards, and initiate GDPR data exports. Cannot demote/remove owners, transfer ownership, or delete the organization.
- **Member**: Read-only access to monitored telemetry, dashboards, and organization profile. Cannot invite members, modify roles, alter quotas, export archives, or manage billing accounts.

### 2.2 Ownership Transfer Protocol (`POST /api/organizations/{id}/ownership-transfer`)
- **Authorization**: Strictly restricted to the current active `Owner`. Non-owners receive HTTP `400 Bad Request` ("Only the organization Owner can transfer ownership.").
- **Target Validation**:
  - Target user must exist and be an active member of the target organization. Non-members receive HTTP `400 Bad Request` ("Target user is not a member of this organization.").
  - Transferring ownership to oneself is prohibited: HTTP `400 Bad Request` ("Cannot transfer ownership to yourself.").
- **Atomic Role Swap**:
  - Target member's role is promoted to `OrganizationRole.Owner`.
  - Previous owner's role is demoted to `OrganizationRole.Admin`.
- **Audit Logging**: Recorded as `Organization.OwnershipTransferred` with full actor and target user identifiers.

### 2.3 Last-Owner Guardrail
- `RemoveMemberAsync`: If an owner attempts to remove themselves or another owner when no other owner exists, the operation is blocked: HTTP `400 Bad Request` ("Cannot remove the only Owner of the organization. Transfer ownership first.").
- `ChangeMemberRoleAsync`: If an owner attempts to demote the sole owner to Admin or Member, it is blocked: HTTP `400 Bad Request` ("Cannot demote the only Owner of the organization. Transfer ownership first.").

---

## 3. GDPR & Tenant Data Export (`POST /api/organizations/{id}/export`)

### 3.1 Authorization
- Allowed only for `Owner` or `Admin` roles.

### 3.2 Export Schema (`TenantDataExportDto`)
The exported JSON package includes:
1. **Organization Metadata**: Id, Name, Slug, Description, IsActive, CreatedAtUtc.
2. **Settings**: PlanTier, RetentionDays, AllowMemberInvites, RequireTwoFactor, Quotas.
3. **Subscription & Billing**: PlanTier, Status, CurrentPeriodStartUtc, CurrentPeriodEndUtc, CancelAtPeriodEnd.
4. **Team Members**: UserId, UserName, Email, FullName, Role, JoinedAtUtc.
5. **Applications & Topology**: Application details, associated Environments, and registered Services.
6. **Alert Rules**: Rule name, metric, condition type, threshold, enabled state, and creation timestamp.
7. **Dashboards**: Dashboard titles, descriptions, default flags, and shared states.
8. **Audit Trail**: Top 50 recent audit events scoped to the organization.

### 3.3 Audit Logging
- Logged as `Organization.DataExported` with timestamp and requesting actor ID.

---

## 4. Soft Account Deletion Safeguards (`DELETE /api/organizations/{id}/account`)

### 4.1 Strict Confirmation Phrase
- Requires the exact phrase: `CONFIRM DELETE {OrganizationName}`.
- Partial matches or generic strings like `DELETE` or `CONFIRM` fail immediately with HTTP `400 Bad Request`.

### 4.2 Cascading Deactivation Protocol
- **Organization**: Sets `IsActive = false`, `UpdatedAtUtc = DateTime.UtcNow`.
- **Applications**: Iterates through all registered applications in the organization and marks each `IsActive = false`.
- **Subscription**: If an active or trialing subscription exists, transitions status to `SubscriptionStatus.Canceled`, records `CanceledAtUtc = DateTime.UtcNow`, and unsets `CancelAtPeriodEnd`.
- **Pending Invitations**: Revokes all pending invites (`RevokedAtUtc = DateTime.UtcNow`) to prevent unauthorized sign-ups post-deletion.
- **Audit Event**: Emits `Organization.AccountDeleted` for compliance and post-mortem retention.

---

## 5. Verification & Test Evidence

Integration test suite: `tests/SentinelOps.Api.IntegrationTests/TenantSelfServiceIntegrationTests.cs`

| Test Scenario | Description | Status |
|---------------|-------------|--------|
| `TransferOwnership_AsOwner_TransfersOwnershipAndDemotesPreviousOwner` | Verifies atomic owner-to-admin and member-to-owner role swap | PASS |
| `TransferOwnership_AsNonOwner_ReturnsBadRequest` | Verifies non-owners cannot transfer ownership | PASS |
| `TransferOwnership_ToNonMember_ReturnsBadRequest` | Rejects transfer to users not in organization | PASS |
| `TransferOwnership_ToSelf_ReturnsBadRequest` | Rejects transfer to oneself | PASS |
| `RemoveMember_LastOwner_BlockedByGuardrail` | Blocks deletion of the only remaining owner | PASS |
| `ExportTenantData_ReturnsCompleteTenantArchive` | Validates full JSON export archive across all domains | PASS |
| `DeleteAccount_WithIncorrectConfirmationPhrase_Fails` | Blocks deletion when phrase mismatch occurs | PASS |
| `DeleteAccount_WithCorrectPhrase_SoftDeletesOrganizationAndCancelsSubscription` | Verifies cascading deactivation & cancellation | PASS |

- **Integration Test Results**: 8/8 Passed in `TenantSelfServiceIntegrationTests.cs` (148/148 across all integration suites).
