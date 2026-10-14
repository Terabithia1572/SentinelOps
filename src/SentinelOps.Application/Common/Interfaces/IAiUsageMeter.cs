using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IAiUsageMeter
{
    Task<bool> CanExecuteAiOperationAsync(Guid organizationId, int estimatedTokens = 1000, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(Guid organizationId, int tokensUsed, double estimatedCostUsd = 0.0, CancellationToken cancellationToken = default);
    Task<AiUsageDto> GetUsageAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
