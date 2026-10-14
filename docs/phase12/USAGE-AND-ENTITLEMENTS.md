# SentinelOps — Phase 12 Checkpoint 12.5: Usage Metering, Quota Management & Entitlement Enforcement

## 1. Executive Summary

Checkpoint 12.5 implements a unified, centralized entitlement and usage metering architecture for SentinelOps. It unifies Phase 8 organization quota tracking and Phase 11 AI usage metering into an authoritative `IEntitlementService` engine that enforces hard limits across applications, member seats, custom dashboards, telemetry ingestion, and AI token consumption.

---

## 2. Entitlement & Quota Enforcement Architecture

### Centralized Evaluation
All resource mutation and ingestion flows delegate to `IEntitlementService.CheckFeatureAccessAsync(orgId, feature)`:

```
                            ┌────────────────────────┐
                            │  IEntitlementService   │
                            └───────────┬────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           │                            │                            │
           v                            v                            v
┌──────────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│ Applications Command │    │ Organization Invites │    │  Custom Dashboards   │
│ (MaxApplications)    │    │ (MaxMembers)         │    │ (MaxCustomDashboards)│
└──────────────────────┘    └──────────────────────┘    └──────────────────────┘
           │                            │                            │
           v                            v                            v
┌──────────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│ Telemetry Ingestion  │    │  AI Inference Meter  │    │ Feature Flags        │
│ (Logs, Traces, Metric│    │ (MaxMonthlyAiTokens) │    │ (Reports, SLOs, API) │
└──────────────────────┘    └──────────────────────┘    └──────────────────────┘
```

### Hard Limit Enforcements
1. **Monitored Applications**:
   - Evaluated in `CreateApplicationCommandHandler`.
   - Rejects with `400 Bad Request` ("Application limit reached (X/Y) for the {Tier} plan") when current active apps equal or exceed `MaxApplications`.
2. **Member Seats & Invitations**:
   - Evaluated in `OrganizationService.InviteMemberAsync`.
   - Prevents sending invites when `(activeMembers + pendingInvites) >= MaxMembers`.
3. **Custom Dashboards**:
   - Evaluated in `DashboardService.CreateDashboardAsync`.
   - Rejects with `400 Bad Request` ("Custom dashboard limit reached (X/Y) for the {Tier} plan").
4. **AI Observability Budget**:
   - Evaluated in `AiUsageMeter.CanExecuteAiOperationAsync` and `EntitlementService`.
   - Throttles / rejects anomaly detection and RCA LLM inferences when monthly consumption exceeds `MaxMonthlyAiTokens`.
5. **Feature Flag Gating**:
   - `AllowCustomReports`: Restricts PDF/CSV scheduled reporting to Pro and Enterprise tiers.
   - `AllowSloAlerts`: Restricts Service Level Objective alerting to Pro and Enterprise tiers.
   - `AllowDedicatedSupport`: Restricts premium SLA features to Enterprise tiers.

---

## 3. Subscription Status Gating

| Status | Ingestion Permitted | Resource Creation (Apps/Dashboards/Invites) | AI Operations |
|---|---|---|---|
| **Active** / **Trialing** | Yes (within quota) | Yes (within quota) | Yes (within quota) |
| **PastDue** (in 7d Grace) | Yes (grace period) | No (Blocked: Payment past due) | No (Paused) |
| **Suspended** | No | No (Blocked: Account delinquent) | No |
| **Canceled** | No | No (Blocked: Subscription canceled) | No |
| **Expired** | No | No (Blocked: Trial expired) | No |

---

## 4. Automated Monthly Usage Rollover

- `RecordUsageAsync(Guid orgId, MeteredMetric metric, long quantity)`:
  - Evaluates `DateTime.UtcNow` against `OrganizationUsage.LastResetAtUtc`.
  - If month or year differs:
    - Automatically resets `LogsIngestedMonthToDate`, `TracesIngestedMonthToDate`, `MetricsIngestedMonthToDate`, `ApiRequestsMonthToDate`, and `AiTokensUsedMonthToDate` to 0.
    - Sets `LastResetAtUtc = DateTime.UtcNow`.
  - Atomically increments metric consumption for the new billing cycle.

---

## 5. Tenant Endpoints

### 1. `GET /api/billing/entitlements`
Returns real-time capability matrix for the authenticated tenant:
- `IsSubscriptionActive`
- `DaysUntilPeriodEnd`
- `CanRegisterApplication`, `CanInviteMember`, `CanCreateDashboard`, `CanIngestTelemetry`, `CanExecuteAi`
- `AllowCustomReports`, `AllowSloAlerts`, `AllowApiAccess`, `AllowDedicatedSupport`
- `InactiveReasons`

### 2. `GET /api/billing/usage`
Returns consumption vs plan quota breakdown with percentage metrics and warnings:
- `LogsUsed` / `LogsQuota` / `LogsPercent` / `LogsExceeded`
- `TracesUsed` / `TracesQuota` / `TracesPercent` / `TracesExceeded`
- `MetricsUsed` / `MetricsQuota` / `MetricsPercent` / `MetricsExceeded`
- `AiTokensUsed` / `AiTokensQuota` / `AiTokensPercent` / `AiTokensExceeded`
- `ApplicationsUsed` / `ApplicationsQuota`
- `MembersUsed` / `MembersQuota`
- `DashboardsUsed` / `DashboardsQuota`
- `HasAnyQuotaWarning` (>= 80% capacity)
- `HasAnyQuotaExceeded` (>= 100% capacity)

---

## 6. Verification Evidence

### Integration Test Suite:
```powershell
dotnet test tests/SentinelOps.Api.IntegrationTests/SentinelOps.Api.IntegrationTests.csproj --filter EntitlementsAndUsageIntegrationTests
```
- `GetEntitlements_ReturnsValidTenantMatrix`: **PASS**
- `GetUsageSummary_CalculatesPercentagesAndWarningsAccurately`: **PASS**
- `EnforceQuota_ApplicationsLimit_RejectsWhenQuotaReached`: **PASS**
- `EnforceQuota_MemberSeats_RejectsWhenQuotaReached`: **PASS**
- `EnforceQuota_CustomDashboards_RejectsWhenQuotaReached`: **PASS**
- `EnforceQuota_SubscriptionSuspended_BlocksResourceCreation`: **PASS**
- `UsageMetering_MonthlyRollover_ResetsMonthToDateCounters`: **PASS**

Total: **7 Passed, 0 Failed**.
Full Integration Test Run: **140 Passed, 0 Failed**.
