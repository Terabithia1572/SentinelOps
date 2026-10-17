# SentinelOps Multi-Tenant Organizations, Teams & RBAC Specification

## 1. Executive Summary

Checkpoint 17.11 standardizes the multi-tenant SaaS organization management (`/organizations`) and enterprise user administration (`/users`) experiences. It replaces all unstyled browser prompts (`window.confirm()`) with accessible design system dialogs (`<Dialog>`, `<ConfirmDialog>`), provides structured empty and skeleton loading states, enforces role-based access control (Owner, Admin, Member, Viewer, Operator), and provides real-time SaaS resource capacity metering.

---

## 2. Multi-Tenant Organization Management (`/organizations`)

### 2.1 Tenant Context & Switching
- `useTenant()` context provides the active organization (`activeOrg`), active organizations list (`organizations`), and tenant switching mechanism (`setActiveOrg`).
- Automatically filters all downstream telemetry, application dashboards, and alert feeds by tenant ID.

### 2.2 Tabbed Management Experience
- **Team Members Tab (`members`)**:
  - Displays user profile, assigned role pill (Owner / Admin / Member / Viewer), and joined timestamp.
  - Role management select for organization owners.
  - Accessible member removal with `<ConfirmDialog open={!!memberToRemove} ... variant="destructive">`.
  - Empty state `<EmptyState>` when no members exist; `<Skeleton>` rows during fetch.
- **Pending Invitations Tab (`invites`)**:
  - Displays outstanding cryptographic invitations.
  - Revocation action and expiration countdowns.
- **Tenant Security & Settings Tab (`settings`)**:
  - Toggle member self-invites.
  - Require two-factor authentication (2FA).
  - Default data retention period (days).
- **Resource Usage & Quotas Tab (`usage`)**:
  - Capacity progress bars with percentage utilization: Max Applications, Team Seats, Custom Dashboards, Monthly Logs, Distributed Traces.
  - Threshold alert styling (amber when > 80%, rose when exceeded).

### 2.3 Accessible Dialogs
- **Create Organization Modal (`<Dialog>`)**: Captures name, slug, and description.
- **Invite Member Modal (`<Dialog>`)**: Generates 7-day cryptographic invitation token with one-click copy confirmation.
- **Configure Plan Quotas Modal (`<Dialog>`)**: Adjusts tier ceilings (Starter, Pro, Enterprise).

---

## 3. Platform User Directory & Security Administration (`/users`)

### 3.1 User Directory & Filtering
- Search accounts by email or username.
- Filter by Role (Admin, Operator, Viewer) and Status (Active, Inactive, Locked).
- `<Skeleton>` table rows during API queries.
- `<EmptyState>` with search guidance when no accounts match filters.

### 3.2 Administrative Governance Modals
- **Role Assignment Dialog (`<Dialog>`)**: Reassigns platform roles with safety guardrail preventing admin lockout.
- **Manage Account Status Dialog (`<Dialog>`)**: Activates, deactivates, locks (24h), or unlocks accounts.
- **Revoke Sessions Confirm Dialog (`<ConfirmDialog variant="destructive">`)**: Forcibly revokes refresh tokens and triggers security stamp renewal with loading feedback (`isLoading`).
- **Reset Password Dialog (`<Dialog>`)**: Admin password override with confirmation and validation.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/organizations.test.tsx` (4 tests):
  - Verified organization directory header and Create Organization button.
  - Verified tabs (Members, Invitations, Settings, Usage).
  - Verified SaaS quotas and resource usage progress bars in usage tab.
- `src/SentinelOps.Web/src/pages/__tests__/admin-and-audit.test.tsx` (4 tests):
  - Verified UsersPage directory, security modals, role updating, and audit log inspector.
- Vitest suite results: **19 test files passed (100%), 76 tests passed**.
- TypeScript type check: **0 errors**.
