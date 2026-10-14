# SentinelOps — Checkpoint 11.7: AI Observability Dashboard & Incident Copilot UX

## 1. Executive Summary

Checkpoint 11.7 establishes the primary enterprise user experience for SentinelOps Phase 11: an interactive, tenant-scoped **AI Observability & Incident Intelligence** dashboard (`/ai-observability`) and **Incident Copilot** assistant. 

The user interface bridges complex mathematical models (Rolling Z-Score, Robust Median/MAD, Ordinary Least Squares Linear Regression) and deterministic heuristics with intuitive operator workflows. Critical human-in-the-loop safeguards are prominently integrated across every surface: AI recommendations and remediation actions explicitly require human review and confirmation, guaranteeing that autonomous actions cannot destabilize production environments.

---

## 2. Dashboard Architecture & Navigation

The AI Observability view is integrated into SentinelOps's core navigation layout:
- **Sidebar Navigation**: Dedicated `AI Observability` link with the `Sparkles` icon placed alongside APM & Service Map and Dashboards.
- **Multi-Tenant Scoping**: Leverages `useTenant` and application selectors, restricting displayed anomalies, log patterns, RCA summaries, and capacity trajectories to the authorized tenant context.
- **AI Quota & Usage Widget**: High-level status bar detailing:
  - Active provider engine (`Local Heuristics / LLM`).
  - Month-to-date invocations.
  - Token consumption versus monthly budget (e.g. `12,500 / 500,000`).
  - Real-time estimated cost tracking in USD.

---

## 3. Dedicated Intelligence Views

### 3.1. Statistical Anomalies View
- **Telemetry Signals**: Visualizes detected deviations across latency, error rate, throughput, and CPU/memory utilization.
- **Statistical Explainability**: Exposes baseline values, observed spikes, standard deviation scores ($\sigma$), and confidence ratings.
- **Detection Method Badges**: Explicitly distinguishes between `RobustMedianMAD` and `RollingZScore` algorithms.
- **Operator Resolution Action**: Allows operators to review and mark anomalies as resolved with one click via `POST /api/anomalies/{id}/resolve`.

### 3.2. Intelligent Log Patterns Explorer
- **Template Clustering**: Displays clustered log structures with standardized variable masks (`<NUM>`, `<UUID>`, `<IP>`, `<URL>`, `<HEX>`, `<PATH>`).
- **Frequency & Lineage**: Details total occurrence counts, first seen / last seen timestamps, and sample log previews.
- **Distributed Trace Drilldown**: Surfaces representative `sampleTraceId` links enabling immediate transition into the distributed trace waterfall.
- **On-Demand Analysis Trigger**: Operator controls to cluster historical logs across configurable windows (1h to 24h).

### 3.3. Root Cause Analysis (RCA) Investigation Card
- **Calibrated Confidence Levels**: Employs strictly defined badges:
  - `Likely cause` (High confidence, corroborating telemetry evidence).
  - `Possible cause` (Medium confidence, potential contributing factor).
  - `Insufficient evidence` (Low confidence, inconclusive telemetry).
- **Dual Evidence Chains**: Displays both supporting evidence (e.g. specific exception logs and slow trace IDs) and contradictory evidence side-by-side.
- **Human Verification Controls**: Integrated operator verification workflow with custom notes input and "Verify" / "Reject" status toggles.

### 3.4. Predictive Capacity Forecasts View
- **OLS Trend Trajectory**: Displays current metric values, predicted values at target horizons (+15m, +30m, +60m), and calculated trend slopes ($\Delta/\text{min}$).
- **95% Confidence Bounds**: Reports mathematical error margins $[CI_{\text{lower}}, CI_{\text{upper}}]$ derived from residual variance.
- **Threshold Breach Countdown**: Projects the exact UTC timestamp when a metric will exceed critical capacity limits, rendering an alert countdown badge.
- **On-Demand Evaluation Form**: Allows operators to submit custom horizon and threshold parameters to forecast any service metric.

### 3.5. Incident Copilot Assistant
- **Mandatory Safety Disclaimer Banner**:
  > **Safety Boundary:** All AI-generated remediation commands require explicit human operator confirmation. Commands are never executed automatically by the platform.
- **Prompt Injection Defense**: Evaluates user prompts against hardened sanitization boundaries and isolates telemetry evidence within inert context tags.
- **Structured Response Display**:
  - Narrative root cause analysis and diagnosis.
  - Working hypothesis with confidence score.
  - Safety check verification badge (`Safety Checks: Passed`).
  - Actionable remediation steps with risk-level assessments.

---

## 4. Verification & Automated Test Evidence

### Frontend Unit & Component Tests (`src/pages/__tests__/ai-observability.test.tsx`):
- `renders page header and token quota cards accurately`: PASS.
- `renders active statistical anomalies with severity badge and details`: PASS.
- `switches to Log Patterns tab and renders clustered templates`: PASS.
- `switches to Predictive Forecasts tab and renders OLS trajectory with 95% CI`: PASS.
- `switches to Incident Copilot and displays safety disclaimer banner and handles question`: PASS.

### Test Suite Execution Summary:
```text
Test Files  13 passed (13)
     Tests  44 passed (44)
  Duration  2.36s
```

### Production Build Verification:
```text
npm run build (tsc -b && vite build): 0 errors, 0 warnings.
```

---

## 5. Checkpoint Verdict

**VERDICT: PASS**
- Full-featured, responsive, accessible AI Observability dashboard implemented in React 19.
- Five dedicated intelligence views (Anomalies, Log Patterns, RCA, Forecasts, Copilot) operational.
- Strict human-in-the-loop safeguards and safety banners enforced.
- 100% test pass rate across all frontend and backend suites.
