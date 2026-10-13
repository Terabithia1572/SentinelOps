using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class ServiceLevelObjective : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? EnvironmentName { get; set; }
    public SloIndicatorType IndicatorType { get; set; } = SloIndicatorType.Availability;
    public double TargetPercentage { get; set; } = 99.9;
    public SloEvaluationWindow EvaluationWindow { get; set; } = SloEvaluationWindow.Rolling7Days;
    public double? LatencyThresholdMs { get; set; } = 500.0;
    public bool IsEnabled { get; set; } = true;
    public double? CurrentCompliancePercentage { get; set; }
    public double? CurrentErrorBudgetRemaining { get; set; }
    public double? BurnRate { get; set; }
    public bool IsBreached { get; set; } = false;
    public DateTime? LastEvaluatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<SloEvaluation> Evaluations { get; set; } = new List<SloEvaluation>();

    public SloEvaluation RecordEvaluation(long totalRequests, long goodRequests, DateTime evaluatedAtUtc)
    {
        LastEvaluatedAtUtc = evaluatedAtUtc;

        double compliance;
        double budgetRemaining;
        double burnRate;

        if (totalRequests == 0)
        {
            compliance = 100.0;
            budgetRemaining = 100.0;
            burnRate = 0.0;
        }
        else
        {
            compliance = Math.Round((double)goodRequests / totalRequests * 100.0, 4);

            double allowedErrorFraction = (100.0 - TargetPercentage) / 100.0;
            long allowedBadRequests = (long)Math.Ceiling(totalRequests * allowedErrorFraction);
            long actualBadRequests = Math.Max(0, totalRequests - goodRequests);

            if (allowedBadRequests <= 0)
            {
                budgetRemaining = actualBadRequests == 0 ? 100.0 : 0.0;
                burnRate = actualBadRequests == 0 ? 0.0 : 999.0;
            }
            else
            {
                budgetRemaining = Math.Round(((double)(allowedBadRequests - actualBadRequests) / allowedBadRequests) * 100.0, 2);
                double actualErrorFraction = (double)actualBadRequests / totalRequests;
                burnRate = allowedErrorFraction > 0 ? Math.Round(actualErrorFraction / allowedErrorFraction, 2) : 0.0;
            }
        }

        CurrentCompliancePercentage = compliance;
        CurrentErrorBudgetRemaining = budgetRemaining;
        BurnRate = burnRate;
        IsBreached = compliance < TargetPercentage || budgetRemaining <= 0;

        var evaluation = new SloEvaluation
        {
            ServiceLevelObjectiveId = Id,
            EvaluatedAtUtc = evaluatedAtUtc,
            TotalRequests = totalRequests,
            GoodRequests = goodRequests,
            CompliancePercentage = compliance,
            ErrorBudgetRemaining = budgetRemaining,
            BurnRate = burnRate,
            IsBreached = IsBreached
        };

        Evaluations.Add(evaluation);
        return evaluation;
    }
}
