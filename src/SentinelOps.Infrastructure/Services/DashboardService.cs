using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<DashboardSummaryItemDto>> GetDashboardsAsync(CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var currentUserId = _currentUserService.UserId;

        var query = _context.Dashboards
            .AsNoTracking()
            .Where(d => d.OrganizationId == orgId);

        if (!string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(d => d.IsShared || d.CreatedByUserId == currentUserId);
        }

        var dashboards = await query
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);

        return dashboards.Select(MapToSummaryDto).ToList();
    }

    public async Task<DashboardDetailDto?> GetDashboardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var currentUserId = _currentUserService.UserId;

        var dashboard = await _context.Dashboards
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, cancellationToken);

        if (dashboard == null) return null;

        if (!dashboard.IsShared && !string.IsNullOrEmpty(currentUserId) && dashboard.CreatedByUserId != currentUserId)
        {
            return null;
        }

        return MapToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto> CreateDashboardAsync(CreateDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var currentUserId = _currentUserService.UserId;
        var currentUserName = _currentUserService.UserName ?? "User";

        if (request.IsDefault)
        {
            await ClearExistingDefaultAsync(orgId, cancellationToken);
        }

        var settings = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.OrganizationId == orgId, cancellationToken);
        var currentDashboards = await _context.Dashboards.CountAsync(d => d.OrganizationId == orgId, cancellationToken);
        if (settings != null && settings.MaxCustomDashboards > 0 && currentDashboards >= settings.MaxCustomDashboards)
        {
            throw new InvalidOperationException($"Custom dashboard limit reached ({currentDashboards}/{settings.MaxCustomDashboards}) for the {settings.PlanTier} plan. Upgrade plan to create more dashboards.");
        }

        var tagsJson = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : "[]";
        var layoutJson = ValidateAndSanitizeLayoutJson(request.LayoutJson);

        var dashboard = new CustomDashboard(
            organizationId: orgId,
            title: request.Title,
            description: request.Description,
            isDefault: request.IsDefault,
            isShared: request.IsShared,
            createdByUserId: currentUserId,
            createdByUserName: currentUserName,
            refreshIntervalSeconds: request.RefreshIntervalSeconds <= 0 ? 30 : request.RefreshIntervalSeconds,
            defaultTimeRange: string.IsNullOrWhiteSpace(request.DefaultTimeRange) ? "1h" : request.DefaultTimeRange,
            tagsJson: tagsJson,
            layoutJson: layoutJson);

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto?> UpdateDashboardAsync(Guid id, UpdateDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var dashboard = await _context.Dashboards
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, cancellationToken);

        if (dashboard == null) return null;

        if (request.IsDefault && !dashboard.IsDefault)
        {
            await ClearExistingDefaultAsync(orgId, cancellationToken);
        }

        dashboard.Title = request.Title;
        dashboard.Description = request.Description;
        dashboard.IsDefault = request.IsDefault;
        dashboard.IsShared = request.IsShared;
        dashboard.RefreshIntervalSeconds = request.RefreshIntervalSeconds <= 0 ? 30 : request.RefreshIntervalSeconds;
        dashboard.DefaultTimeRange = string.IsNullOrWhiteSpace(request.DefaultTimeRange) ? "1h" : request.DefaultTimeRange;

        if (request.Tags != null)
        {
            dashboard.TagsJson = JsonSerializer.Serialize(request.Tags);
        }

        if (!string.IsNullOrWhiteSpace(request.LayoutJson))
        {
            dashboard.LayoutJson = ValidateAndSanitizeLayoutJson(request.LayoutJson);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDetailDto(dashboard);
    }

    public async Task<bool> DeleteDashboardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var dashboard = await _context.Dashboards
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, cancellationToken);

        if (dashboard == null) return false;

        _context.Dashboards.Remove(dashboard);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DashboardDetailDto?> DuplicateDashboardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var original = await _context.Dashboards
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, cancellationToken);

        if (original == null) return null;

        var currentUserId = _currentUserService.UserId;
        var currentUserName = _currentUserService.UserName ?? "User";

        var cloned = new CustomDashboard(
            organizationId: orgId,
            title: $"{original.Title} (Copy)",
            description: original.Description,
            isDefault: false,
            isShared: original.IsShared,
            createdByUserId: currentUserId,
            createdByUserName: currentUserName,
            refreshIntervalSeconds: original.RefreshIntervalSeconds,
            defaultTimeRange: original.DefaultTimeRange,
            tagsJson: original.TagsJson,
            layoutJson: original.LayoutJson);

        _context.Dashboards.Add(cloned);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(cloned);
    }

    public async Task<DashboardDetailDto> ImportDashboardAsync(ImportDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var currentUserId = _currentUserService.UserId;
        var currentUserName = _currentUserService.UserName ?? "User";

        var tagsJson = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : "[]";
        var layoutJson = string.IsNullOrWhiteSpace(request.LayoutJson) ? "[]" : request.LayoutJson;

        var dashboard = new CustomDashboard(
            organizationId: orgId,
            title: request.Title,
            description: request.Description,
            isDefault: false,
            isShared: true,
            createdByUserId: currentUserId,
            createdByUserName: currentUserName,
            refreshIntervalSeconds: 30,
            defaultTimeRange: "1h",
            tagsJson: tagsJson,
            layoutJson: layoutJson);

        _context.Dashboards.Add(dashboard);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(dashboard);
    }

    public async Task<DashboardDetailDto?> InstantiateTemplateAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        var (title, desc, tags, widgets) = GetTemplateDefinition(templateKey);
        if (string.IsNullOrEmpty(title)) return null;

        var request = new CreateDashboardRequest
        {
            Title = title,
            Description = desc,
            Tags = tags,
            IsDefault = false,
            IsShared = true,
            RefreshIntervalSeconds = 30,
            DefaultTimeRange = "1h",
            LayoutJson = JsonSerializer.Serialize(widgets)
        };

        return await CreateDashboardAsync(request, cancellationToken);
    }

    private async Task ClearExistingDefaultAsync(Guid orgId, CancellationToken cancellationToken)
    {
        var existingDefaults = await _context.Dashboards
            .Where(d => d.OrganizationId == orgId && d.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var d in existingDefaults)
        {
            d.IsDefault = false;
        }
    }

    public async Task<DashboardDetailDto?> SetDefaultDashboardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orgId = await GetRequiredOrganizationIdAsync(cancellationToken);
        var dashboard = await _context.Dashboards
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == orgId, cancellationToken);

        if (dashboard == null) return null;

        await ClearExistingDefaultAsync(orgId, cancellationToken);
        dashboard.IsDefault = true;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(dashboard);
    }

    private static readonly HashSet<string> AllowedWidgetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeseries", "kpi", "error_rate", "latency", "throughput",
        "log_severity", "service_health", "alerts", "incidents", "slo"
    };

    private static string ValidateAndSanitizeLayoutJson(string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson) || layoutJson.Trim() == "[]")
        {
            return "[]";
        }

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return "[]";
            }
            return layoutJson;
        }
        catch
        {
            return "[]";
        }
    }

    private async Task<Guid> GetRequiredOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(SentinelOps.Domain.Constants.Roles.Admin);

        if (_currentTenantService.OrganizationId.HasValue && _currentTenantService.OrganizationId.Value != Guid.Empty)
        {
            var requestedOrg = _currentTenantService.OrganizationId.Value;
            if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
            {
                var isMember = await _context.OrganizationMembers
                    .AnyAsync(m => m.OrganizationId == requestedOrg && m.UserId == currentUserId, cancellationToken);
                if (isMember)
                {
                    return requestedOrg;
                }
            }
            else
            {
                return requestedOrg;
            }
        }

        if (!string.IsNullOrEmpty(currentUserId))
        {
            var userOrg = await _context.OrganizationMembers
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (userOrg != Guid.Empty)
            {
                return userOrg;
            }
        }

        var defaultOrg = await _context.Organizations
            .Where(o => o.Slug == "default-org")
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultOrg != Guid.Empty)
        {
            return defaultOrg;
        }

        var firstOrg = await _context.Organizations
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return firstOrg != Guid.Empty ? firstOrg : Guid.NewGuid();
    }

    private static (string Title, string Description, List<string> Tags, List<DashboardWidgetDto> Widgets) GetTemplateDefinition(string key)
    {
        return key.ToLowerInvariant() switch
        {
            "k8s-infra" or "kubernetes" => (
                "Kubernetes & Infrastructure Overview",
                "Cluster resource health, node status matrix, pod restart rates, and infrastructure telemetry.",
                ["infrastructure", "kubernetes", "nodes", "metrics"],
                [
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "timeseries", Title = "Cluster CPU & Memory Utilization", X = 0, Y = 0, W = 8, H = 4, ConfigJson = "{\"metric\":\"system.cpu.utilization\",\"aggregation\":\"avg\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "stat", Title = "Active Compute Nodes", X = 8, Y = 0, W = 4, H = 2, ConfigJson = "{\"metric\":\"k8s.node.count\",\"aggregation\":\"max\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "gauge", Title = "Cluster Memory Allocation", X = 8, Y = 2, W = 4, H = 2, ConfigJson = "{\"metric\":\"system.memory.usage\",\"target\":90}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "status-grid", Title = "Infrastructure Node Health", X = 0, Y = 4, W = 6, H = 3, ConfigJson = "{}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "incidents", Title = "Active Infra Incidents", X = 6, Y = 4, W = 6, H = 3, ConfigJson = "{}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "log-stream", Title = "Cluster Event Stream", X = 0, Y = 7, W = 12, H = 4, ConfigJson = "{\"severity\":\"Warning\"}" }
                ]
            ),
            "apm-services" or "apm" => (
                "API Gateway & Microservices APM",
                "Service request rates, P95/P99 latency trends, HTTP error rate breakdown, and distributed traces.",
                ["apm", "microservices", "latency", "slo"],
                [
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "stat", Title = "System Error Rate", X = 0, Y = 0, W = 3, H = 2, ConfigJson = "{\"metric\":\"http.server.error_rate\",\"format\":\"percent\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "stat", Title = "P95 Response Time", X = 3, Y = 0, W = 3, H = 2, ConfigJson = "{\"metric\":\"http.server.duration.p95\",\"unit\":\"ms\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "bar", Title = "Requests Per Service", X = 6, Y = 0, W = 6, H = 3, ConfigJson = "{\"metric\":\"http.server.requests\",\"groupBy\":\"service\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "timeseries", Title = "P95 vs P99 Duration Trends", X = 0, Y = 2, W = 6, H = 4, ConfigJson = "{\"metric\":\"http.server.duration\",\"aggregations\":[\"p95\",\"p99\"]}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "pie", Title = "HTTP Status Code Distribution", X = 6, Y = 3, W = 6, H = 3, ConfigJson = "{}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "table", Title = "Top Slow Endpoints", X = 0, Y = 6, W = 7, H = 4, ConfigJson = "{\"type\":\"slow_endpoints\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "waterfall-summary", Title = "Trace Latency Distribution", X = 7, Y = 6, W = 5, H = 4, ConfigJson = "{}" }
                ]
            ),
            "slo-incidents" or "slo" => (
                "Executive SLO & Incident Summary",
                "High-level service reliability objectives, burn rates, open incident severity overview, and runbooks.",
                ["executive", "slo", "incidents", "governance"],
                [
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "slo-burn", Title = "Checkout API 99.9% SLO Burn", X = 0, Y = 0, W = 6, H = 3, ConfigJson = "{\"target\":99.9}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "slo-burn", Title = "Gateway Ingestion 99.95% SLO", X = 6, Y = 0, W = 6, H = 3, ConfigJson = "{\"target\":99.95}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "incidents", Title = "Open Operational Incidents", X = 0, Y = 3, W = 7, H = 4, ConfigJson = "{\"status\":\"Open\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "markdown", Title = "Executive Incident Runbook", X = 7, Y = 3, W = 5, H = 4, ConfigJson = "{\"markdown\":\"### Emergency Escalation Runbook\\n- **P1 Incident**: Page Lead On-Call via PagerDuty\\n- **Bridge**: Ops Room 12-A\\n- Status updates must be posted every 15 minutes.\"}" }
                ]
            ),
            "db-redis" or "database" => (
                "Database & Redis Cache Health",
                "PostgreSQL database query latency, connection pool pressure, and Redis cache hit ratios.",
                ["database", "redis", "postgresql", "cache"],
                [
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "gauge", Title = "Redis Cache Hit Ratio", X = 0, Y = 0, W = 4, H = 3, ConfigJson = "{\"metric\":\"redis.hit_ratio\",\"target\":95}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "timeseries", Title = "PostgreSQL Query Latency", X = 4, Y = 0, W = 8, H = 3, ConfigJson = "{\"metric\":\"db.query.duration\",\"aggregation\":\"avg\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "table", Title = "Database Slow Queries", X = 0, Y = 3, W = 8, H = 4, ConfigJson = "{\"type\":\"slow_queries\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "stat", Title = "Active DB Connections", X = 8, Y = 3, W = 4, H = 2, ConfigJson = "{\"metric\":\"db.connections.active\"}" },
                    new DashboardWidgetDto { Id = Guid.NewGuid().ToString(), Type = "stat", Title = "Redis Memory Used (MB)", X = 8, Y = 5, W = 4, H = 2, ConfigJson = "{\"metric\":\"redis.memory.used\"}" }
                ]
            ),
            _ => (string.Empty, string.Empty, [], [])
        };
    }

    private static DashboardSummaryItemDto MapToSummaryDto(CustomDashboard d)
    {
        var tags = ParseTags(d.TagsJson);
        var widgetCount = CountWidgets(d.LayoutJson);

        return new DashboardSummaryItemDto
        {
            Id = d.Id,
            OrganizationId = d.OrganizationId,
            Title = d.Title,
            Description = d.Description,
            IsDefault = d.IsDefault,
            IsShared = d.IsShared,
            CreatedByUserName = d.CreatedByUserName,
            RefreshIntervalSeconds = d.RefreshIntervalSeconds,
            DefaultTimeRange = d.DefaultTimeRange,
            Tags = tags,
            WidgetCount = widgetCount,
            CreatedAtUtc = d.CreatedAtUtc,
            UpdatedAtUtc = d.UpdatedAtUtc
        };
    }

    private static DashboardDetailDto MapToDetailDto(CustomDashboard d)
    {
        var summary = MapToSummaryDto(d);
        var widgets = ParseWidgets(d.LayoutJson);

        return new DashboardDetailDto
        {
            Id = summary.Id,
            OrganizationId = summary.OrganizationId,
            Title = summary.Title,
            Description = summary.Description,
            IsDefault = summary.IsDefault,
            IsShared = summary.IsShared,
            CreatedByUserName = summary.CreatedByUserName,
            RefreshIntervalSeconds = summary.RefreshIntervalSeconds,
            DefaultTimeRange = summary.DefaultTimeRange,
            Tags = summary.Tags,
            WidgetCount = summary.WidgetCount,
            CreatedAtUtc = summary.CreatedAtUtc,
            UpdatedAtUtc = summary.UpdatedAtUtc,
            LayoutJson = d.LayoutJson,
            Widgets = widgets
        };
    }

    private static List<string> ParseTags(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static int CountWidgets(string layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.GetArrayLength();
            }
        }
        catch
        {
            // Ignore
        }
        return 0;
    }

    private static List<DashboardWidgetDto> ParseWidgets(string layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson)) return [];
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<DashboardWidgetDto>>(layoutJson, options) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
