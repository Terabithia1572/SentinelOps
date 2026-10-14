using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var appsQuery = _context.Applications.AsNoTracking();
        if (_tenantService.OrganizationId.HasValue)
        {
            appsQuery = appsQuery.Where(a => a.OrganizationId == _tenantService.OrganizationId.Value);
        }

        var appIds = await appsQuery.Select(a => a.Id).ToListAsync(cancellationToken);
        var totalApps = appIds.Count;

        var registeredServices = await _context.Services
            .AsNoTracking()
            .Where(s => !_tenantService.OrganizationId.HasValue || appIds.Contains(s.ApplicationId))
            .CountAsync(cancellationToken);

        var activeTelemetryServices = await _context.TraceRecords
            .AsNoTracking()
            .Where(t => (!_tenantService.OrganizationId.HasValue || appIds.Contains(t.ApplicationId)) && t.StartedAtUtc >= todayUtc)
            .Select(t => t.ServiceName)
            .Distinct()
            .CountAsync(cancellationToken);
        var totalServices = Math.Max(registeredServices, activeTelemetryServices);

        var activeAlerts = await _context.AlertIncidents
            .AsNoTracking()
            .Where(i => i.Status == AlertStatus.Firing && (!_tenantService.OrganizationId.HasValue || _context.AlertRules.Any(r => r.Id == i.AlertRuleId && appIds.Contains(r.ApplicationId))))
            .CountAsync(cancellationToken);

        var totalLogsToday = await _context.LogEntries
            .AsNoTracking()
            .Where(l => (!_tenantService.OrganizationId.HasValue || appIds.Contains(l.ApplicationId)) && l.Timestamp >= todayUtc)
            .CountAsync(cancellationToken);

        var errorLogsToday = await _context.LogEntries
            .AsNoTracking()
            .Where(l => (!_tenantService.OrganizationId.HasValue || appIds.Contains(l.ApplicationId)) && l.Timestamp >= todayUtc && (l.Severity == Severity.Error || l.Severity == Severity.Critical))
            .CountAsync(cancellationToken);

        var totalTracesToday = await _context.TraceRecords
            .AsNoTracking()
            .Where(t => (!_tenantService.OrganizationId.HasValue || appIds.Contains(t.ApplicationId)) && t.StartedAtUtc >= todayUtc)
            .CountAsync(cancellationToken);

        // Average response time from live traces first, falling back to health checks
        var avgTraceDuration = await _context.TraceRecords
            .AsNoTracking()
            .Where(t => (!_tenantService.OrganizationId.HasValue || appIds.Contains(t.ApplicationId)) && t.StartedAtUtc >= todayUtc)
            .Select(t => (double?)t.Duration.TotalMilliseconds)
            .AverageAsync(cancellationToken);

        var avgResponseTime = avgTraceDuration
            ?? await _context.HealthCheckResults
                .AsNoTracking()
                .Where(h => !_tenantService.OrganizationId.HasValue || appIds.Contains(h.ApplicationId))
                .Select(h => (double?)h.ResponseTimeMs)
                .AverageAsync(cancellationToken)
            ?? 0.0;

        var healthyServices = await _context.HealthCheckResults
            .AsNoTracking()
            .Where(h => (!_tenantService.OrganizationId.HasValue || appIds.Contains(h.ApplicationId)) && h.Status == HealthStatus.Healthy)
            .CountAsync(cancellationToken);

        if (healthyServices == 0 && totalServices > 0)
        {
            var servicesWithErrors = await _context.TraceRecords
                .AsNoTracking()
                .Where(t => (!_tenantService.OrganizationId.HasValue || appIds.Contains(t.ApplicationId)) && t.StartedAtUtc >= todayUtc && t.HasErrors)
                .Select(t => t.ServiceName)
                .Distinct()
                .CountAsync(cancellationToken);
            healthyServices = Math.Max(0, totalServices - servicesWithErrors);
        }

        var errorRate = totalLogsToday > 0
            ? Math.Round((double)errorLogsToday / totalLogsToday * 100, 2)
            : 0.0;

        return new DashboardSummaryDto
        {
            TotalApplications = totalApps,
            TotalServices = totalServices,
            ActiveAlerts = activeAlerts,
            HealthyServices = healthyServices,
            ErrorRate = errorRate,
            AvgResponseTimeMs = Math.Round(avgResponseTime, 2),
            TotalLogsToday = totalLogsToday,
            TotalTracesToday = totalTracesToday
        };
    }
}
