using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IAiProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    bool IsEnabled { get; }

    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default);
    Task<AiCopilotChatResponse> ChatAsync(AiCopilotChatRequest request, CancellationToken cancellationToken = default);
}
