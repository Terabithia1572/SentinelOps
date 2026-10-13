using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class SloEvaluation : Entity
{
    public Guid ServiceLevelObjectiveId { get; set; }
    public ServiceLevelObjective? ServiceLevelObjective { get; set; }
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public long TotalRequests { get; set; }
    public long GoodRequests { get; set; }
    public double CompliancePercentage { get; set; }
    public double ErrorBudgetRemaining { get; set; }
    public double BurnRate { get; set; }
    public bool IsBreached { get; set; }
}
