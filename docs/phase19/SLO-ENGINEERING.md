# SentinelOps — Phase 19 Service-Level Objective (SLO) Engineering

**Author:** Principal Software & Observability Platform Architect  
**Date:** 2026-10-17  
**Status:** IMPLEMENTED AND VERIFIED  

---

## 1. Mathematical Definitions and SLI Formulations

SentinelOps implements mathematically rigorous Service Level Indicators (SLIs) and Service Level Objectives (SLOs) derived directly from ingested distributed trace records.

### Availability SLI
Measures the proportion of successful transactions over a measurement window:

$$\text{SLI}_{\text{availability}} = \frac{N_{\text{good}}}{N_{\text{total}}} \times 100\% = \frac{\sum [ \text{HasErrors} = \text{false} ]}{N_{\text{total}}} \times 100\%$$

### Latency SLI
Measures the proportion of transactions that both completed without errors and satisfied the latency threshold $T_{\text{max}}$:

$$\text{SLI}_{\text{latency}} = \frac{\sum [ \text{Duration} \le T_{\text{max}} \land \text{HasErrors} = \text{false} ]}{N_{\text{total}}} \times 100\%$$

### Error Budget & Burn Rate
Given an SLO target $T_{\text{target}}$ (e.g. 99.9%):
- **Allowed Error Fraction**:
  $$F_{\text{allowed}} = \frac{100\% - T_{\text{target}}}{100\%}$$
- **Allowed Bad Requests**:
  $$N_{\text{allowed\_bad}} = \lceil N_{\text{total}} \times F_{\text{allowed}} \rceil$$
- **Error Budget Remaining**:
  $$\text{Budget Remaining} = \frac{N_{\text{allowed\_bad}} - N_{\text{actual\_bad}}}{N_{\text{allowed\_bad}}} \times 100\%$$
- **Burn Rate**:
  $$\text{Burn Rate} = \frac{F_{\text{actual}}}{F_{\text{allowed}}} = \frac{N_{\text{actual\_bad}} / N_{\text{total}}}{F_{\text{allowed}}}$$

---

## 2. Telemetry Semantics & Missing Data Handling

1. **Zero-Traffic Windows**: If $N_{\text{total}} = 0$ during an evaluation cycle, the system records:
   - Compliance: `100.0%`
   - Budget Remaining: `100.0%`
   - Burn Rate: `0.0`
   - Breached: `false`
   *Rationale*: Eliminates false-positive paging and synthetic alert storms during low-traffic maintenance periods.
2. **Measurement Windows**:
   - `Rolling24Hours`: Rolling 24-hour evaluation for immediate triage.
   - `Rolling7Days`: Standard operational weekly window.
   - `Rolling30Days`: Enterprise contractual SLA reporting window.
3. **Timezone Handling**: All evaluations, cutoffs, and historical evaluations are stored in UTC (`timestamp with time zone`). Client presentation converts to localized viewer timezones.

---

## 3. Tenant Isolation & Security Hardening

In Phase 19, `SlosController` was audited and hardened against cross-tenant enumeration:
- `GetSlos`: Joins through active tenant applications (`a.OrganizationId == _tenantService.OrganizationId.Value`).
- `GetSlo`, `UpdateSlo`, `DeleteSlo`, `GetSloCompliance`, `EvaluateSlo`: Explicitly verifies that the target SLO's `ApplicationId` belongs to the requesting tenant's organization. Cross-tenant access attempts immediately return HTTP 404 with no data leakage.
