# SentinelOps Phase 15 — Authentication & Session Security Deep Audit

## 1. Executive Summary

- **Status**: `PASS`
- **Assessment Date**: October 15, 2026
- **Scope**: Comprehensive security review of token lifecycles, refresh token rotation, session invalidation, account lockout, rate limiting, password policies, CORS, CSRF/XSS vectors, browser token storage, security headers, and error masking.
- **Automated Verification**: 24 / 24 security integration tests passed in `SecurityHardeningIntegrationTests.cs`.

---

## 2. Authentication Lifecycle & Token Security Matrix

### 2.1 JWT Validation Controls
Configured in `src/SentinelOps.Api/Program.cs`:
- **Algorithm**: HMAC-SHA256 with minimum 256-bit (32-character) secret keys.
- **Clock Skew**: Strict `TimeSpan.Zero` (eliminates default 5-minute leeway window).
- **Claim Verification**: `ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`, `ValidateIssuerSigningKey = true`.
- **Active User / Lockout Check**:
  ```csharp
  options.Events = new JwtBearerEvents
  {
      OnTokenValidated = async context =>
      {
          var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
          var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (!string.IsNullOrEmpty(userId))
          {
              var user = await userManager.FindByIdAsync(userId);
              if (user == null || !user.IsActive)
                  context.Fail("User is inactive or deleted.");
              if (await userManager.IsLockedOutAsync(user))
                  context.Fail("User account is locked.");
          }
      }
  };
  ```
  *Impact*: Deactivated or locked accounts are blocked immediately on their next request, even if they hold an unexpired JWT.

### 2.2 Refresh Token Rotation (RTR) & Breach Invalidation
- **Cryptographic Generation**: 64 cryptographically secure random bytes (`RandomNumberGenerator.GetBytes(64)`), Base64URL-encoded.
- **Storage**: Only SHA-256 hashes are persisted in the `RefreshTokens` database table.
- **Rotation**: On every refresh (`POST /api/auth/refresh`), the used token is marked `IsUsed = true`, and a new pair is issued.
- **Replay Attack Defense**: If an already-used refresh token is presented again (indicating token theft or replay), the platform detects the breach, revokes the entire user token family, and blocks the request (`401 Unauthorized`).

### 2.3 Account Lockout & Brute-Force Rate Limiting
- **Account Lockout**: After 5 consecutive failed login attempts, the account is locked for 15 minutes (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`).
- **IP Rate Limiting**: The `/api/auth/*` route family is protected by ASP.NET Core Fixed Window Rate Limiting (`auth-rate-limit`, 60 requests/minute per client IP partition).

---

## 3. Browser Token Storage Evaluation: `localStorage` vs. `HttpOnly` Cookies

A critical requirement of Checkpoint 15.4 is analyzing the existing browser token storage approach before modifying authentication topology.

### 3.1 Architectural Comparison

| Dimension | `localStorage` + `Authorization: Bearer` (Current) | `HttpOnly` SameSite Cookies |
| :--- | :--- | :--- |
| **CSRF Exposure** | **Zero CSRF Vulnerability**. Browsers never automatically attach custom `Authorization` headers to cross-origin requests. | **High CSRF Vulnerability**. Browsers automatically attach cookies to cross-origin requests unless complex anti-CSRF tokens are maintained. |
| **Cross-Origin / Staging Flexibility** | Seamless across subdomains (e.g. `app.staging.sentinelops.io` talking to `api.staging.sentinelops.io`). | Difficult across subdomains; requires `SameSite=None` + `Secure`, which weakens third-party tracking protection in modern browsers. |
| **Mobile & Native API Parity** | 100% unified contract for SPAs, mobile apps, and CLI tools. | Requires dual authentication handlers (cookie for web, bearer for API). |
| **XSS Exposure** | Vulnerable if malicious JavaScript executes in the DOM. | Cookie payload is inaccessible to JavaScript via `document.cookie`. |

### 3.2 Engineering Decision & Threat Defense
- **Conclusion**: Migrating to `HttpOnly` cookies would introduce Cross-Site Request Forgery (CSRF) vectors and complicate staging multi-subdomain deployments without eliminating XSS risks (an XSS attacker can simply use the browser's fetch API to make requests on behalf of the user regardless of cookie visibility).
- **Threat Defense for `localStorage`**:
  1. **Content-Security-Policy (CSP)**: Strict headers disallow untrusted inline scripts and external domains (`script-src 'self'`).
  2. **React JSX Auto-Escaping**: React 19 natively sanitizes untrusted inputs against DOM-based XSS.
  3. **Short-Lived Access Tokens**: 15-minute access token lifespan minimizes the window of opportunity if a token is intercepted.
  4. **Token Family Invalidation**: Any attempt to replay a refresh token immediately invalidates all active sessions.

---

## 4. OWASP Security Headers & Error Masking

### 4.1 Enforced Security Headers (`SecurityHeadersMiddleware.cs`)
- `X-Content-Type-Options: nosniff` (prevents MIME-type sniffing)
- `X-Frame-Options: DENY` (clickjacking defense)
- `Referrer-Policy: strict-origin-when-cross-origin` (prevents leaking query parameters to third parties)
- `X-XSS-Protection: 0` (modern standard per OWASP, relies on CSP instead of flawed legacy XSS auditors)
- `Permissions-Policy: accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()`
- `Content-Security-Policy: default-src 'self'; img-src 'self' data:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self' ws: wss:; frame-ancestors 'none';`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload` (enforced when request is HTTPS)
- Strips `Server` and `X-Powered-By` headers to prevent framework profiling.

### 4.2 Sensitive Error Masking (`ExceptionHandlingMiddleware.cs`)
- Returns standard RFC 7807 `ProblemDetails` (`application/problem+json`).
- Internal database details, SQL queries, and stack traces are suppressed in non-development environments, returning:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "Internal Server Error",
    "status": 500,
    "detail": "An unexpected error occurred. Please try again later.",
    "instance": "/api/..."
  }
  ```

---

## 5. Verification Summary

All 24 security tests in `SecurityHardeningIntegrationTests.cs` passed, confirming:
- Immediate rejection of expired and tampered tokens.
- Breach detection and family invalidation on refresh token replay.
- Account lockout after 5 consecutive failed attempts.
- CSP and OWASP headers present on HTTP responses.
- SSRF filtering blocking loopback (`127.0.0.1`), private RFC 1918 subnets, and cloud metadata endpoints (`169.254.169.254`).
