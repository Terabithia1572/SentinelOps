namespace SentinelOps.Contracts.DTOs;

public record DashboardSummaryDto
{
    public int TotalApplications { get; init; }
    public int TotalServices { get; init; }
    public int ActiveAlerts { get; init; }
    public int HealthyServices { get; init; }
    public double ErrorRate { get; init; }
    public double AvgResponseTimeMs { get; init; }
    public long TotalLogsToday { get; init; }
    public long TotalTracesToday { get; init; }
}
