namespace SentinelOps.Contracts.DTOs;

public record SloDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? EnvironmentName { get; init; }
    public string IndicatorType { get; init; } = string.Empty;
    public double TargetPercentage { get; init; }
    public string EvaluationWindow { get; init; } = string.Empty;
    public double? LatencyThresholdMs { get; init; }
    public bool IsEnabled { get; init; }
    public double? CurrentCompliancePercentage { get; init; }
    public double? CurrentErrorBudgetRemaining { get; init; }
    public double? BurnRate { get; init; }
    public bool IsBreached { get; init; }
    public DateTime? LastEvaluatedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public record SloComplianceHistoryDto
{
    public Guid ServiceLevelObjectiveId { get; init; }
    public string Name { get; init; } = string.Empty;
    public double TargetPercentage { get; init; }
    public double CurrentCompliance { get; init; }
    public double ErrorBudgetRemaining { get; init; }
    public double BurnRate { get; init; }
    public bool IsBreached { get; init; }
    public List<SloEvaluationPointDto> History { get; init; } = new();
}

public record SloEvaluationPointDto
{
    public DateTime Timestamp { get; init; }
    public double CompliancePercentage { get; init; }
    public double ErrorBudgetRemaining { get; init; }
    public double BurnRate { get; init; }
    public long TotalRequests { get; init; }
    public long GoodRequests { get; init; }
    public bool IsBreached { get; init; }
}
