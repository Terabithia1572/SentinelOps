using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface IRetentionService
{
    Task<RetentionPolicyDto> GetRetentionPolicyAsync(CancellationToken cancellationToken = default);

    Task<RetentionPolicyDto> UpdateRetentionPolicyAsync(
        UpdateRetentionPolicyRequest request,
        string updatedBy,
        CancellationToken cancellationToken = default);

    Task<RetentionDryRunResultDto> PreviewRetentionCleanupAsync(
        CancellationToken cancellationToken = default);

    Task<RetentionExecutionResultDto> ExecuteRetentionCleanupAsync(
        ExecuteRetentionCleanupRequest request,
        string executedBy,
        CancellationToken cancellationToken = default);
}
