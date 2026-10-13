namespace SentinelOps.Application.Common.Interfaces;

public interface ISloEvaluationService
{
    Task<SloEvaluationResult> EvaluateSloAsync(Guid sloId, CancellationToken cancellationToken = default);
    Task<int> EvaluateAllPendingSlosAsync(CancellationToken cancellationToken = default);
}

public record SloEvaluationResult(
    Guid SloId,
    bool Evaluated,
    double CompliancePercentage,
    double ErrorBudgetRemaining,
    double BurnRate,
    bool IsBreached,
    string Message
);
