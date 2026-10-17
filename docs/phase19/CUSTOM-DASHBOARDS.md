# SentinelOps — Phase 19 User-Configurable Custom Dashboards

**Author:** Principal Software & Observability Platform Architect  
**Date:** 2026-10-17  
**Status:** IMPLEMENTED AND VERIFIED  

---

## 1. Overview & Architectural Goals

The Custom Dashboard Engine empowers enterprise tenants to create, customize, and share specialized monitoring cockpits tailored to specific operational domains (e.g., SRE executive overview, database latency monitoring, authentication incident triage).

### Core Principles
1. **Multi-Tenant Ownership & Isolation**: Every dashboard record (`CustomDashboard`) is strictly bounded by `OrganizationId`. Shared dashboards (`IsShared: true`) are visible to all members of the organization; private dashboards are restricted to their creator.
2. **Server-Side Layout Sanitization**: All incoming widget layouts are parsed and validated server-side. Unsafe widget structures or attempted cross-site script injections are sanitized into safe JSON schemas.
3. **Default Dashboard Setting**: Organizations can designate a single default dashboard via `PUT /api/dashboards/{id}/default`. Setting a new default atomically clears previous default flags within the organization.

---

## 2. Supported Widget Categories & Schema

Widgets are defined as structured JSON elements within the dashboard's `LayoutJson` column:

```typescript
export interface DashboardWidget {
  id: string;
  type: 
    | "timeseries"       // Continuous telemetry metrics (CPU, Memory, Ingestion rate)
    | "kpi"              // High-impact numeric readout with trend delta
    | "error_rate"       // HTTP/RPC error percentage over measurement window
    | "latency"          // P50, P95, P99 duration breakdown
    | "throughput"       // Requests / spans per minute
    | "log_severity"     // Distribution bar of log severities (Info, Warn, Error, Crit)
    | "service_health"   // Operational health status across active services
    | "alerts"           // Active firing alert rules
    | "incidents"        // Ongoing incident queue
    | "slo";             // Service level objective compliance and error budget gauge
  title: string;
  w: number;            // Grid width units (1 to 12)
  h: number;            // Grid height units (1 to 6)
  config: {
    metricName?: string;
    serviceName?: string;
    timeRange?: string;
    thresholdWarning?: number;
    thresholdCritical?: number;
    dataSourceType?: string;
  };
}
```

---

## 3. Server-Side Dashboard APIs

| Method | Endpoint | Description | Authorization |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/dashboards` | List summaries of all organization dashboards | Viewer / Operator / Admin |
| `GET` | `/api/dashboards/{id}` | Retrieve complete dashboard detail and widget layout | Viewer / Operator / Admin |
| `POST` | `/api/dashboards` | Create a new custom dashboard | Operator / Admin |
| `PUT` | `/api/dashboards/{id}` | Update dashboard metadata, intervals, and layout | Operator / Admin |
| `DELETE` | `/api/dashboards/{id}` | Delete a dashboard | Operator / Admin |
| `POST` | `/api/dashboards/{id}/duplicate` | Clone an existing dashboard as a new draft | Operator / Admin |
| `PUT` | `/api/dashboards/{id}/default` | Designate dashboard as organization default | Operator / Admin |
| `GET` | `/api/dashboards/{id}/export` | Export dashboard configuration JSON | Viewer / Operator / Admin |
| `POST` | `/api/dashboards/import` | Import dashboard configuration JSON | Operator / Admin |
| `POST` | `/api/dashboards/templates/{key}/instantiate` | Instantiate predefined starter template | Operator / Admin |

---

## 4. Precision Dark UI Experience

The custom dashboard frontend in `src/SentinelOps.Web/src/pages/dashboards.tsx` delivers:
- **Interactive Widget Drawer**: Select from ten validated widget categories.
- **Dynamic Configuration Modal**: Configure metric targets, alert rules, or service bindings per widget.
- **Live Auto-Refresh**: User-selectable polling cycles (10s, 30s, 60s, or manual freeze).
- **Default Dashboard Star**: One-click toggle to set or unset the organization's landing dashboard.
