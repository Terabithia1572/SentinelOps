# SentinelOps SaaS Subscription & Billing Experience Specification

## 1. Executive Summary

Checkpoint 17.12 standardizes the SaaS Subscription, Billing, and Account Governance experience (`/billing`). It verifies end-to-end integration with mock and sandbox payment flows, confirms zero plaintext credential leakage, implements accessible modal dialogs (`<Dialog>`, `<ConfirmDialog>`) for plan transitions and subscription cancellations, and delivers itemized invoice inspection.

---

## 2. Subscription & Billing Architecture

### 2.1 Sandbox Safety & Boundary Controls
- **Deterministic SaaS Sandbox Banner**: Visible security banner informing operators that all payment flows, Stripe checkout sessions, and invoice settlements execute in authorized sandbox mode with zero live credit card processing.
- **Role Enforcement**: Billing portal actions, plan changes, and cancellations are strictly restricted to `Owner` and `Admin` tenant roles.

### 2.2 Tabbed Workflows
- **Subscription Overview (`overview`)**:
  - Displays current plan badge, renewal date, and billing interval (Monthly / Annual).
  - Quick action to open Customer Billing Portal.
- **Plans & Pricing (`plans`)**:
  - Three canonical tiers: **Starter ($0/mo)**, **Pro ($49/mo)**, and **Enterprise ($199/mo)**.
  - Plan comparison cards highlighting maximum applications, team seats, monthly log capacity, distributed traces, and AI token limits.
  - Plan change confirmation powered by `<ConfirmDialog variant="default" ...>` with immediate quota update details.
- **Usage & Quotas (`usage`)**:
  - Resource capacity progress bars: Monitored Applications, Active Team Members, Custom Dashboards, Monthly Ingested Logs, Monthly Traces, and Monthly AI Tokens.
  - Color-coded threshold warnings: Amber pill at ≥ 80% quota, Rose pill at 100% capacity exceeded.
- **Invoices & History (`invoices`)**:
  - Formatted tabular list of tax-compliant itemized billing statements with status pills (Paid, Open, Pending).
  - `<Skeleton>` table loader during fetch; `<EmptyState>` when no invoices exist.
  - Accessible invoice inspection drawer `<Dialog className="max-w-lg">` displaying subtotal, tax rate, and total settlement.
- **Account Governance & Security (`governance`)**:
  - Primary ownership transfer dialog (`<Dialog>`).
  - Tenant data export.
  - Guarded, audited soft-delete account workflow requiring explicit confirmation phrase (`CONFIRM DELETE <OrgName>`).

---

## 3. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/billing.test.tsx` (6 tests):
  - Verified canonical plan cards, monthly/annual pricing details.
  - Verified tab switching across Overview, Plans, Usage, Invoices, Governance.
  - Verified quota progress meters with warning and exceeded badges.
  - Verified plan upgrade confirmation dialog and mutation dispatch.
  - Verified cancellation modal radio selections and execution.
  - Verified itemized invoice modal rendering and breakdown.
- Vitest suite results: **19 test files passed (100%), 76 tests passed**.
- TypeScript type check: **0 errors**.
