using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IRootCauseAnalysisService
{
    Task<RcaAnalysisResult> PerformRcaAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<bool> VerifyHypothesisAsync(Guid rcaId, string hypothesisText, bool isConfirmed, string? operatorNotes, CancellationToken cancellationToken = default);
}
