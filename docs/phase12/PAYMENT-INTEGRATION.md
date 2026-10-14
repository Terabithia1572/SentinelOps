# SentinelOps — Phase 12 Checkpoint 12.4: Payment Provider Integration & Webhook Security Architecture

## 1. Executive Summary

Checkpoint 12.4 establishes the payment gateway integration architecture and webhook security engine for SentinelOps. It implements zero raw card credential storage, deterministic sandbox payment simulation, cryptographic HMAC-SHA256 webhook signature verification, strict timestamp tolerance, and database-level event deduplication.

---

## 2. Gateway Provider Abstraction & Fail-Safe Modes

### Provider Architecture
```
                         ┌─────────────────────┐
                         │  IPaymentProvider   │
                         └──────────┬──────────┘
                                    │
           ┌────────────────────────┼────────────────────────┐
           │                        │                        │
           v                        v                        v
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│SandboxPaymentProvider│  │DisabledPaymentProvider│ │Stripe (Adapter)     │
│(Test & Local Dev)   │  │(Safety Default)     │  │(LIVE BLOCKED IF     │
│                      │  │                     │  │ UNCONFIGURED)       │
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

- **Zero PAN/CVV Storage Guarantee**: SentinelOps never accepts, processes, transmits, or persists credit card numbers or CVVs. All transactions use external tokens, checkout sessions, and webhook callbacks.
- **Fail-Safe Mode**: If `PaymentSettings:Provider` is set to `Stripe` but no secret key is provided, the system automatically resolves `DisabledPaymentProvider` with an explicit `BLOCKED / NOT CONFIGURED` status.
- **Sandbox Provider**: Provides deterministic test checkout sessions (`cs_test_*`), customer portal redirects, simulated refunds (`re_test_*`), and test webhook signature generation.

---

## 3. Cryptographic Webhook Security Engine

The webhook ingress endpoint (`POST /api/billing/webhooks/{provider}`) is protected by multi-layer cryptographic safeguards:

### 1. Cryptographic HMAC-SHA256 Verification
- **Header Parsing**: Evaluates standard `t={timestamp},v1={hash}` format (Stripe-compatible).
- **Signed Payload**: Computes HMAC-SHA256 of `"{timestamp}.{rawPayload}"` using the configured webhook secret.
- **Constant-Time Comparison**: Uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.

### 2. Replay & Timestamp Freshness Protection
- **Tolerance Window**: 300 seconds (5 minutes). Events older than 300 seconds are rejected with `400 BadRequest`.
- **Clock Drift Bounds**: Events stamped > 60 seconds in the future are rejected.

### 3. Database-Level Deduplication
- **Unique Constraint**: `PaymentWebhookEvents` table enforces a database-level unique index on `(Provider, EventId)`.
- **Idempotent Handling**: If an event with the same ID was already processed, the engine returns `200 OK` with `IsDuplicate = true`, safely acknowledging delivery without double-applying subscription changes or crediting invoices twice.

---

## 4. Webhook Event Handlers

| Event Type | Action Taken |
|---|---|
| `checkout.session.completed` | Validates organization, upgrades plan to target tier, transitions subscription to `Active`, synchronizes quotas to `OrganizationSettings`, records audit event. |
| `invoice.payment_succeeded` / `invoice.paid` | Locates invoice by provider ID or invoice number, marks status `Paid`, records immutable `PaymentTransaction`, recovers `PastDue` / `GracePeriod` subscription to `Active`. |
| `invoice.payment_failed` | Transitions active subscription to `PastDue`, initiates 7-day grace period, queues high-priority alert into `NotificationOutbox` for the tenant billing admin. |
| `customer.subscription.deleted` | Transitions subscription to `Canceled` status. |

---

## 5. Administrative Refunds

Endpoint `POST /api/billing/transactions/{transactionId}/refund`:
- Enforces global `Admin` role (`[Authorize(Roles = Roles.Admin)]`). Standard tenant users receive `403 Forbidden`.
- Generates a distinct negative `PaymentTransaction` record.
- Reduces `AmountPaid` on the associated invoice and reopens the invoice if partially refunded.
- Writes an immutable audit entry to `AuditEvents`.

---

## 6. Verification Evidence

### Webhook Cryptographic Unit Tests:
```powershell
dotnet test tests/SentinelOps.Application.Tests/SentinelOps.Application.Tests.csproj --filter SandboxPaymentWebhookVerifierTests
```
- Valid signature and current timestamp: **PASS**
- Forged signature rejection: **PASS**
- Tampered payload detection: **PASS**
- Expired timestamp rejection (> 300s): **PASS**
- Future timestamp rejection (> 60s): **PASS**
- Missing / malformed header rejection: **PASS**

Total: **6 Passed, 0 Failed**.

### Payment & Webhook Integration Tests:
```powershell
dotnet test tests/SentinelOps.Api.IntegrationTests/SentinelOps.Api.IntegrationTests.csproj --filter PaymentProviderAndWebhookIntegrationTests
```
- `CreateCheckoutSession_ReturnsValidSessionWithSimulatedUrl`: **PASS**
- `HandleWebhook_CheckoutSessionCompleted_ActivatesSubscription`: **PASS**
- `HandleWebhook_InvoicePaymentSucceeded_MarksInvoicePaidAndRecordsTransaction`: **PASS**
- `HandleWebhook_InvoicePaymentFailed_TransitionsActiveToPastDueWithGracePeriod`: **PASS**
- `HandleWebhook_DuplicateEvent_IsIdempotentAndReturns200WithoutDuplicating`: **PASS**
- `HandleWebhook_ForgedSignature_Returns400BadRequest`: **PASS**
- `HandleWebhook_ExpiredTimestamp_Returns400BadRequest`: **PASS**
- `RefundTransaction_AdminOnly_ProcessesRefundAndAdjustsInvoice`: **PASS**

Total: **8 Passed, 0 Failed**.
