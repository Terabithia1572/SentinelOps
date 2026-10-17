# SentinelOps Metrics, Dashboards & Visualization Experience

## 1. Executive Summary

Checkpoint 17.7 standardizes the metrics and dashboard visualization suite across `/metrics` and `/dashboards`. The metrics engine correlates OpenTelemetry time-series telemetry into responsive throughput, latency percentile (P50/P95/P99), and error rate charts, while the dashboard ecosystem provides a 12-type customizable widget catalog with JSON import/export, template instantiation, and auto-refresh orchestration.

---

## 2. Architecture & Capabilities

### 2.1 Metrics Explorer (`/metrics`)
- **Vitals Aggregate KPI Cards**:
  - **Total Requests**: Formatted request counter within the active temporal scope.
  - **Throughput (RPS)**: Real-time request per second rate.
  - **Avg Latency & P95 Latency**: High-precision mean and 95th percentile duration markers in milliseconds.
  - **Error Rate (%)**: Color-coded percentage highlighting anomalous failures (rose for > 0%, emerald for 0%).
- **Multi-Resolution Charts (Recharts)**:
  - **Latency Percentiles**: Multi-series `LineChart` plotting Mean, P50, P95, and P99 latency against temporal buckets.
  - **Request Throughput**: Gradient `AreaChart` visualizing load trends and traffic spikes.
  - **Error Distribution**: `BarChart` illustrating error volume per bucket interval.
- **Controls & Bucketing**:
  - Temporal windows: `15m`, `1h`, `6h`, `24h`, `7d`.
  - Resolution buckets: `1m`, `5m`, `15m`, `1h`.

### 2.2 Enterprise Dashboards Engine (`/dashboards`)
- **12-Widget Catalog**:
  1. `timeseries`: Metric trends over time with area fill.
  2. `stat`: Single key performance KPI with sparkline & delta.
  3. `bar`: Comparative bar distribution across microservices.
  4. `gauge`: Utilization meter with threshold color bands.
  5. `table`: Tabular telemetry logs or slow endpoints.
  6. `status-grid`: Service and node health state matrix.
  7. `log-stream`: Real-time filtered event stream tail.
  8. `waterfall-summary`: Trace latency distribution percentiles.
  9. `incidents`: Open incident queue with severity badges.
  10. `slo-burn`: Objective error budget consumption gauge.
  11. `markdown`: Operational guide, notes, and runbooks.
  12. `pie`: Categorical status and severity donut chart.
- **Operational Features**:
  - Export dashboard configuration to reproducible JSON bundle.
  - Import custom JSON dashboard schemas with schema validation.
  - Reorder widgets dynamically with responsive grid layout bindings.
  - Configurable auto-refresh interval (`10s`, `30s`, `1m`, `5m`, or `Off`).

---

## 3. Resilience & State Handling

- **Loading State**: Integrated `<Skeleton>` placeholders during data queries.
- **Empty State**: Integrated `<EmptyState>` when no data points exist in the selected time window.
- **Error State**: Integrated `<ErrorState>` with retry callbacks upon API failures.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/dashboards.test.tsx`:
  - Verified DashboardsPage header, controls, and widget catalog rendering.
- `src/SentinelOps.Web/src/pages/__tests__/observability.test.tsx`:
  - Verified MetricsPage rendering aggregate metrics (`750` requests, `12.50` RPS, `2.50%` error rate).
- Vitest suite results: **18 test files passed (100%), 70 tests passed**.
- TypeScript type check: **0 errors**.
