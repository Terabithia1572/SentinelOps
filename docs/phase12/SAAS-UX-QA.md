# Phase 12 Checkpoint 12.7 — SaaS Admin Dashboard & Customer Experience (React 19)

## 1. Overview
Checkpoint 12.7 delivers the SaaS Administration Dashboard and customer management frontend within SentinelOps (`src/SentinelOps.Web`), enabling multi-tenant subscription management, transparent plan comparison, usage monitoring against hard server quotas, itemized invoice inspection, and account governance.

---

## 2. Page Architecture & Routing

### 2.1 Route Registration (`src/SentinelOps.Web/src/App.tsx`)
- Registered routes:
  - `/billing` & `/subscription` &rarr; `<BillingPage />` (enclosed within `<ProtectedRoute>` and `<MainLayout>`).
- Navigation integration (`src/SentinelOps.Web/src/components/layout/sidebar.tsx`):
  - Added `Billing & Plans` link with the `CreditCard` icon adjacent to `Team` and `Settings`.

### 2.2 Tabbed Management Interface (`src/SentinelOps.Web/src/pages/billing.tsx`)
The `BillingPage` features 5 dedicated management tabs:

1. **Subscription Overview**:
   - Displays the current active subscription plan (Starter, Pro, Enterprise).
   - Lifecycle status badge: `Active` (Emerald), `Trial Period` (Blue), `Payment Past Due` (Amber), `Grace Period (7 Days)` (Orange), `Suspended` (Rose), `Canceled` (Gray).
   - Billing cadence (Monthly / Annual) and renewal / cancellation period-end timestamps.
   - Immediate actions: Upgrade Plan, Customer Billing Portal, Cancel / Reactivate Subscription.
   - Quick visual meters for month-to-date log ingestion, trace ingestion, and AI Copilot tokens.

2. **Plans & Pricing**:
   - Side-by-side comparison of the 3 canonical tiers:
     - **Starter ($0/mo)**: 3 apps, 5 seats, 100k logs, 50k traces, 50k AI tokens, 7 days retention.
     - **Pro ($49/mo)**: 20 apps, 25 seats, 1M logs, 500k traces, 500k AI tokens, 30 days retention, custom reports.
     - **Enterprise ($249/mo)**: 100 apps, 100 seats, 10M logs, 5M traces, 2M AI tokens, 90 days retention, 24/7 priority SLA support.
   - Interactive modal confirmation before switching plan tiers.
   - Sandbox checkout session simulation (`cs_test_*`) directly launching test checkout flows.

3. **Usage & Quotas**:
   - Real-time capability matrix displaying actual resource consumption against server-enforced quotas.
   - Visual progress bars with adaptive color indicators:
     - Normal consumption (< 80%): Primary theme color.
     - Warning state (&ge; 80%): Amber warning badge (`80% Quota Warning`).
     - Hard limit exceeded (&ge; 100%): Rose badge (`100% Exceeded`).
   - Feature entitlement grid: AI Copilot analysis status, custom SLA reports, API key capabilities, retention period.

4. **Invoices & Billing History**:
   - Chronological table of tax-compliant SaaS invoices (`INV-YYYYMM-XXXXX`).
   - Columns: Invoice Number, Issuance Date, Billing Cycle Period, Total Amount with Currency, Tax / VAT Breakdown, Status (`Paid`, `Open`, `Void`, `Uncollectible`).
   - Detailed modal breakdown displaying itemized line items, unit prices, subtotal, applicable tax rate %, and total settled.

5. **Governance & Security**:
   - **Ownership Transfer**: Select an active team member and execute atomic role promotion (`Owner`) and demotion (`Admin`).
   - **GDPR Tenant Data Portability**: One-click download of the complete tenant JSON archive (`exportTenantData`).
   - **Danger Zone / Account Deletion**: Soft account deletion workflow requiring the exact confirmation phrase `CONFIRM DELETE {org.name}`, deactivating applications and canceling active subscriptions.

---

## 3. Sandbox Transparency & Security Disclaimers

> [!IMPORTANT]
> A permanent informational banner is displayed prominently atop the billing dashboard:
> **"Deterministic SaaS Sandbox Environment: All payment flows, Stripe checkout sessions, customer billing portals, and invoice settlements are simulated via authorized test providers. No real payment credentials or live charges are ever stored or processed."**

- **Zero PAN/CVV Exposure**: The UI contains no credit card input fields, preventing PCI-DSS scope creep.
- **External Redirection**: Real payment gateway handoffs use hosted Stripe / provider portals rather than embedded card forms.

---

## 4. Frontend Verification & Quality Gates

### 4.1 Vitest Component Tests (`src/pages/__tests__/billing.test.tsx`)
| Test Case | Verification Target | Status |
|-----------|---------------------|--------|
| `renders deterministic sandbox alert banner` | Asserts presence of sandbox explanation banner | PASS |
| `renders subscription overview and current plan badge` | Asserts header, active plan badge, and renewal dates | PASS |
| `renders canonical plans and allows switching tabs` | Asserts Starter, Pro, Enterprise cards, prices, and feature limits | PASS |
| `renders usage and quota bars with warning and exceeded badges` | Asserts 80% warning and 100% exceeded badge rendering | PASS |
| `renders itemized invoice history and allows opening details modal` | Asserts invoice table, itemized breakdown modal, tax % | PASS |
| `renders governance tab and validates soft account deletion phrase` | Asserts transfer dropdown, GDPR export, and phrase-locked deletion button | PASS |

### 4.2 Quality Gate Summary
- **Typecheck**: `tsc -b --noEmit` &rarr; 0 errors (`PASS`).
- **Unit & Component Tests**: 15 test files passed, 54 tests passed (`PASS`).
- **Production Bundle**: `vite build` &rarr; built in 595ms (`PASS`).
