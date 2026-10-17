# SentinelOps Observability Overview Dashboard Specification

## 1. Executive Summary

Checkpoint 17.3 upgrades the central landing experience (`/` and `/overview`) from a static 8-box grid into a real-time, interactive Command & Operations Center. The page now synthesizes high-density KPI telemetry, overall platform posture, live incident feeds, and rapid topology jumps into a single responsive view.

---

## 2. Architecture & Interactive Controls

### 2.1 Time Range & Dynamic Refresh
- **Time Range Selectors**: `15m`, `1h`, `6h`, `24h`, and `7d` pills allow instant temporal filtering of backend aggregate queries.
- **Auto-Refresh Polling**: Dropdown selector supporting `Off`, `10s`, `30s` (default), and `1m` automated background polling intervals.
- **Manual Sync**: Refresh button with an active rotation animation during data fetching.

### 2.2 System Posture & Platform Health Banner
- Evaluates operational health based on live backend data:
  - **Healthy State**: Rendered when active alerts = 0 and error rate ≤ 5%. Green glowing status indicator (`StatusIndicator` with pulse), `All Core Systems Operational`, and `SLA 99.99%` badge.
  - **Degraded State**: Rendered when alerts are firing or error rates exceed 5%. Amber pulsing indicator, `Elevated Anomaly Alerting Active`, and `Attention Required` badge.
- **Vitals Metrics Block**: Displays healthy/total services ratio (e.g. `12 / 12 Active`), mean response latency (e.g. `42 ms`), and error rate (e.g. `0.15%`).

### 2.3 Interactive High-Density KPI Cards (8 Metrics)
Each KPI card is directly clickable and routes the user into the dedicated diagnostic view:
1. **Applications** -> `/applications`
2. **Active Services** -> `/services`
3. **Active Alerts** -> `/alerts` (dynamically turns red/destructive when count > 0)
4. **Healthy Services** -> `/services` (tracks uptime health checks)
5. **Error Rate** -> `/exceptions` (color-coded: normal < 1%, warning 1-5%, critical > 5%)
6. **Avg Response** -> `/apm` (mean latency in milliseconds)
7. **Logs Today** -> `/logs` (total ingested log records today)
8. **Traces Today** -> `/traces` (distributed trace span volume)

### 2.4 Multi-Panel Operations Row
- **Live Incident Feed (2/3 width)**:
  - Queries `GET /api/incidents?page=1&pageSize=5`.
  - Displays firing incidents with severity dots, incident titles, target service names, status pills, and direct 1-click links to the incident root-cause triage page (`/incidents/:id`).
  - Renders `<EmptyState>` when no incidents are firing (`Zero Active Incident Reports`).
- **Telemetry Topology & Quick Actions (1/3 width)**:
  - Instant navigation to **APM & Service Map** (`/apm`).
  - Instant navigation to **AI Root-Cause Assistant** (`/ai-observability`).
  - Instant navigation to **Structured Log Stream** (`/logs`).
  - Real-time OTel collector ingestion status indicator.

---

## 3. Resilience & Error Handling

- **Loading State**: Uses `OverviewSkeleton` featuring shimmer placeholders for the banner, the 8 KPI cards, and both bottom panels.
- **API Error State**: Utilizes `ErrorState` with retry callback (`refetch()`), preventing page crashes when network connections drop.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/overview.test.tsx`:
  - Verified initial rendering of heading and `.animate-pulse` skeletons.
  - Verified loaded state displaying `All Core Systems Operational`, 8 KPI metrics, live incident feed, and topology jumps.
- Vitest results: **18 test files passed (100%), 69 tests passed**.
- TypeScript type check: **0 errors**.
