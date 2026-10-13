namespace SentinelOps.Contracts.Requests;

public record CreateSloRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? EnvironmentName { get; init; }
    public string IndicatorType { get; init; } = "Availability";
    public double TargetPercentage { get; init; } = 99.9;
    public string EvaluationWindow { get; init; } = "Rolling7Days";
    public double? LatencyThresholdMs { get; init; } = 500.0;
}

public record UpdateSloRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double TargetPercentage { get; init; } = 99.9;
    public string EvaluationWindow { get; init; } = "Rolling7Days";
    public double? LatencyThresholdMs { get; init; } = 500.0;
    public bool IsEnabled { get; init; } = true;
}
