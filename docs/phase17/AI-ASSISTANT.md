# SentinelOps AI Observability & Root-Cause Analysis Specification

## 1. Executive Summary

Checkpoint 17.10 standardizes the AI Observability suite (`/ai-observability`) and the AI Diagnostic Assistant embedded in the Incident Command Center (`/incidents/:id`). It connects telemetry streams, anomaly models, log clusters, predictive forecasting, and generative root-cause analysis to real backend APIs, providing deterministic heuristic fallbacks when LLM quotas or keys are unavailable, and adhering to strict human-in-the-loop safety boundaries.

---

## 2. AI Observability Center (`/ai-observability`)

### 2.1 Domain Workflows & Tabs
- **Statistical Anomalies (`anomalies`)**:
  - Unsupervised algorithms (Z-Score, Isolation Forest) detecting metric outliers.
  - Severity classification (Critical / Warning / Info), confidence ratings, and deviation standard deviations (`σ`).
  - Loading skeleton state `<Skeleton className="h-28" />` and clean `<EmptyState>` when metrics are within normal baseline.
  - Manual anomaly resolution (`POST /api/ai/anomalies/{id}/resolve`).
- **Log Pattern Mining (`patterns`)**:
  - Clustered log stream templates with variable token masking (Draining algorithm).
  - Frequency metrics and sample extracted logs.
  - On-demand analysis buttons: "Analyze Last 1h", "Analyze 24h".
- **Root Cause Analysis (`rca`)**:
  - Telemetry evidence correlation against target incident GUIDs.
  - Generates ranked root-cause hypotheses with confidence scores, impacted services, and evidence points.
  - Interactive hypothesis verification with operator notes.
  - `<Skeleton>` feedback during synthesis and contextual `<EmptyState>` when awaiting analysis target.
- **Capacity Projections (`forecasts`)**:
  - Ordinary Least Squares (OLS) linear regressions forecasting resource exhaustion.
  - Displays slope rates, breach projections, and R² goodness-of-fit scores.
- **Observability Copilot (`copilot`)**:
  - Natural language chat assistant with contextual telemetry queries.
  - Enforces mandatory **Human-in-the-Loop Safety Boundary** banner: AI suggested commands are never executed automatically without explicit operator confirmation.

### 2.2 Token Quota & Cost Tracking
- Real-time token consumption cards: Daily Tokens, Monthly Tokens, Estimated Spend ($ USD), and Plan Quota Bar.

---

## 3. Incident Diagnostic Assistant (`/incidents/:id`)

### 3.1 Live Heuristic Synthesis & Triage
- Automated multi-signal analysis (`POST /api/ai/analyze-incident`):
  - Synthesizes error log spikes, trace waterfall anomalies, and metric deviations.
  - Executive diagnostic summary with primary root-cause hypotheses.
  - Correlated logs and anomalous metrics breakdown.
  - Historical similarity matching against previous incidents.
- Remediation Guidance (`POST /api/ai/suggest-remediation`):
  - Actionable terminal commands and configuration fixes.
  - Copy command with clipboard verification.
  - Operator thumbs-up / thumbs-down feedback loop (`POST /api/ai/incident-feedback`).

### 3.2 Postmortem Generator Modal (`<Dialog>`)
- Automatically generates comprehensive markdown postmortems (`POST /api/ai/generate-postmortem`).
- Rendered in accessible `<Dialog className="max-w-3xl">` with formatted monospaced preview.
- One-click "Copy Markdown" action with visual confirmation for GitHub, Jira, or Confluence export.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/ai-observability.test.tsx` (5 tests):
  - Verified token quota cards, tab switching (Anomalies, Patterns, RCA, Forecasts, Copilot), and Copilot chat interaction.
- `src/SentinelOps.Web/src/pages/__tests__/incident-intelligence.test.tsx` (5 tests):
  - Verified AI diagnostic card rendering, hypothesis synthesis, remediation suggestions, feedback rating, and postmortem generation.
- Vitest suite results: **19 test files passed (100%), 76 tests passed**.
- TypeScript type check: **0 errors**.
