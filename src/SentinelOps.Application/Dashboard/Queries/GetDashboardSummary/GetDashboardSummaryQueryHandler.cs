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
        var totalServices = await _context.Services.CountAsync(cancellationToken);
        var activeAlerts = await _context.AlertIncidents
            .CountAsync(i => i.Status == AlertStatus.Firing, cancellationToken);
        var healthyServices = await _context.HealthCheckResults
            .CountAsync(h => h.Status == HealthStatus.Healthy, cancellationToken);

        var totalLogsToday = await _context.LogEntries
            .CountAsync(l => l.Timestamp >= todayUtc, cancellationToken);
        var errorLogsToday = await _context.LogEntries
            .CountAsync(l => l.Timestamp >= todayUtc && (l.Severity == Severity.Error || l.Severity == Severity.Critical), cancellationToken);

        var totalTracesToday = await _context.TraceRecords
            .CountAsync(t => t.StartedAtUtc >= todayUtc, cancellationToken);

        var avgResponseTime = await _context.HealthCheckResults
            .Select(h => (double?)h.ResponseTimeMs)
            .AverageAsync(cancellationToken) ?? 0.0;

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
