namespace SentinelOps.Contracts.DTOs;

public record AlertRuleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string? EnvironmentName { get; init; }
    public string? ServiceName { get; init; }
    public string ConditionType { get; init; } = string.Empty;
    public string Operator { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double Threshold { get; init; }
    public int EvaluationWindowMinutes { get; init; }
    public int EvaluationFrequencyMinutes { get; init; }
    public int ConsecutiveCountRequired { get; init; }
    public int CurrentConsecutiveFailures { get; init; }
    public int CooldownMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime? LastEvaluatedAtUtc { get; init; }
    public DateTime? LastFiredAtUtc { get; init; }
    public DateTime? LastResolvedAtUtc { get; init; }
    public double? LastMeasuredValue { get; init; }
}
