using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IIncidentAnalysisService
{
    Task<IncidentAnalysisResult> AnalyzeIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<IncidentPostmortemResult> GeneratePostmortemAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<RemediationSuggestionsResult> SuggestRemediationAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<AiCopilotChatResponse> AskCopilotAsync(Guid incidentId, string question, CancellationToken cancellationToken = default);
    Task<bool> RecordFeedbackAsync(Guid incidentId, IntelligenceFeedbackRequest feedback, CancellationToken cancellationToken = default);
}
