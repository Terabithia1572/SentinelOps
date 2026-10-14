# SentinelOps — Phase 12 Checkpoint 12.3: Subscriptions and Billing Engine

## 1. Executive Summary

Checkpoint 12.3 implements the core commercial subscription lifecycle and multi-tenant billing engine for SentinelOps. It establishes deterministic plan progression, self-service tier adjustments, automated invoicing with VAT/tax calculation, and administrative settlement workflows with tamper-evident audit logging.

---

## 2. Subscription Lifecycle State Machine

The subscription lifecycle governs tenant service access through explicit, audited state transitions:

```
[Trialing] ─── (Upgrade / Payment) ────> [Active]
    │                                       │
    ├── (Trial End without Card) ──> [Expired]
    │                                       │ (Payment Failure)
    └── (Customer Cancellation) ──> [Canceled] <───────┐
                                            │          │
                                            v          │
                                       [PastDue] ──────┤
                                            │          │
                                            v          │
                                       [GracePeriod] ──┤
                                            │          │
                                            v          │
                                       [Suspended] ────┘
```

### Transition Safeguards:
- **Concurrency Protection**: Every status transition generates an unpredictable, unique `ConcurrencyToken` (UUIDv4) and updates `UpdatedAtUtc`.
- **Audit History**: All transitions automatically record immutable `SubscriptionHistory` records storing `PreviousStatus`, `NewStatus`, `PreviousPlanId`, `NewPlanId`, `Reason`, `ChangedByUserId`, and `TimestampUtc`.
- **Immediate vs Period-End Cancellation**:
  - `Immediate = false`: Flags `CancelAtPeriodEnd = true`, leaving tenant on Active status until the renewal runner processes period end. The tenant can revoke cancellation at any time via `/api/subscriptions/reactivate`.
  - `Immediate = true`: Transitions immediately to `Canceled` status.

---

## 3. Plan Catalog & Quota Propagation

When a plan change is requested (`/api/subscriptions/change-plan`):
1. **Role Enforcement**: Only organization `Owner` or `Admin` members can modify the subscription.
2. **Quota Propagation**: Limits configured on the target `SubscriptionPlan` are immediately synchronized to `OrganizationSettings`:
   - `MaxApplications`
   - `MaxMembers`
   - `MaxCustomDashboards`
   - `MaxMonthlyLogs`
   - `MaxMonthlyTraces`
   - `RetentionDays`
   - `MaxMonthlyAiTokens`
3. **Audit Event**: Emits a `Subscription.PlanChanged` audit event with previous and target plan tiers.

---

## 4. Invoicing and Tax Engine

The billing engine provides automated invoice drafting and settlement tracking:
- **Deterministic Invoice Numbers**: Formatted as `INV-YYYYMM-XXXXX`.
- **Tax/VAT Calculation**: Integrated `ITaxCalculationService` with country and postal-code based tax rules (e.g., standard rates, US state tax lookups, and tax-exempt B2B flags).
- **Line Items**: Subtotal calculation with explicit tax and feature line items.
- **Payment Transaction Log**: Every partial or full settlement appends an immutable `PaymentTransaction` record linked to the invoice.

---

## 5. Administrative Settlement Flow

For enterprise wire transfers, purchase orders, or offline checks:
- **Authorization**: Endpoint `/api/billing/invoices/{invoiceId}/settle-admin` requires the global `Admin` role (`[Authorize(Roles = Roles.Admin)]`). Standard tenant members and owners receive `403 Forbidden`.
- **Audit Requirement**: Mandatory settlement `Reason` and optional external `PaymentReference` (e.g., `WIRE-987654`).
- **Idempotency**: Prevents settling already-paid invoices.
- **Audit Trail**: Logs `Invoice.SettledAdministratively` in `AuditEvents`.

---

## 6. Verification Evidence

### Integration Test Execution:
```powershell
dotnet test tests/SentinelOps.Api.IntegrationTests/SentinelOps.Api.IntegrationTests.csproj --filter SubscriptionsAndBillingIntegrationTests
```
**Results**:
- `GetPlans_PublicEndpoint_ReturnsActivePlansWithEntitlements`: PASS
- `SubscriptionLifecycle_ChangePlan_Cancel_Reactivate_WorksIdempotently`: PASS
- `BillingAndInvoicing_AdministrativeSettlement_AuditedAndEnforcesAdminRole`: PASS

Total: **3 Passed, 0 Failed, 0 Skipped**.
