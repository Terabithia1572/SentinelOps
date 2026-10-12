using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class AlertRule : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string Condition { get; set; } = ">";
    public double Threshold { get; set; }
    public int EvaluationWindowMinutes { get; set; } = 5;
    public int CooldownMinutes { get; set; } = 15;
    public AlertStatus Status { get; set; } = AlertStatus.Normal;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastEvaluatedAtUtc { get; set; }

    public ICollection<AlertIncident> Incidents { get; set; } = new List<AlertIncident>();
}
