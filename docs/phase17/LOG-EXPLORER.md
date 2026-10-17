# SentinelOps Telemetry & Log Explorer Experience

## 1. Executive Summary

Checkpoint 17.5 standardizes the Log Explorer (`/logs`), delivering a high-performance, real-time structured log streaming interface capable of filtering millions of OpenTelemetry collector entries by application slug, microservice identifier, environment stage, severity level, temporal boundary, and trace ID.

---

## 2. Architecture & Capabilities

### 2.1 Multi-Dimensional Filtering & Query Engine
- **Application & Service Cascading**: Selecting an application dynamically queries `/api/logs/metadata` to populate relevant microservices and environments. Supports cross-page deep linking via both `?applicationId={id}` and `?app={slug}`.
- **Severity Pill Filters**: Instant single-click filtering across `All`, `Trace`, `Debug`, `Information`, `Warning`, `Error`, and `Critical`.
- **Temporal Windows**: Flexible window selection (`15m`, `1h`, `6h`, `24h`, `7d`) computing exact UTC ISO timestamps.
- **Trace Correlation**: Direct filtering on `traceId` with 1-click jumps to the distributed waterfall trace view (`/traces?traceId=...`).
- **Full-Text Search**: Substring matching across messages, message templates, and exception details.

### 2.2 Live Streaming & Polling Controls
- **Interval Controls**: Toggle between `Manual`, `5s`, `10s`, and `30s` automated background fetch intervals.
- **Silent Background Updates**: Subsequent auto-refresh polling executes silently without unmounting the table or resetting expanded rows.
- **Manual Refresh**: Explicit trigger button with animated spinner.

### 2.3 Detailed Forensic Inspection
Clicking any log row toggles an inline forensic drawer:
- **Timestamp (UTC)**: Formatted microsecond ISO timestamp.
- **Full Message**: Complete unclipped message with 1-click "Copy" action (accompanied by green `Check` visual feedback).
- **Exception Stack Trace**: High-contrast rose background container with dedicated "Copy Stack" action.
- **Structured Properties**: Prettified, syntax-formatted JSON view with dedicated "Copy JSON" action.

### 2.4 Ergonomic State Handling
- **Loading State**: Replaced unstyled spinner with a multi-column `<Skeleton>` row layout.
- **Empty State**: `<EmptyState>` with 1-click "Clear Filter Query" action.
- **Error State**: `<ErrorState>` with retry callback.

---

## 3. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/observability.test.tsx`:
  - Verified rendering of heading `Log Explorer`.
  - Verified search input placeholder and filter toolbar.
  - Verified log rows with severity badges, service names (`payments-svc`), and error messages.
- Vitest results: **18 test files passed (100%), 70 tests passed**.
- TypeScript type check: **0 errors**.
