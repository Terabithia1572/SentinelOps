using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Services.Queries.GetServiceHealth;

public record GetServiceHealthQuery(Guid? ApplicationId = null) : IRequest<IReadOnlyList<ServiceHealthSummaryDto>>;

public class GetServiceHealthQueryHandler : IRequestHandler<GetServiceHealthQuery, IReadOnlyList<ServiceHealthSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetServiceHealthQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<IReadOnlyList<ServiceHealthSummaryDto>> Handle(GetServiceHealthQuery request, CancellationToken cancellationToken)
    {
        var sinceUtc = DateTime.UtcNow.AddHours(-24);

        List<Guid>? tenantAppIds = null;
        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (request.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == request.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return [];
                }
                tenantAppIds = [request.ApplicationId.Value];
            }
            else
            {
                tenantAppIds = await _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id)
                    .ToListAsync(cancellationToken);
            }
        }
        else if (request.ApplicationId.HasValue)
        {
            tenantAppIds = [request.ApplicationId.Value];
        }

        var appDict = await _context.Applications
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName ?? a.Name, cancellationToken);

        var tracesQuery = _context.TraceRecords
            .AsNoTracking()
            .Where(t => t.StartedAtUtc >= sinceUtc);

        if (tenantAppIds != null)
        {
            tracesQuery = tracesQuery.Where(t => tenantAppIds.Contains(t.ApplicationId));
        }

        var traceData = await tracesQuery
            .Select(t => new
            {
                t.ApplicationId,
                t.ServiceName,
                t.EnvironmentName,
                DurationMs = t.Duration.TotalMilliseconds,
                t.HasErrors,
                t.StartedAtUtc
            })
            .ToListAsync(cancellationToken);

        var registeredServicesQuery = _context.Services.AsNoTracking();
        if (tenantAppIds != null)
        {
            registeredServicesQuery = registeredServicesQuery.Where(s => tenantAppIds.Contains(s.ApplicationId));
        }
        var registeredServices = await registeredServicesQuery.ToListAsync(cancellationToken);

        // Group telemetry traces by service and environment
        var telemetryGroups = traceData
            .GroupBy(t => (t.ApplicationId, t.ServiceName, t.EnvironmentName))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ServiceHealthSummaryDto>();
        var processedKeys = new HashSet<(Guid, string, string)>();

        foreach (var (key, traces) in telemetryGroups)
        {
            processedKeys.Add(key);

            var total = traces.Count;
            var errors = traces.Count(t => t.HasErrors);
            var errorRate = total > 0 ? Math.Round((errors / (double)total) * 100.0, 2) : 0.0;
            var durations = traces.Select(t => t.DurationMs).OrderBy(d => d).ToList();
            var avg = durations.Count > 0 ? Math.Round(durations.Average(), 2) : 0.0;
            var p95 = durations.Count > 0 ? Math.Round(GetPercentile(durations, 0.95), 2) : 0.0;
            var lastSeen = traces.Max(t => t.StartedAtUtc);

            string status;
            if (errorRate >= 10.0 || p95 >= 3000.0)
            {
                status = "Unhealthy";
            }
            else if (errorRate >= 2.0 || p95 >= 1200.0)
            {
                status = "Degraded";
            }
            else
            {
                status = "Healthy";
            }

            var appName = appDict.GetValueOrDefault(key.ApplicationId, "Application");

            result.Add(new ServiceHealthSummaryDto
            {
                ApplicationId = key.ApplicationId,
                ApplicationName = appName,
                ServiceName = key.ServiceName,
                EnvironmentName = key.EnvironmentName,
                Status = status,
                TotalRequests = total,
                TotalErrors = errors,
                ErrorRatePercent = errorRate,
                AvgDurationMs = avg,
                P95DurationMs = p95,
                LastSeenAtUtc = lastSeen
            });
        }

        // Include any registered services without recent telemetry
        foreach (var s in registeredServices)
        {
            var key = (s.ApplicationId, s.Name, "Production");
            if (!processedKeys.Contains(key))
            {
                result.Add(new ServiceHealthSummaryDto
                {
                    ApplicationId = s.ApplicationId,
                    ApplicationName = appDict.GetValueOrDefault(s.ApplicationId, "Application"),
                    ServiceName = s.Name,
                    EnvironmentName = "Production",
                    Status = "Healthy",
                    TotalRequests = 0,
                    TotalErrors = 0,
                    ErrorRatePercent = 0,
                    AvgDurationMs = 0,
                    P95DurationMs = 0,
                    LastSeenAtUtc = null
                });
            }
        }

        return result
            .OrderBy(r => r.Status switch { "Unhealthy" => 0, "Degraded" => 1, _ => 2 })
            .ThenByDescending(r => r.TotalRequests)
            .ToList();
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];
        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }
}
