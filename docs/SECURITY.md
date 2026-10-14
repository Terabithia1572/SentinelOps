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

---

## 6. Telemetry Ingestion Security & Tenant Boundaries

- **Strict Identity Derivation**: Ingestion endpoints (`/v1/logs`, `/v1/traces`, `/v1/metrics`) require valid application API keys via `X-API-Key` or `Authorization: Bearer <key>`.
- **Anti-Spoofing & Cross-App Isolation**: Any application identifier submitted inside the OTLP payload body (`Resource.Attributes`) is untrusted and discarded. Telemetry records are bound exclusively to the application identified by the authenticated API key hash in PostgreSQL.
- **Payload Size Limits**: Enforced `[RequestSizeLimit(10 * 1024 * 1024)]` (10 MB) prevents memory exhaustion and denial-of-service through excessive payload batches.
- **Telemetry Anti-Spoofing Test**: Validated by `IngestLogs_WithSpoofedApplicationAttributes_StrictlyAttributedToKeyOwner` in `OtlpIngestionIntegrationTests.cs`.

---

## 7. Webhook Security & Server-Side Request Forgery (SSRF) Mitigation

To prevent attackers from using the SentinelOps alert dispatch system to probe internal services or cloud infrastructure metadata APIs, webhook notifications are strictly validated by [`SsrfValidator`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Infrastructure/Security/SsrfValidator.cs):

### DNS Pre-Resolution & IP Filtering
1. **Scheme Enforcement**: Only `https://` and `http://` protocols are allowed (`ftp://`, `file://`, `gopher://` are strictly rejected).
2. **DNS Pre-Resolution**: Hostnames are resolved to IP addresses via system DNS prior to connecting.
3. **Blacklisted IP Ranges**: All resolved IPv4 and IPv6 addresses are checked against security boundaries:
   - **Loopback Addresses**: `127.0.0.0/8` and `::1` (prevents loopback requests to local databases, Redis, or internal microservices).
   - **Cloud Provider Metadata Endpoints**: `169.254.169.254` and link-local range `169.254.0.0/16` / `fe80::/10` (prevents IMDS / instance metadata credential harvesting on AWS, Azure, GCP, and DigitalOcean).
   - **Private RFC 1918 LANs**: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16` (prevents probing corporate intranets or VPC subnets).
   - **Carrier-Grade NAT & Benchmark**: `100.64.0.0/10` and `198.18.0.0/15`.

### Payload Authentication & Integrity
- **HMAC-SHA256 Signatures**: When a webhook channel is configured with a shared secret, SentinelOps signs the exact UTF-8 JSON request body with `HMAC-SHA256` and attaches the signature in the `X-SentinelOps-Signature` HTTP header (formatted as `sha256=<hex-digest>`).
- **Timestamp Anti-Replay**: The delivery payload includes an ISO-8601 UTC timestamp for receiver anti-replay verification.
- **SSRF Test Suite**: Verified by unit test suite in `tests/SentinelOps.Application.Tests/Security/SsrfValidatorTests.cs`.

---

## 8. Multi-Tenant SaaS Isolation & Boundary Verification

- **Strict Server-Side Organization Scoping**: `ICurrentTenantService` resolves the active tenant from authenticated claims and the `X-Organization-Id` header, requiring verified active membership in `OrganizationMembers`.
- **Query Hardening**: All relational queries across Logs, Traces, Metrics, Exceptions, Service Health, Alert Rules, and Incidents strictly enforce `WHERE "ApplicationId" IN (SELECT "Id" FROM "Applications" WHERE "OrganizationId" = @TenantOrgId)`.
- **SignalR Hub Group Authorization**: `/hubs/alerts` verifies tenant ownership before permitting clients to join `app-{id}` or `org-{id}` groups. Cross-tenant group join attempts throw `HubException ("Access denied")`.
- **Cross-Tenant Regression Tests**: Validated by integration test suites in `MultiTenantTelemetryPipelineIntegrationTests.cs` and `AlertingAndSignalRMultiTenantIntegrationTests.cs`.


