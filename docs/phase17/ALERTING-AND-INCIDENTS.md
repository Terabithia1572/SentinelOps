# SentinelOps Alerting & Incident Management Experience Specification

## 1. Executive Summary

Checkpoint 17.8 standardizes the Alerting (`/alerts`) and Incident Management (`/incidents`, `/incidents/:id`) workflows. It eliminates unstyled/native browser prompts, establishes accessible modal dialogs for operational triage, integrates design system primitives (`<Skeleton>`, `<EmptyState>`), and provides full auditability and trace correlation for incidents.

---

## 2. Alerting Engine (`/alerts`)

### 2.1 Tabbed Rule & Incident Management
- **Alert Rules Tab**:
  - Displays configured threshold and anomaly rules with metric name, evaluation window, cooldown period, and enabled/disabled status.
  - Quick action to trigger manual rule evaluation (`/api/alerts/rules/{id}/evaluate`).
- **Triggered Incidents Tab**:
  - Active incidents originating from automated alert rule firings.
  - Status badges: `Firing`, `Acknowledged`, `Resolved`.
  - Filter by Application and Status.

### 2.2 Operational Triage Modals
- **Acknowledge Modal (`<Dialog>`)**:
  - Captures operator note.
  - Invokes `POST /api/alerts/incidents/{id}/acknowledge`.
  - Accessible dialog with autofocus, keyboard dismissal (`Esc`), and background overlay.
- **Resolve Modal (`<Dialog>`)**:
  - Captures resolution notes.
  - Invokes `POST /api/alerts/incidents/{id}/resolve`.

### 2.3 State Handling
- Integrated `<Skeleton>` loading state for rules and incidents table while fetching.
- `<EmptyState>` with context-aware message when no alert rules or incidents match active filters.

---

## 3. Incident Command & Triage (`/incidents` & `/incidents/:id`)

### 3.1 Incident Index & Filtering
- KPI summary cards: Active Incidents, Critical P1s, Mean Time to Resolve (MTTR), and Escalated Incidents.
- Filters: Status (`ALL`, `Triggered`, `Acknowledged`, `Resolved`), Severity (`Critical`, `Error`, `Warning`, `Information`), and Application.
- Direct links to **Incident Detail Command Center** (`/incidents/{id}`) and correlated traces.

### 3.2 Declare Incident Dialog (`<Dialog>`)
- Replaces raw inline state with accessible modal dialog `<Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen} title="Declare New Incident">`.
- Required fields: Incident Title, Severity, Affected Application.
- Optional forensic fields: Service Name, Environment, Correlated Trace ID, Description & Impact.
- Disables submission during pending mutations with loading feedback (`"Declaring..."`).

### 3.3 Loading & Empty States
- Replaced custom spinners with standardized `<Skeleton>` blocks matching tabular row heights.
- Render `<EmptyState>` with `ShieldAlert` icon when no incidents match query parameters.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/alerting.test.tsx`:
  - Verified AlertRules tab rendering and active alert rule display (`High CPU Usage`).
  - Verified Incident acknowledgment and resolution workflow triggers.
- `src/SentinelOps.Web/src/pages/__tests__/incident-intelligence.test.tsx`:
  - Verified Incident Intelligence card and diagnostic assistant.
- Vitest suite results: **18 test files passed (100%), 70 tests passed**.
- TypeScript type check: **0 errors**.
