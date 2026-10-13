namespace SentinelOps.Contracts.DTOs;

public class IncidentAnalysisResult
{
    public Guid IncidentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ProviderName { get; set; } = "Deterministic Heuristic Engine";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<RootCauseHypothesisDto> Hypotheses { get; set; } = [];
    public CorrelatedTelemetryDto CorrelatedTelemetry { get; set; } = new();
    public List<SimilarIncidentDto> SimilarIncidents { get; set; } = [];
}

public class RootCauseHypothesisDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; } // 0-100%
    public List<string> Evidence { get; set; } = [];
    public List<string> RecommendedDiagnostics { get; set; } = [];
}

public class CorrelatedTelemetryDto
{
    public int LogCount { get; set; }
    public List<string> TopExceptions { get; set; } = [];
    public List<string> AnomalousMetrics { get; set; } = [];
    public List<string> SlowTraceIds { get; set; } = [];
}

public class SimilarIncidentDto
{
    public Guid IncidentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? ResolvedAtUtc { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public int MttrMinutes { get; set; }
}

public class IncidentPostmortemResult
{
    public Guid IncidentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MarkdownContent { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RemediationSuggestionsResult
{
    public Guid IncidentId { get; set; }
    public List<RemediationActionDto> Actions { get; set; } = [];
}

public class RemediationActionDto
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CommandOrAction { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High
    public string Rationale { get; set; } = string.Empty;
}

public class IntelligenceFeedbackRequest
{
    public string HypothesisId { get; set; } = string.Empty;
    public bool IsHelpful { get; set; }
    public string? Comment { get; set; }
}
