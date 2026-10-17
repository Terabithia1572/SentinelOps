# SentinelOps Phase 19: Precision Dark UI Integration & Observability Frontend Enhancements

## 1. Overview & Aesthetic Direction

In Phase 19, the frontend was extended to support advanced enterprise observability workflows while strictly preserving the approved **Precision Dark** design system:
- **Surface Palette**: Deep, dark zinc/slate canvas (`#08090D`, `hsl(var(--background))`) with subtle borders (`hsl(var(--border))`) and micro-elevations.
- **Data Density**: Compact, high-information-density UI tables and metric cards with monospaced timestamps and numerical indicators.
- **Accents**: Functional status tokens:
  - Emerald (`#10b981`) for normal / healthy operations (< 80% quota, compliant SLOs).
  - Amber (`#f59e0b`) for warning indicators (>= 80% quota, burn rate > 1.0x).
  - Rose (`#f43f5e`) for critical / exceeded states (>= 95% or 100% quota, breached SLOs).
  - Violet/Purple (`#a855f7`) for distributed trace orphaned context.

---

## 2. Advanced Log Explorer (`src/pages/logs.tsx`)

1. **Saved Queries Selector & Persistence**:
   - Operators can select from an organization-scoped dropdown of pinned and personal queries.
   - A dedicated **Save Current Query** modal dialog (`Dialog`) validates title, optional description, and serializes the exact filter state (`searchTerm`, `severity`, `serviceName`, `environmentName`, `timeWindow`).
2. **Query Execution Latency Indicator**:
   - The stream header dynamically extracts server timing from the `X-Query-Duration-Ms` HTTP header or client-side high-resolution performance timers:
   - Rendered as a compact badge: `⏱ {queryDurationMs}ms`.
3. **Bounded Telemetry Export**:
   - **Export CSV** and **Export JSON** actions trigger streaming downloads directly in the browser via Blob URL generation, clamped server-side to 5,000 records to prevent memory pressure.

---

## 3. Distributed Tracing & APM Waterfall (`src/pages/traces.tsx` & `apm.tsx`)

1. **Slowest Spans Identification**:
   - Spans located on the critical execution path with the highest execution duration are labeled with an amber `Slowest` tag.
2. **Orphaned Parent & Context Fragmentation**:
   - Spans with parent IDs missing from the local trace span collection are labeled with a purple `Orphaned` badge.
   - The trace detail statistics bar surfaces a `Missing Parents Detected` alert when distributed trace propagation is fragmented.
3. **Zero-Telemetry Empty States**:
   - Eliminates all hardcoded synthetic mock fallbacks in favor of clean, contextual `EmptyState` primitives.

---

## 4. Custom Observability Dashboards (`src/pages/dashboards.tsx`)

1. **Organization Default Dashboard**:
   - Active default dashboards feature a gold filled `Star` icon and `★ (Default)` badge.
   - Operators can toggle any custom dashboard to become the team-wide landing dashboard via the `Set as Default` button, which dispatches `PUT /api/dashboards/{id}/default`.

---

## 5. SLO & Error Budget Engineering (`src/pages/slos.tsx`)

1. **Error Budget Consumption Meter**:
   - Color-coded progress bar transitioning from Emerald (> 20%) to Amber (< 20%) to Rose (<= 0% breached).
2. **Multi-Window Burn Rate Gauge**:
   - Displays real-time burn rate with visual alerts:
     - `> 1.0x`: Amber indicator (error budget depleting faster than planned).
     - `> 2.0x`: Rose pulsing alert (critical burn requiring immediate incident response).

---

## 6. Multi-Dimensional Tenant Quota Meters (`src/pages/organizations.tsx`)

1. **Warning & Exceeded Banners**:
   - Amber warning banner displayed when any quota metric reaches 80% capacity.
   - Rose alert banner displayed when any quota reaches 100% capacity.
2. **Six Dedicated Quota Cards**:
   - Logs Ingestion, Distributed Traces, Monitored Applications, Custom Dashboards, Member Seats, and Storage Footprint.
