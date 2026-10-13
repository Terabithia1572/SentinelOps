namespace SentinelOps.Application.Common.Interfaces;

public interface IAlertEvaluationService
{
    Task<AlertEvaluationResult> EvaluateRuleAsync(Guid alertRuleId, CancellationToken cancellationToken = default);
    Task<int> EvaluateAllPendingRulesAsync(CancellationToken cancellationToken = default);
}

public record AlertEvaluationResult(
    Guid AlertRuleId,
    bool Evaluated,
    double MeasuredValue,
    double Threshold,
    bool ConditionMet,
    string OldStatus,
    string NewStatus,
    bool StateChanged,
    string Message
);
