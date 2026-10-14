using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IAnomalyDetectionService
{
    Task<AnomalyDetectionResult> EvaluateSignalAsync(AnomalyEvaluationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TelemetryAnomalyDto>> GetActiveAnomaliesAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ResolveAnomalyAsync(Guid anomalyId, string? resolvedBy, CancellationToken cancellationToken = default);
}
