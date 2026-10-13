namespace SentinelOps.Contracts.Requests;

public record CreateAlertRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string? EnvironmentName { get; init; }
    public string? ServiceName { get; init; }
    public string ConditionType { get; init; } = "ErrorRateAboveThreshold";
    public string Operator { get; init; } = "GreaterThan";
    public string MetricName { get; init; } = "error_rate";
    public double Threshold { get; init; }
    public int EvaluationWindowMinutes { get; init; } = 5;
    public int EvaluationFrequencyMinutes { get; init; } = 1;
    public int ConsecutiveCountRequired { get; init; } = 1;
    public int CooldownMinutes { get; init; } = 15;
    public string Severity { get; init; } = "Warning";
    public List<Guid>? NotificationChannelIds { get; init; }
}

public record UpdateAlertRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? EnvironmentName { get; init; }
    public string? ServiceName { get; init; }
    public string ConditionType { get; init; } = "ErrorRateAboveThreshold";
    public string Operator { get; init; } = "GreaterThan";
    public string MetricName { get; init; } = "error_rate";
    public double Threshold { get; init; }
    public int EvaluationWindowMinutes { get; init; } = 5;
    public int EvaluationFrequencyMinutes { get; init; } = 1;
    public int ConsecutiveCountRequired { get; init; } = 1;
    public int CooldownMinutes { get; init; } = 15;
    public string Severity { get; init; } = "Warning";
    public List<Guid>? NotificationChannelIds { get; init; }
}

public record AcknowledgeAlertRequest
{
    public string? Notes { get; init; }
}

public record ResolveAlertRequest
{
    public string Reason { get; init; } = string.Empty;
}
