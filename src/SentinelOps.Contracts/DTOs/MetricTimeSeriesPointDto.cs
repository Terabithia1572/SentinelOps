namespace SentinelOps.Contracts.DTOs;

public record MetricTimeSeriesPointDto
{
    public DateTime Timestamp { get; init; }
    public double RequestsPerSecond { get; init; }
    public double ErrorRatePercent { get; init; }
    public double AvgDurationMs { get; init; }
    public double P50DurationMs { get; init; }
    public double P95DurationMs { get; init; }
    public double P99DurationMs { get; init; }
    public int TotalRequests { get; init; }
    public int ErrorRequests { get; init; }
}

public record MetricSeriesResponse
{
    public IReadOnlyList<MetricTimeSeriesPointDto> Points { get; init; } = [];
    public double AvgLatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public double OverallErrorRate { get; init; }
    public double TotalThroughputRps { get; init; }
    public int TotalRequests { get; init; }
}
