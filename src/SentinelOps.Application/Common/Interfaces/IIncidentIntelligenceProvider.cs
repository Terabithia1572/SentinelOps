using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IIncidentIntelligenceProvider
{
    string ProviderName { get; }
    Task<IncidentAnalysisResult> AnalyzeIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<IncidentPostmortemResult> GeneratePostmortemAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<RemediationSuggestionsResult> SuggestRemediationAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<bool> RecordFeedbackAsync(Guid incidentId, IntelligenceFeedbackRequest feedback, CancellationToken cancellationToken = default);
}
