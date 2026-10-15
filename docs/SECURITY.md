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

---

## 9. Kubernetes & Cloud-Native Hardening (Phase 10)

- **NSA/CISA Kubernetes Hardening Baseline**:
  - `runAsNonRoot: true` with dedicated unprivileged UIDs (`1000` for .NET API and Worker, `101` for Nginx Web frontend).
  - All Linux capabilities stripped (`capabilities.drop: ["ALL"]`).
  - Privilege escalation strictly disabled (`allowPrivilegeEscalation: false`).
  - Seccomp profile enforced: `seccompProfile: { type: RuntimeDefault }`.
  - ServiceAccount tokens detached: `automountServiceAccountToken: false` prevents token exfiltration.
- **Zero-Trust Network Policies**:
  - `deploy/helm/sentinelops/templates/networkpolicy.yaml` restricts inter-pod communication. Workers accept 0 inbound traffic, API only allows Ingress on 8080, and Redis only allows connections from API and Worker pods.
- **Reverse Proxy Protection (`UseForwardedHeaders`)**:
  - Ingress controller proxy IPs are handled via `ForwardedHeadersOptions` with `KnownIPNetworks.Clear()`, ensuring real client IP extraction for rate limiters, security audit trails, and HSTS/HTTPS detection.
- **Production Fail-Closed Durability Policy**:
  - In production (`RequireDurableStorage = true`), when Redis is unavailable, telemetry ingestion endpoints fail closed with HTTP 503 (`Service Unavailable`) and `Retry-After: 5` header, preventing silent data loss into volatile in-memory buffers.
  - Pods report `Unhealthy` on `/health/ready` during buffer outages to be removed from Ingress routing.

---

## 10. AI Data Protection, Secret Redaction & Prompt Injection Defenses (Phase 11)

### Zero Raw Secret Transmission
Before any telemetry is formatted into diagnostic context or processed by AI providers, it passes through `DeterministicAiProvider.ScrubSensitiveData`:
- API Keys matching `sent_(live|test)_[a-zA-Z0-9]{16,}` are replaced with `[REDACTED_API_KEY]`.
- Bearer tokens matching `Bearer\s+[a-zA-Z0-9_\-\.]{20,}` are replaced with `[REDACTED_TOKEN]`.
- Database connection strings (`Server=...;Password=...`) are replaced with `Password=[REDACTED_CREDENTIAL]`.
- Stored XSS defense: Log templates and user inputs are HTML-encoded via `WebUtility.HtmlEncode`.

### Prompt Injection Defense & Quarantine
- Telemetry evidence is quarantined inside inert `<telemetry_evidence>` tags with explicit system instructions directing the model never to follow instructions found within telemetry strings.
- Inbound inquiries are inspected for prompt injection patterns (`ignore previous instructions`, `system prompt`, `delete all`) and safely neutralized without reflecting malicious instructions back to the client.

### Mandatory Human-in-the-Loop Safeguard
- AI-generated remediation commands are never executed automatically (`RequiresHumanApproval = true`).
- The operator UI renders an amber safety boundary banner mandating human operator verification and confirmation.

### Tenant-Scoped AI Quotas
- `AiUsageMeter` tracks monthly token consumption per organization against `OrganizationSettings.MaxMonthlyAiTokens`.
- Quota exceeded conditions prevent further token usage, eliminating runaway cloud LLM billing risks.

---

## 11. Enterprise SaaS Security & Payment Architecture (Phase 12)

### Zero Cardholder Data Environment (CDE) Storage
- SentinelOps implements a strict zero-card policy: PAN, CVV, and credit card numbers are never accepted, processed, transmitted, or persisted on SentinelOps servers.
- All checkout and payment operations leverage external hosted checkout sessions (Stripe Checkout) or deterministic sandbox redirects.
- Client applications and web UI forms do not contain payment card inputs, preventing PCI-DSS compliance scope expansion.

### Cryptographic Webhook Signatures & Replay Prevention
- Incoming payment webhooks (`POST /api/webhooks/payments/{provider}`) require HMAC-SHA256 signature verification in the header (`t={timestamp},v1={hash}`).
- Verification enforces a maximum 300-second (5-minute) clock drift tolerance to prevent replay attacks.
- Constant-time comparison is executed via `CryptographicOperations.FixedTimeEquals` to prevent side-channel timing analysis.

### Idempotency & Deduplication
- Every received webhook event is recorded in the `PaymentWebhookEvents` table with a database-enforced unique index on `(Provider, EventId)`.
- Duplicate delivery attempts from payment gateways are safely recognized and returned as HTTP 200 OK without double-processing or corrupting subscription state.

### Server-Side Entitlement & Quota Hard Blocks
- All SaaS plan resource limits (Applications, Members, Dashboards, Ingestion, AI Tokens) are enforced at the command/service layer in addition to UI feedback.
- Tenant delinquency (PastDue, Suspended, Canceled, Expired) immediately gates resource provisioning while granting a 7-day grace window for telemetry continuity during transient card declines.

### Tenant Self-Service, GDPR Portability & Deletion Safeguards
- **Ownership Transfer**: Strictly restricted to the organization Owner; atomically promotes new owner to Owner and demotes old owner to Admin with full audit tracking.
- **Last-Owner Guardrails**: Prevents an organization from becoming orphaned by rejecting removal or demotion of the sole remaining owner.
- **GDPR Export**: Allows Owners/Admins to generate an immediate, comprehensive JSON export of all organizational assets.
- **Guarded Soft Deletion**: Requires typing the exact confirmation phrase `CONFIRM DELETE {org.name}`, cascading to deactivating applications and canceling active subscriptions.

---

## 12. Multi-Tenant Authorization & IDOR Defense (Phase 13 Hardening)

### Cryptographic Identity & Organization Membership Validation
- **Defense Against Header Spoofing (IDOR)**: In multi-tenant environments, callers cannot access or mutate resources by forging or altering the `X-Organization-Id` HTTP header. All query handlers (`GetApplicationsQueryHandler`, `GetApplicationByIdQueryHandler`) and infrastructure services (`ApmService`, `DashboardService`, `StatusPageService`) verify that the authenticated caller (`_currentUserService.UserId`) maintains an active membership in `_context.OrganizationMembers` for the requested organization. Non-member callers receive empty results or `404 Not Found`.
- **Unconstrained Query Fallback Elimination**: When `X-Organization-Id` is omitted, queries dynamically restrict results to the collection of organizations where the user has active membership, preventing tenant data leakage in multi-tenant environments.
- **Entitlement Privacy Enforcement**: Billing, plan quotas, and usage consumption summaries (`/api/billing/entitlements`, `/api/billing/usage`) are strictly isolated; callers cannot probe or inspect another organization's subscription status.
- **Legacy Migration Isolation Gating**: Automatic reassignment of orphan entities to `default-org` is gated behind an explicit configuration switch `MigrationSettings:AutoReconcileLegacyTenants` (default `false`), ensuring database integrity in production environments.

---

## 13. Phase 15 Hardening, Secret Hygiene & Staging Release Gates

### Secret Parameterization & Continuous Scanning
- **Automated Gitleaks CI Gate**: Secret scanning runs as a required check on every pull request and push to `main` (`.github/workflows/ci.yml`), blocking any commits containing private keys, JWT secrets, database connection strings, or cloud tokens.
- **E2E Credentials Decoupling**: Browser testing suites (`auth.setup.ts`) strictly read administrator credentials via environment variables (`E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`), preventing hardcoded test credentials in version control.
- **Lingering Secret Revocation**: Verified revocation of historical development and benchmark keys in PostgreSQL (`RevokedAtUtc` set; subsequent ingestion attempts rejected with HTTP 401).

### Privileged Role Hierarchies & Negative Authorization
- **Strict Role Boundaries**: Three explicit roles (`Admin`, `Operator`, `Viewer`) are enforced. Administrative operations (user role updates, API key revocation, tenant plan changes) strictly require the `Admin` role.
- **Negative Testing Enforcement**: Integration test suite `EnterpriseAdminAndSecurityIntegrationTests.cs` validates that `Operator` and `Viewer` requests to mutate system roles return HTTP 403 Forbidden.

### Decoupled DDL Migrations & Multi-Replica Race Condition Defense
- **Explicit Migration CLI**: Runtime web API process boot does not apply EF Core DDL migrations. Migrations must be run via explicit CLI commands (`--apply-migrations`, `--migrate-only`, `--migrate-database-only`) or Kubernetes Helm pre-upgrade hooks (`helm.sh/hook: pre-install,pre-upgrade`).
- **Fail-Fast Staging/Production Verification**: In `Staging` and `Production` environments, the API terminates immediately on boot if any pending EF Core migrations or model snapshot divergences exist, preventing split-brain states or silent schema drift across horizontal replicas.

### Authentication Rate Limiting Scoping
- **Endpoint-Specific Limiting**: Brute-force rate limiting (`auth-rate-limit`, 60 req/min) is strictly scoped to `POST /api/auth/login` and `POST /api/auth/register`. Authenticated session verification (`GET /api/auth/me`) does not share this limiter, preventing false-positive session drops during rapid client-side routing.






