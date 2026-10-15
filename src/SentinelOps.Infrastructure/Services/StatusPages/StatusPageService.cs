using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.StatusPages;

public partial class StatusPageService : IStatusPageService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public StatusPageService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
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
            .OrderBy(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultOrg != null) return defaultOrg.Id;

        throw new InvalidOperationException("No active organization found for tenant context.");
    }

    public async Task<List<StatusPageDto>> GetStatusPagesAsync(CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var pages = await _context.StatusPages
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        var pageIds = pages.Select(p => p.Id).ToList();
        var activeIncidentCounts = await _context.StatusPageIncidents
            .Where(i => pageIds.Contains(i.StatusPageId) && i.Status != "Resolved")
            .GroupBy(i => i.StatusPageId)
            .Select(g => new { StatusPageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.StatusPageId, v => v.Count, ct);

        return pages.Select(p => MapToDto(p, activeIncidentCounts.GetValueOrDefault(p.Id, 0))).ToList();
    }

    public async Task<StatusPageDto?> GetStatusPageByIdAsync(Guid id, CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var page = await _context.StatusPages
            .Include(p => p.Incidents)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id, ct);

        if (page == null) return null;

        var activeCount = page.Incidents.Count(i => i.Status != "Resolved");
        return MapToDto(page, activeCount);
    }

    public async Task<StatusPageDto> CreateStatusPageAsync(CreateStatusPageRequest request, CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var cleanSlug = GenerateSlug(request.Slug);
        var existingSlug = await _context.StatusPages.AnyAsync(p => p.Slug == cleanSlug, ct);
        if (existingSlug)
        {
            cleanSlug = $"{cleanSlug}-{Guid.NewGuid().ToString("N")[..4]}";
        }

        var components = request.Components ?? new List<StatusPageComponentDto>
        {
            new() { Name = "Web Application", Description = "Customer dashboard and web interface", ComputedStatus = "Operational" },
            new() { Name = "Core API", Description = "REST API and telemetry processing", ComputedStatus = "Operational" },
            new() { Name = "Database & Storage", Description = "Primary database cluster and caches", ComputedStatus = "Operational" }
        };

        var maintenance = request.MaintenanceWindows ?? new List<StatusPageMaintenanceWindowDto>();

        var statusPage = new StatusPage(
            organizationId,
            request.Name,
            cleanSlug,
            request.Description,
            request.LogoUrl,
            request.SupportUrl,
            request.IsPublic,
            request.AutoRollupEnabled,
            JsonSerializer.Serialize(components, JsonOptions),
            JsonSerializer.Serialize(maintenance, JsonOptions))
        {
            HeaderNotice = request.HeaderNotice
        };

        _context.StatusPages.Add(statusPage);
        await _context.SaveChangesAsync(ct);

        return MapToDto(statusPage, 0);
    }

    public async Task<StatusPageDto?> UpdateStatusPageAsync(Guid id, UpdateStatusPageRequest request, CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var page = await _context.StatusPages.FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id, ct);
        if (page == null) return null;

        page.Name = request.Name;
        page.Description = request.Description;
        page.LogoUrl = request.LogoUrl;
        page.SupportUrl = request.SupportUrl;
        page.HeaderNotice = request.HeaderNotice;
        page.IsPublic = request.IsPublic;
        page.AutoRollupEnabled = request.AutoRollupEnabled;

        if (request.Components != null)
        {
            page.ComponentsJson = JsonSerializer.Serialize(request.Components, JsonOptions);
        }

        if (request.MaintenanceWindows != null)
        {
            page.MaintenanceWindowsJson = JsonSerializer.Serialize(request.MaintenanceWindows, JsonOptions);
        }

        await _context.SaveChangesAsync(ct);

        var activeCount = await _context.StatusPageIncidents
            .CountAsync(i => i.StatusPageId == id && i.Status != "Resolved", ct);

        return MapToDto(page, activeCount);
    }

    public async Task<bool> DeleteStatusPageAsync(Guid id, CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var page = await _context.StatusPages.FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id, ct);
        if (page == null) return false;

        _context.StatusPages.Remove(page);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<StatusPageIncidentDto?> PostIncidentAsync(
        Guid statusPageId,
        CreateStatusPageIncidentRequest request,
        string postedBy,
        CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var page = await _context.StatusPages.FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == statusPageId, ct);
        if (page == null) return null;

        var updates = new List<StatusPageIncidentUpdateDto>
        {
            new()
            {
                Status = request.Status,
                Message = request.InitialMessage,
                CreatedAtUtc = DateTime.UtcNow,
                PostedBy = postedBy
            }
        };

        var incident = new StatusPageIncident(
            statusPageId,
            request.Title,
            request.Status,
            request.Severity,
            request.LinkedIncidentId,
            JsonSerializer.Serialize(request.AffectedComponentIds ?? new List<string>(), JsonOptions),
            JsonSerializer.Serialize(updates, JsonOptions))
        {
            ResolvedAtUtc = request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null
        };

        _context.StatusPageIncidents.Add(incident);
        await _context.SaveChangesAsync(ct);

        return MapToIncidentDto(incident);
    }

    public async Task<StatusPageIncidentDto?> AddIncidentUpdateAsync(
        Guid statusPageId,
        Guid incidentId,
        AddStatusPageIncidentUpdateRequest request,
        string postedBy,
        CancellationToken ct = default)
    {
        var organizationId = await GetRequiredOrganizationIdAsync(ct);
        var page = await _context.StatusPages.FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == statusPageId, ct);
        if (page == null) return null;

        var incident = await _context.StatusPageIncidents
            .FirstOrDefaultAsync(i => i.StatusPageId == statusPageId && i.Id == incidentId, ct);
        if (incident == null) return null;

        var updates = DeserializeJson<List<StatusPageIncidentUpdateDto>>(incident.UpdatesJson) ?? new List<StatusPageIncidentUpdateDto>();
        updates.Insert(0, new StatusPageIncidentUpdateDto
        {
            Status = request.Status,
            Message = request.Message,
            CreatedAtUtc = DateTime.UtcNow,
            PostedBy = postedBy
        });

        incident.Status = request.Status;
        incident.UpdatesJson = JsonSerializer.Serialize(updates, JsonOptions);
        if (request.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
        {
            incident.ResolvedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return MapToIncidentDto(incident);
    }

    public async Task<PublicStatusPageDto?> GetPublicStatusPageBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var page = await _context.StatusPages
            .Include(p => p.Incidents)
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug && p.IsPublic, ct);

        if (page == null) return null;

        var components = DeserializeJson<List<StatusPageComponentDto>>(page.ComponentsJson) ?? new List<StatusPageComponentDto>();
        var maintenance = DeserializeJson<List<StatusPageMaintenanceWindowDto>>(page.MaintenanceWindowsJson) ?? new List<StatusPageMaintenanceWindowDto>();

        var allIncidents = page.Incidents.Select(MapToIncidentDto).ToList();
        var activeIncidents = allIncidents.Where(i => !i.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase)).ToList();
        var pastIncidents = allIncidents.Where(i => i.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.ResolvedAtUtc ?? i.CreatedAtUtc)
            .Take(10)
            .ToList();

        // Automated component status calculation
        if (page.AutoRollupEnabled)
        {
            var appIds = components.Where(c => c.ApplicationId.HasValue).Select(c => c.ApplicationId!.Value).Distinct().ToList();

            var firingAlerts = appIds.Count > 0
                ? await _context.AlertRules
                    .Where(r => appIds.Contains(r.ApplicationId) && r.Status == AlertStatus.Firing && r.IsEnabled)
                    .Select(r => new { r.ApplicationId, r.Severity })
                    .ToListAsync(ct)
                : new();

            var recentHealth = appIds.Count > 0
                ? await _context.HealthCheckResults
                    .Where(h => appIds.Contains(h.ApplicationId) && h.CheckedAtUtc >= DateTime.UtcNow.AddMinutes(-10))
                    .GroupBy(h => new { h.ApplicationId, h.ServiceName })
                    .Select(g => new
                    {
                        g.Key.ApplicationId,
                        g.Key.ServiceName,
                        Status = g.OrderByDescending(x => x.CheckedAtUtc).Select(x => x.Status).FirstOrDefault()
                    })
                    .ToListAsync(ct)
                : new();

            foreach (var comp in components)
            {
                if (!string.IsNullOrWhiteSpace(comp.StatusOverride))
                {
                    comp.ComputedStatus = comp.StatusOverride;
                    continue;
                }

                var activeAffectingIncident = activeIncidents.FirstOrDefault(i => i.AffectedComponentIds.Contains(comp.Id));
                if (activeAffectingIncident != null)
                {
                    comp.ComputedStatus = activeAffectingIncident.Severity switch
                    {
                        "Critical" => "Major Outage",
                        "Major" => "Partial Outage",
                        _ => "Degraded Performance"
                    };
                    continue;
                }

                if (comp.ApplicationId.HasValue)
                {
                    var appAlerts = firingAlerts.Where(a => a.ApplicationId == comp.ApplicationId.Value).ToList();
                    if (appAlerts.Any(a => a.Severity == Severity.Critical))
                    {
                        comp.ComputedStatus = "Major Outage";
                        continue;
                    }
                    if (appAlerts.Any(a => a.Severity == Severity.Error))
                    {
                        comp.ComputedStatus = "Partial Outage";
                        continue;
                    }
                    if (appAlerts.Any(a => a.Severity == Severity.Warning))
                    {
                        comp.ComputedStatus = "Degraded Performance";
                        continue;
                    }

                    var health = recentHealth.FirstOrDefault(h => h.ApplicationId == comp.ApplicationId.Value &&
                        (string.IsNullOrWhiteSpace(comp.ServiceName) || string.Equals(h.ServiceName, comp.ServiceName, StringComparison.OrdinalIgnoreCase)));

                    if (health != null && health.Status == HealthStatus.Unhealthy)
                    {
                        comp.ComputedStatus = "Major Outage";
                        continue;
                    }
                    if (health != null && health.Status == HealthStatus.Degraded)
                    {
                        comp.ComputedStatus = "Degraded Performance";
                        continue;
                    }
                }

                comp.ComputedStatus = "Operational";
            }
        }
        else
        {
            foreach (var comp in components)
            {
                comp.ComputedStatus = !string.IsNullOrWhiteSpace(comp.StatusOverride) ? comp.StatusOverride : "Operational";
            }
        }

        // Overall status rollup
        string overallStatus;
        if (components.Any(c => c.ComputedStatus == "Major Outage"))
        {
            overallStatus = "Major System Outage";
        }
        else if (components.Any(c => c.ComputedStatus == "Partial Outage"))
        {
            overallStatus = "Partial System Outage";
        }
        else if (components.Any(c => c.ComputedStatus == "Degraded Performance"))
        {
            overallStatus = "Degraded System Performance";
        }
        else if (maintenance.Any(m => m.StartTimeUtc <= DateTime.UtcNow && m.EndTimeUtc >= DateTime.UtcNow))
        {
            overallStatus = "Scheduled Maintenance Active";
        }
        else
        {
            overallStatus = "All Systems Operational";
        }

        return new PublicStatusPageDto
        {
            Name = page.Name,
            Slug = page.Slug,
            Description = page.Description,
            LogoUrl = page.LogoUrl,
            SupportUrl = page.SupportUrl,
            HeaderNotice = page.HeaderNotice,
            OverallStatus = overallStatus,
            Components = components,
            ActiveIncidents = activeIncidents,
            PastIncidents = pastIncidents,
            ScheduledMaintenance = maintenance.Where(m => m.EndTimeUtc >= DateTime.UtcNow).ToList(),
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    private static StatusPageDto MapToDto(StatusPage page, int activeIncidentsCount)
    {
        return new StatusPageDto
        {
            Id = page.Id,
            OrganizationId = page.OrganizationId,
            Name = page.Name,
            Slug = page.Slug,
            Description = page.Description,
            LogoUrl = page.LogoUrl,
            SupportUrl = page.SupportUrl,
            HeaderNotice = page.HeaderNotice,
            IsPublic = page.IsPublic,
            AutoRollupEnabled = page.AutoRollupEnabled,
            Components = DeserializeJson<List<StatusPageComponentDto>>(page.ComponentsJson) ?? new(),
            MaintenanceWindows = DeserializeJson<List<StatusPageMaintenanceWindowDto>>(page.MaintenanceWindowsJson) ?? new(),
            ActiveIncidentsCount = activeIncidentsCount,
            CreatedAtUtc = page.CreatedAtUtc,
            UpdatedAtUtc = page.UpdatedAtUtc
        };
    }

    private static StatusPageIncidentDto MapToIncidentDto(StatusPageIncident incident)
    {
        return new StatusPageIncidentDto
        {
            Id = incident.Id,
            StatusPageId = incident.StatusPageId,
            LinkedIncidentId = incident.LinkedIncidentId,
            Title = incident.Title,
            Status = incident.Status,
            Severity = incident.Severity,
            AffectedComponentIds = DeserializeJson<List<string>>(incident.AffectedComponentIdsJson) ?? new(),
            Updates = DeserializeJson<List<StatusPageIncidentUpdateDto>>(incident.UpdatesJson) ?? new(),
            CreatedAtUtc = incident.CreatedAtUtc,
            ResolvedAtUtc = incident.ResolvedAtUtc
        };
    }

    private static T? DeserializeJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return $"status-{Guid.NewGuid().ToString("N")[..6]}";
        var slug = input.Trim().ToLowerInvariant();
        slug = SlugRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"status-{Guid.NewGuid().ToString("N")[..6]}" : slug;
    }

    [GeneratedRegex(@"[^a-z0-9\-]+")]
    private static partial Regex SlugRegex();
}
