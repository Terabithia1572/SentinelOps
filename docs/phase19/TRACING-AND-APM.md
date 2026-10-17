# SentinelOps — Phase 19 Distributed Tracing and APM Architecture

**Author:** Principal Software & Observability Platform Architect  
**Date:** 2026-10-17  
**Status:** IMPLEMENTED AND VERIFIED  

---

## 1. Architectural Overview

Distributed tracing and Application Performance Monitoring (APM) in SentinelOps provides continuous visibility into end-to-end request flows across distributed microservice architectures. Built on OpenTelemetry W3C TraceContext standards, the tracing engine supports high-cardinality span ingestion, topological service maps, and granular latency breakdowns.

### Key Enhancements in Phase 19
1. **Zero Mock Fallbacks**: Prior versions fell back to synthetic demo topologies (`api-gateway`, `orders-service`, `payments-service`) when database tables were empty. In Phase 19, all APM endpoints (`/api/apm/service-map`, `/api/apm/endpoints`, `/api/apm/external-dependencies`) strictly return verified, real-world computed data or valid empty states.
2. **Dynamic External Dependency Extraction**: Database queries (PostgreSQL, Redis) and outbound HTTP/RPC calls are now dynamically parsed from OpenTelemetry semantic attributes (`db.system`, `db.name`, `db.operation`, `http.url`, `peer.service`) stored in the JSONB `Attributes` column.
3. **Resilient Graph Reconstruction**:
   - **Missing Parents & Partial Traces**: Spans whose `ParentSpanId` is not present in the ingested trace are gracefully flagged (`IsOrphanedParent: true`) and rendered at root depth (depth 0) with a visual indicator, avoiding broken hierarchies.
   - **Clock Skew Mitigation**: Span offsets relative to the trace root start are clamped at `>= 0.0ms` (`Math.Max(0, (span.StartedAtUtc - traceStart).TotalMilliseconds)`), and duration percentages are clamped to prevent exceeding 100% of the viewport.
   - **Slowest Spans Identification**: Spans are ranked by execution duration to immediately identify bottleneck operations (`IsSlowestSpan: true`).
4. **Log-to-Trace Correlation**: Every trace detail payload aggregates all log records possessing matching `TraceId` attributes, providing immediate contextual telemetry when analyzing errors.

---

## 2. Trace Detail & Span Hierarchy Model

```csharp
public record TraceDetailDto
{
    public TraceDto Trace { get; init; } = default!;
    public IReadOnlyList<SpanDetailDto> Spans { get; init; } = [];
    public IReadOnlyList<LogEntryDto> CorrelatedLogs { get; init; } = [];
    public IReadOnlyList<string> Services { get; init; } = [];
    public int MaxDepth { get; init; }
    public double TotalDurationMs { get; init; }
    public IReadOnlyList<string> SlowestSpanIds { get; init; } = [];
    public bool HasMissingParents { get; init; }
}

public record SpanDetailDto
{
    public Guid Id { get; init; }
    public string SpanId { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public string? ParentSpanId { get; init; }
    public string OperationName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public string Status { get; init; } = "Ok";
    public DateTime StartedAtUtc { get; init; }
    public string Attributes { get; init; } = "{}";
    public int Depth { get; init; }
    public double OffsetMs { get; init; }
    public double OffsetPercent { get; init; }
    public double DurationPercent { get; init; }
    public bool IsOrphanedParent { get; init; }
    public bool IsSlowestSpan { get; init; }
    public bool HasError => !string.Equals(Status, "Ok", StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(Status, "Unset", StringComparison.OrdinalIgnoreCase);
}
```

---

## 3. APM Performance Analytics

- **Percentile Calculation**: Accurate P50, P95, and P99 latency percentiles are calculated using sample interpolation from sorted duration arrays:
  $$\text{Index} = \lceil P \times N \rceil - 1$$
- **Tenant Isolation**: All span queries join through `TraceRecords.ApplicationId` to verify ownership against the active tenant's `OrganizationId`. Spans belonging to other organizations are strictly omitted.
- **Service Dependency Graph**: Directed edges $S_{\text{parent}} \rightarrow S_{\text{child}}$ are inferred from parent-child span pairs across differing service names, tracking call count, error count, and P95 latency across the call path.
