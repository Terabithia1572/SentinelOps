using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Ai;

public class DisabledAiProvider : IAiProvider
{
    public string ProviderId => "Disabled";
    public string ProviderName => "Disabled AI Provider";
    public bool IsEnabled => false;

    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AiCompletionResult
        {
            IsSuccessful = false,
            ResultText = "AI operations are disabled for this organization or system configuration.",
            ErrorMessage = "AI provider is disabled.",
            ProviderName = ProviderName,
            Model = "None",
            DurationMs = 0,
            TokensUsed = 0,
            IsDeterministic = true
        });
    }

    public Task<AiCopilotChatResponse> ChatAsync(AiCopilotChatRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AiCopilotChatResponse
        {
            Reply = "AI Copilot is currently disabled by system policy or organization preference. Please enable AI features in Organization Settings to interact with Copilot.",
            ProviderName = ProviderName,
            IsDeterministic = true,
            Citations = [],
            RecommendedActions = ["Review Organization Settings > AI Configuration."],
            RequiresHumanApproval = true,
            TokensUsed = 0
        });
    }
}
