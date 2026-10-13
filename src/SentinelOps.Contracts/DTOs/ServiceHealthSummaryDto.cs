namespace SentinelOps.Contracts.DTOs;

public record ServiceHealthSummaryDto
{
    public string ServiceName { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string ApplicationName { get; init; } = string.Empty;
    public string Status { get; init; } = "Healthy";
    public int TotalRequests { get; init; }
    public int TotalErrors { get; init; }
    public double ErrorRatePercent { get; init; }
    public double AvgDurationMs { get; init; }
    public double P95DurationMs { get; init; }
    public DateTime? LastSeenAtUtc { get; init; }
}
