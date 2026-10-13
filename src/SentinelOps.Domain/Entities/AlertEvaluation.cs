using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class AlertEvaluation : Entity
{
    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public double MeasuredValue { get; set; }
    public double Threshold { get; set; }
    public bool IsTriggered { get; set; }
    public string Details { get; set; } = string.Empty;
}
