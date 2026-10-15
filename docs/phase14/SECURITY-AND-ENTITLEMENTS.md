# SentinelOps Phase 14 — Authentication, Security & SaaS Entitlements Verification

## 1. Executive Summary

- **Status**: `PASS`
- **Date**: October 15, 2026
- **Scope**: Live verification of authentication lifecycles, JWT policies, refresh token rotation/revocation, API key ingestion authentication, role-based access control (RBAC), and SaaS multi-tenant quota/entitlement enforcement.
- **Target Environment**:
  - Live API: `http://localhost:5100` (.NET 10 ASP.NET Core)
  - PostgreSQL 17: `127.0.0.1:55432` (`sentinelops`)
  - Identity & Security Framework: ASP.NET Core Identity + JWT Bearer + Custom ApiKeyAuthFilter

---

## 2. Authentication & JWT Lifecycle Verification

### 2.1 Token Issuance & Payload Structure
Authentication request to `POST /api/auth/login` with credentials (`admin@sentinelops.local`):
```json
{
  "success": true,
  "data": {
    "userId": "b29af199-4f07-463b-b6df-c2497ed4a614",
    "userName": "admin",
    "email": "admin@sentinelops.local",
    "roles": ["Admin", "Viewer"],
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "hSm3Ql_kzaJfKORJgaot3bUu8mnHwtKsKK7o3XBh_q4gnLg7z3n0UzvQdl8PHXnHL6oIx1Ppdz9fgUTl5ojHoQ",
    "expiration": "2026-10-15T20:28:05.0298427Z"
  }
}
```

### 2.2 Endpoint Authorization & Identity Propagation
- Request to `GET /api/auth/me` with `Bearer <token>`:
  - **Status**: `200 OK`
  - **Returned Identity**: `admin` (`b29af199-4f07-463b-b6df-c2497ed4a614`), `isActive: true`.
- Request to `GET /api/auth/me` with no token:
  - **Status**: `401 Unauthorized`
- Request to `GET /api/auth/me` with malformed / unsigned token:
  - **Status**: `401 Unauthorized`

### 2.3 Refresh Token Rotation & Revocation
1. Call `POST /api/auth/refresh` with valid `refreshToken`:
   - Returns `200 OK` with a newly minted access token and rotated refresh token.
   - Verified that the new access token successfully authorizes requests on `/api/auth/me`.
2. Call `POST /api/auth/revoke` with the active refresh token:
   - Returns `200 OK` (`"data": true`).
3. Re-attempting `POST /api/auth/refresh` using the revoked token:
   - **Status**: `401 Unauthorized` (revocation enforced immediately).

---

## 3. Role-Based Access Control (RBAC) Enforcement

The platform enforces three canonical hierarchical roles:
1. `Admin` (System Administration, tenant settings, role assignment)
2. `Operator` (Service management, telemetry viewing, API key management)
3. `Viewer` (Read-only observability access)

### Verified Policy Matrix:

| Policy | Target Endpoint | Tested User Role | Expected Result | Actual Result | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `RequireOperator` | `GET /api/users` | `Admin` (`admin`) | `200 OK` | `200 OK` (3 users returned) | **PASS** |
| `RequireOperator` | `GET /api/users` | `Viewer` (`alpha_admin`) | `403 Forbidden` | `403 Forbidden` | **PASS** |
| `RequireAdmin` | `PUT /api/users/{id}/roles` | `Viewer` (`alpha_admin`) | `403 Forbidden` | `403 Forbidden` | **PASS** |
| Unauthenticated | `GET /api/users` | Anonymous | `401 Unauthorized` | `401 Unauthorized` | **PASS** |

---

## 4. API Key Authentication & Ingestion Boundary

High-throughput telemetry ingestion endpoints (`/v1/logs`, `/v1/traces`, `/v1/metrics`) are secured via `ApiKeyAuthFilter` with SHA-256 hashed keys stored in PostgreSQL.

### Verified Scenarios:
1. **Missing Key**:
   - `POST http://localhost:5100/v1/logs` with no API key header:
   - `HTTP/1.1 401 Unauthorized` (`Missing API key. Provide 'X-API-Key' header or 'Authorization: Bearer <key>'.`)
2. **Invalid / Corrupted Key**:
   - `POST http://localhost:5100/v1/logs` with `X-API-Key: invalid_key_foo`:
   - `HTTP/1.1 401 Unauthorized` (`Invalid, expired, or revoked API key.`)
3. **Valid Active Key**:
   - Created key via `POST /api/applications/{appId}/keys`:
     - Key ID: `e6941f45-ccc7-491d-9eb9-51e2e754f084`
   - `POST http://localhost:5100/v1/logs` with `X-API-Key: <rawApiKey>`:
   - `HTTP/1.1 200 OK`
4. **Revoked Key**:
   - Revoked key via `POST /api/keys/e6941f45-ccc7-491d-9eb9-51e2e754f084/revoke` (`RevokedAtUtc` written to DB and audited in `AuditEvents`).
   - Immediate re-request to `POST /v1/logs` with previous key:
   - `HTTP/1.1 401 Unauthorized` (`Invalid, expired, or revoked API key.`)

---

## 5. SaaS Entitlements, Plans & Tenant Boundary

### 5.1 Subscription Plans
`GET /api/subscriptions/plans` provides tier definitions and entitlement limits:

| Plan | Price (Monthly) | Max Apps | Max Members | Monthly Logs | Retention | AI Tokens | Custom Reports | SLO Alerts |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Starter** | Free ($0) | 3 | 3 | 100,000 | 7 days | 10,000 | No | No |
| **Pro** | $49.00 | 20 | 25 | 1,000,000 | 30 days | 100,000 | Yes | Yes |
| **Enterprise** | $249.00 | 100 | 100 | 10,000,000 | 90 days | 1,000,000 | Yes | Yes |

### 5.2 Live Tenant Entitlements Query
`GET /api/billing/entitlements` for Acme Corp (`12e8a28f-9a9d-4990-92e7-0c039ec73849`):
```json
{
  "organizationId": "12e8a28f-9a9d-4990-92e7-0c039ec73849",
  "planTier": "Enterprise",
  "subscriptionStatus": "Trialing",
  "isSubscriptionActive": true,
  "daysUntilPeriodEnd": 31,
  "canRegisterApplication": true,
  "canInviteMember": true,
  "canCreateDashboard": true,
  "canIngestTelemetry": true,
  "canExecuteAi": true,
  "allowCustomReports": true,
  "allowSloAlerts": true,
  "allowApiAccess": true,
  "allowDedicatedSupport": true,
  "inactiveReasons": []
}
```

### 5.3 Cross-Tenant Authorization Gating
- Tenant Alpha user (`alpha.admin@sentinelops.local`) requested Acme Corp's billing entitlements:
  - `GET /api/billing/entitlements?organizationId=12e8a28f-9a9d-4990-92e7-0c039ec73849`
  - **Result**: `HTTP/1.1 404 Not Found` (`Organization not found or access denied.`).
- Cross-tenant data leakage is strictly prevented at both the middleware and service layers.
