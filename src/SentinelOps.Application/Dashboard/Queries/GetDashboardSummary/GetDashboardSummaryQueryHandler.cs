using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var totalApps = await _context.Applications.CountAsync(cancellationToken);
        
        var registeredServices = await _context.Services.CountAsync(cancellationToken);
        var activeTelemetryServices = await _context.TraceRecords
            .Where(t => t.StartedAtUtc >= todayUtc)
            .Select(t => t.ServiceName)
            .Distinct()
            .CountAsync(cancellationToken);
        var totalServices = Math.Max(registeredServices, activeTelemetryServices);

        var activeAlerts = await _context.AlertIncidents
            .CountAsync(i => i.Status == AlertStatus.Firing, cancellationToken);

        var totalLogsToday = await _context.LogEntries
            .CountAsync(l => l.Timestamp >= todayUtc, cancellationToken);
        var errorLogsToday = await _context.LogEntries
            .CountAsync(l => l.Timestamp >= todayUtc && (l.Severity == Severity.Error || l.Severity == Severity.Critical), cancellationToken);

        var totalTracesToday = await _context.TraceRecords
            .CountAsync(t => t.StartedAtUtc >= todayUtc, cancellationToken);

        // Average response time from live traces first, falling back to health checks
        var avgTraceDuration = await _context.TraceRecords
            .Where(t => t.StartedAtUtc >= todayUtc)
            .Select(t => (double?)t.Duration.TotalMilliseconds)
            .AverageAsync(cancellationToken);

        var avgResponseTime = avgTraceDuration
            ?? await _context.HealthCheckResults.Select(h => (double?)h.ResponseTimeMs).AverageAsync(cancellationToken)
            ?? 0.0;

        var healthyServices = await _context.HealthCheckResults
            .CountAsync(h => h.Status == HealthStatus.Healthy, cancellationToken);
        if (healthyServices == 0 && totalServices > 0)
        {
            var servicesWithErrors = await _context.TraceRecords
                .Where(t => t.StartedAtUtc >= todayUtc && t.HasErrors)
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
