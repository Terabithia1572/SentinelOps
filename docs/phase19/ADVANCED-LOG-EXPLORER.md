# SentinelOps — Phase 19 Advanced Log Explorer

**Author:** Principal Software & Observability Platform Architect  
**Date:** 2026-10-17  
**Status:** IMPLEMENTED AND VERIFIED  

---

## 1. Overview & Architectural Principles

The Advanced Log Explorer extends SentinelOps from basic timestamp-ordered log retrieval into a high-throughput, structured log investigation workbench tailored for Site Reliability Engineers (SREs).

### Non-Negotiable Safety & Security Constraints
1. **Parameterized Queries Only**: Arbitrary user strings are never concatenated into SQL or dynamic raw queries. All searches execute via strongly typed EF Core LINQ parameterized expressions.
2. **Authoritative Tenant Scoping**: Every query strictly resolves the tenant's `OrganizationId` from server-side claims or session context. Cross-tenant log leakage is blocked at the database query predicate level.
3. **Bounded Scans & Export Limits**: Result windows default to 50 records per page (max 200). Exports are hard-bounded at 5,000 records to prevent memory exhaustion and database lock starvation.
4. **Execution Telemetry**: Server execution duration is returned in the `X-Query-Duration-Ms` response header, giving users immediate feedback on query complexity and indexing efficiency.

---

## 2. Safe Query Model & Filter Capabilities

| Filter Dimension | Type | Evaluation Semantics | Index Support |
| :--- | :--- | :--- | :--- |
| **Full-Text Search** | Substring / Pattern | Matches against `Message`, `MessageTemplate`, `ExceptionDetail`, and `Properties` | GIN / B-Tree indices |
| **Structured Properties** | `key=value` or string | Matches key and value substrings within the JSONB `Properties` column | JSONB containment |
| **Severity** | Enum | Filters by `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` | Composite index with `(ApplicationId, Severity, Timestamp)` |
| **Application & Service** | UUID / String | Bounded to active tenant applications and specific microservices | Composite index with `(ApplicationId, Timestamp)` |
| **Environment** | String | Isolates `Production`, `Staging`, `Development`, `Testing` | Filtered index |
| **Trace & Span Correlation** | String (hex) | Instant correlation with OpenTelemetry distributed traces and span IDs | `IX_LogEntries_TraceId` |
| **Time Windows** | UTC timestamps | Enforces `FromUtc` and `ToUtc` bounded ranges | `Timestamp DESC` B-Tree index |

---

## 3. Saved Log Queries Architecture

The `SavedLogQuery` domain entity stores named investigation views in PostgreSQL with tenant isolation:

```csharp
public class SavedLogQuery : Entity
{
    public Guid OrganizationId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SearchTerm { get; set; }
    public string? Severity { get; set; }
    public string? ServiceName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? TraceId { get; set; }
    public string? PropertyFiltersJson { get; set; }
    public string TimeRange { get; set; } = "1h";
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public bool IsShared { get; set; } = true;
    public string TagsJson { get; set; } = "[]";
}
```

### API Endpoints
- `GET /api/logs/saved`: List saved queries available to the current tenant organization (shared queries + personal queries).
- `GET /api/logs/saved/{id}`: Inspect saved query detail by ID.
- `POST /api/logs/saved`: Create a new saved query view.
- `PUT /api/logs/saved/{id}`: Update an existing saved query view.
- `DELETE /api/logs/saved/{id}`: Delete a saved query.
- `GET /api/logs/export`: Export filtered logs in `json` or `csv` format with bounded limits.

---

## 4. Precision Dark Frontend Experience

The frontend log explorer in `src/SentinelOps.Web/src/pages/logs.tsx` features:
1. **Query Toolbar & Saved Queries Bar**: One-click switching between saved investigation presets (e.g., "Critical Exceptions", "Slow Ingestion", "Auth Failures").
2. **Deep-Link Shareability**: Query parameters are synced to browser URL search params (`?q=...&severity=...&service=...&traceId=...`), allowing instant sharing among engineers.
3. **Structured Log Detail Drawer**: Clicking any log row slides open a structured JSON property tree, exception stack trace viewer, and a direct link to navigate to the correlated distributed trace.
4. **Execution Performance Readout**: Displays query execution latency (e.g., `⚡ 14ms (50 records returned)`) in real time.
