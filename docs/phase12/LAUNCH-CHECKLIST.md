# SentinelOps — Phase 12 SaaS Launch Checklist & Pre-Flight Runbook

## 1. Overview
This launch checklist defines the operational, security, infrastructure, and billing prerequisites required to transition SentinelOps from the current verified **CONDITIONALLY READY** staging state into public commercial production (**READY**).

---

## 2. Pre-Flight Verification Gate Status

| Category | Gate Item | Dev / Sandbox Status | Production Launch Requirement | Status |
|----------|-----------|----------------------|--------------------------------|--------|
| **Payment Provider** | Stripe Live API Key | Simulated (`cs_test_*`) | `Payment:Stripe:ApiKey=sk_live_...` in secure vault | ⏳ PENDING PROD DEPLOY |
| **Payment Provider** | Stripe Webhook Secret | Deterministic HMAC Secret | `Payment:Stripe:WebhookSecret=whsec_...` configured in Stripe Dashboard | ⏳ PENDING PROD DEPLOY |
| **Payment Provider** | Zero Card Storage (PCI-DSS) | Verified in unit & integration tests | Verified (No PAN/CVV form fields or tables) | ✅ PASS |
| **Identity & JWT** | Production JWT Key | User-Secrets / Env Var | 256-bit+ random secret; Fail-fast enforced in `Program.cs` | ✅ PASS |
| **Identity & JWT** | Refresh Token Rotation | Verified in integration tests | SHA-256 hashed, automatic RTR breach detection active | ✅ PASS |
| **Infrastructure** | PostgreSQL Database | EF Core 10 relational / InMemory test | Production managed cluster (e.g. AWS RDS / Azure Postgres) with automated daily backups | ⏳ PENDING PROD DEPLOY |
| **Infrastructure** | Redis Streams | StackExchange.Redis / In-memory test | Production Redis Cluster with AOF persistence enabled | ⏳ PENDING PROD DEPLOY |
| **Infrastructure** | OTel Collector | Verified compose pipeline | Kubernetes DaemonSet / Ingress OTLP endpoint configured | ✅ PASS |
| **Security** | HTTPS / TLS 1.3 | Local self-signed / HTTP | Let's Encrypt / Cloudflare Edge TLS certificates active | ⏳ PENDING PROD DEPLOY |
| **Security** | Webhook Anti-Replay | HMAC-SHA256, 300s window | Verified in `SandboxPaymentWebhookVerifierTests` | ✅ PASS |
| **Security** | Deduplication | `(Provider, EventId)` unique index | Verified in `PaymentProviderAndWebhookIntegrationTests` | ✅ PASS |
| **SaaS Engine** | Plan Catalog | Starter ($0), Pro ($49), Enterprise ($249) | Verified canonical seed and EF migration | ✅ PASS |
| **SaaS Engine** | Invoicing Engine | `INV-YYYYMM-XXXXX` + Tax VAT | Verified in `SubscriptionsAndBillingIntegrationTests` | ✅ PASS |
| **SaaS Engine** | Entitlement Enforcer | Server-side hard blocks | Verified in `EntitlementsAndUsageIntegrationTests` | ✅ PASS |
| **Governance** | Ownership Transfer | Owner role promotion/demotion | Verified in `TenantSelfServiceIntegrationTests` | ✅ PASS |
| **Governance** | GDPR Data Portability | JSON archive export | Verified in `TenantSelfServiceIntegrationTests` | ✅ PASS |
| **Governance** | Soft Deletion Safeguard | Exact confirmation phrase | Verified in `TenantSelfServiceIntegrationTests` | ✅ PASS |
| **Frontend UI** | SaaS Customer Portal | React 19 UI in `billing.tsx` | Verified across desktop, tablet, and mobile | ✅ PASS |
| **Automated Tests** | Backend Test Suite | 298 passed / 0 failed | 100% pass rate across Domain, Application, and API tests | ✅ PASS |
| **Automated Tests** | Frontend Test Suite | 54 passed / 0 failed | 100% pass rate across 15 Vitest suites | ✅ PASS |

---

## 3. Production Environment Variable Manifest

Prior to commercial traffic cutover, the following environment variables must be populated within the target container orchestrator (Kubernetes Secrets / AWS Secrets Manager):

```bash
# ==============================================================================
# SentinelOps Production SaaS Environment Configuration
# ==============================================================================

# ASP.NET Core Runtime
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Primary Data Stores
ConnectionStrings__DefaultConnection="Host=postgres-prod.internal;Port=5432;Database=sentinelops_prod;Username=sentinelops_app;Password=<VaultSecret>;SslMode=Require"
ConnectionStrings__Redis="redis-cluster.internal:6379,password=<VaultSecret>,ssl=true,abortConnect=false"

# Cryptographic Token Signing
JwtSettings__Key="<RandomCryptographicallySecure512BitSecretKey>"
JwtSettings__Issuer="https://auth.sentinelops.io"
JwtSettings__Audience="https://api.sentinelops.io"
JwtSettings__ExpirationMinutes=15
JwtSettings__RefreshTokenExpirationDays=30

# Commercial Payment Gateway (Live Mode)
Payment__Provider=Stripe
Payment__Stripe__ApiKey="sk_live_<RealProductionStripeKey>"
Payment__Stripe__WebhookSecret="whsec_<RealProductionWebhookSecret>"
Payment__Stripe__SuccessUrl="https://app.sentinelops.io/billing?status=success"
Payment__Stripe__CancelUrl="https://app.sentinelops.io/billing?status=canceled"

# CORS Origins
CorsSettings__AllowedOrigins__0="https://app.sentinelops.io"
CorsSettings__AllowedOrigins__1="https://admin.sentinelops.io"

# Outbox & Notification Dispatch
Notifications__SmtpHost="smtp.sendgrid.net"
Notifications__SmtpPort=587
Notifications__SmtpUser="apikey"
Notifications__SmtpPassword="<SendGridApiKey>"
Notifications__SenderEmail="billing@sentinelops.io"
Notifications__SenderName="SentinelOps Billing"
```

---

## 4. Production Go / No-Go Decision Rules

1. **NO-GO Condition 1**: If `Payment__Provider` is configured as `Stripe` but `Payment__Stripe__ApiKey` or `Payment__Stripe__WebhookSecret` are empty or placeholder values, the startup pipeline must fall back safely to `DisabledPaymentProvider` (`NOT CONFIGURED / BLOCKED`) to prevent unauthorized activations.
2. **NO-GO Condition 2**: If database connection strings point to superuser `postgres` rather than unprivileged application user `sentinelops_app`.
3. **NO-GO Condition 3**: If `ASPNETCORE_ENVIRONMENT=Production` and `JwtSettings__Key` is shorter than 32 characters or contains default placeholder text.
4. **GO Condition**: When all production cluster secrets are verified, edge SSL certificates are active, DNS points to load balanced ingress, and automated backup schedules are confirmed.
