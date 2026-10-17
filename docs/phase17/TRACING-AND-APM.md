# SentinelOps Distributed Tracing & APM Experience Specification

## 1. Executive Summary

Checkpoint 17.6 standardizes the Distributed Tracing (`/traces`) and Application Performance Monitoring (`/apm`) workflows, providing deep visibility into microservice interactions, request latency waterfalls, and real-time dependency DAG topologies.

---

## 2. Distributed Tracing Architecture (`/traces`)

### 2.1 Trace Waterfall & Span Hierarchy
- **Root Span Summary**: Formatted total duration (ms), total span count, and participating service count.
- **Ruler Timeline**: 5-point millisecond timeline markers (`0ms`, `25%`, `50%`, `75%`, `100%`) computed dynamically from total trace duration.
- **Span Execution Bars**:
  - Start offset rendered via `left: ${(span.startOffsetMs / trace.totalDurationMs) * 100}%`.
  - Span duration bar width rendered via `width: ${(span.durationMs / trace.totalDurationMs) * 100}%`.
  - Color-coded: Emerald/Blue for successful operations, Rose for failing spans.
- **Span Inspector Drawer**:
  - Operation name, service name, status badge, start timestamp, execution duration.
  - Formatted JSON span attributes.
  - Direct 1-click correlation link to **Log Explorer** (`/logs?traceId=${traceId}`).

### 2.2 Trace Filtering & Querying
- Filter by Application ID, Service name, Error state (`errorsOnly`), Substring search (root span name, trace ID), and Minimum latency threshold (`minDurationMs`).
- Integrated `<Skeleton>` loaders, `<EmptyState>`, and `<ErrorState>` primitives.

---

## 3. Application Performance Monitoring (`/apm`)

### 3.1 Service Dependency Map
- **Live Node Cards**: Displays each monitored microservice with semantic health status (Healthy / Degraded / Critical), P95 latency, error rate percentage, and total call throughput.
- **Dependency Inspector Drawer**:
  - **Inbound Callers**: Upstream clients calling this service with call volume, average latency, and error rate.
  - **Outbound Dependencies**: Downstream services or databases invoked with latency breakdown.

### 3.2 Endpoints Explorer & External Dependencies
- **Endpoints Tab**: Breakdown of HTTP routes with P50, P95, and P99 latency percentiles, throughput requests/sec, and error rates.
- **External Dependencies Tab**: SQL database queries, Redis caches, and outbound third-party APIs with average latency and failure rates.

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/pages/__tests__/apm.test.tsx`:
  - Verified APM page title and tab switching (`Service Dependency Map`, `Endpoints Explorer`, `External & DB Dependencies`).
- `src/SentinelOps.Web/src/pages/__tests__/observability.test.tsx`:
  - Verified TracesPage rendering with root span `POST /api/v1/charge`, duration pill `420.5 ms`, and span count `3 spans`.
- Vitest suite results: **18 test files passed (100%), 70 tests passed**.
- TypeScript type check: **0 errors**.
