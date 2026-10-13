namespace SentinelOps.Contracts.DTOs;

public record AlertEventDto
{
    public Guid Id { get; init; }
    public Guid AlertRuleId { get; init; }
    public string AlertRuleName { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public double MeasuredValue { get; init; }
    public double Threshold { get; init; }
    public DateTime TriggeredAtUtc { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record AlertSummaryDto
{
    public int TotalRules { get; init; }
    public int EnabledRules { get; init; }
    public int FiringAlerts { get; init; }
    public int AcknowledgedAlerts { get; init; }
    public int PendingAlerts { get; init; }
    public int CriticalAlerts { get; init; }
    public int WarningAlerts { get; init; }
}
