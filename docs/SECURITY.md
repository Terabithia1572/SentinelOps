# SentinelOps — Security & Runtime Hardening Policy

This document defines the security architecture, threat model mitigations, and hardening standards implemented in SentinelOps.

---

## 1. Secret Management & Credential Policy

- **Zero Plaintext Credentials in Version Control**: No real database passwords, JWT signing keys, or API tokens are ever committed to Git.
- **Local Development**:
  - Application secrets are managed using .NET User Secrets:
    ```bash
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sentinelops;Username=sentinelops_app;Password=<YourStrongPassword>" --project src/SentinelOps.Api
    dotnet user-secrets set "JwtSettings:Key" "<Your32+CharacterSecretKey>" --project src/SentinelOps.Api
    ```
  - Committed configuration files (`appsettings.json`, `.env.example`) contain strictly placeholder values (`REPLACE_WITH_USER_SECRETS_OR_ENV`).
- **Production Deployments**:
  - Secrets are supplied exclusively via environment variables or secret vaults (e.g. Docker Secrets, Azure Key Vault, Kubernetes Secrets).
  - **Fail-Fast Validation**: In production environments (`ASPNETCORE_ENVIRONMENT=Production`), the API terminates immediately on startup if `JwtSettings:Key` is missing, contains placeholder text, or is shorter than 32 characters (256 bits).

---

## 2. Database Least-Privilege Architecture

- **Elimination of Superuser Access**: The development database connection strings in application configuration no longer reference the `postgres` superuser.
- **Dedicated Application Role**: SentinelOps operates under a dedicated, non-superuser role `sentinelops_app`.
- **Permissions**:
  - The role is granted `CONNECT` on `sentinelops` database and `USAGE, CREATE` on schema `public`.
  - Object permissions are restricted to application tables and sequences (`scripts/setup-db-user.sql`).
  - Superuser operations (`pg_read_file`, administrative cluster management) are inaccessible to the application role.

---

## 3. Identity, Tokens & Session Security

### Short-Lived Access Tokens
- JWT access tokens use HMAC-SHA256 (`HmacSha256`) and expire within 15 minutes.
- Cryptographic clock skew is set to zero (`ClockSkew = TimeSpan.Zero`) to enforce strict expiry boundaries.

### Refresh Token Rotation (RTR) & Breach Detection
- Refresh tokens are 64-byte cryptographically random values, stored exclusively as SHA-256 hashes in PostgreSQL.
- Each refresh request rotates the token: the old token is invalidated and linked to the replacement token hash.
- **Token Replay / Breach Detection**: If an attacker attempts to reuse an already-revoked refresh token, SentinelOps detects the anomaly, invalidates **all active refresh tokens** for that user account, and logs a security audit event (`RefreshTokenReuseBreachDetected`).

### API Key Lifecycle
- Ingestion API keys follow the format: `sent_{environment}_{highEntropySecret}`.
- Database records only `KeyPrefix` (first 16 characters for index lookup) and `KeyHash` (SHA-256 digest).
- Secret comparison is performed using constant-time equality checks (`CryptographicOperations.FixedTimeEquals`).
- Raw secrets are revealed to the user exactly once during creation or rotation and cannot be retrieved thereafter.

---

## 4. HTTP & Browser Protection

### OWASP Security Headers
The API pipeline executes [`SecurityHeadersMiddleware`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Api/Middleware/SecurityHeadersMiddleware.cs) on every response:
- `X-Content-Type-Options: nosniff`: Prevents MIME-sniffing exploits.
- `X-Frame-Options: DENY`: Defends against clickjacking.
- `Content-Security-Policy`: Restricts script and resource execution origins.
- `Referrer-Policy: strict-origin-when-cross-origin`: Restricts referrer data leakage.
- `Permissions-Policy`: Restricts browser hardware access (camera, mic, geolocation).
- `Strict-Transport-Security`: Enforces HTTPS when TLS is enabled.
- Server header disclosure (`Server`, `X-Powered-By`) is stripped.

### Endpoint Authorization Enforcement
- All management endpoints ([`ApplicationsController`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Api/Controllers/ApplicationsController.cs), [`DashboardController`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Api/Controllers/DashboardController.cs), [`ApiKeysController`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Api/Controllers/ApiKeysController.cs)) are strictly protected by `[Authorize]`.
- Mutations require explicit `RequireOperator` or `RequireAdmin` role policies.

---

## 5. Security Regression Tests

Security behaviors are validated by automated integration tests in [`SecurityHardeningIntegrationTests.cs`](file:///c:/Users/Yunus/Desktop/SentinelOps/tests/SentinelOps.Api.IntegrationTests/SecurityHardeningIntegrationTests.cs):
1. `ApplicationsEndpoint_WhenAnonymous_ShouldReturnUnauthorized`: Verifies anonymous requests to `/api/applications` return `401 Unauthorized`.
2. `DashboardSummaryEndpoint_WhenAnonymous_ShouldReturnUnauthorized`: Verifies anonymous requests to `/api/dashboard/summary` return `401 Unauthorized`.
3. `HttpResponses_ShouldContainOwaspSecurityHeaders`: Verifies presence and values of OWASP security headers.
4. `RefreshTokenReplayAttack_ShouldDetectBreachAndInvalidateTokenFamily`: Verifies replay attack detection and user session invalidation.
