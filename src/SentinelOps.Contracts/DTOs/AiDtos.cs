namespace SentinelOps.Contracts.DTOs;

public class AiOptions
{
    public bool Enabled { get; set; } = true;
    public string DefaultProvider { get; set; } = "Deterministic"; // "Disabled", "Deterministic", "ExternalLlm"
    public ExternalLlmOptions ExternalProvider { get; set; } = new();
}

public class ExternalLlmOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int MaxTokens { get; set; } = 2000;
    public int MonthlyTokenBudget { get; set; } = 1_000_000;
    public int MaxConcurrency { get; set; } = 5;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
}

public class AiCompletionRequest
{
    public Guid OrganizationId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.2;
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public class AiCompletionResult
{
    public string ResultText { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public bool IsDeterministic { get; set; }
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user"; // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AiCopilotChatRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> History { get; set; } = [];
}

public class AiCopilotChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsDeterministic { get; set; } = true;
    public List<string> Citations { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];
    public bool RequiresHumanApproval { get; set; } = true;
    public int TokensUsed { get; set; }
}

public class AnomalyEvaluationRequest
{
    public Guid OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string SignalType { get; set; } = "ErrorRate";
    public double CurrentValue { get; set; }
    public List<double> HistoricalValues { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AnomalyDetectionResult
{
    public bool IsAnomaly { get; set; }
    public double BaselineValue { get; set; }
    public double ObservedValue { get; set; }
    public double DeviationScore { get; set; }
    public double ConfidenceScore { get; set; }
    public string Severity { get; set; } = "Warning";
    public string DetectionMethod { get; set; } = "RollingZScore";
    public string Explanation { get; set; } = string.Empty;
}

public class TelemetryAnomalyDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public DateTime DetectionTimestamp { get; set; }
    public double BaselineValue { get; set; }
    public double ObservedValue { get; set; }
    public double DeviationScore { get; set; }
    public double ConfidenceScore { get; set; }
    public string Severity { get; set; } = "Warning";
    public string DetectionMethod { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime? ResolvedAtUtc { get; set; }
}

public class LogPatternDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string PatternFingerprint { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public long TotalOccurrences { get; set; }
    public DateTime FirstSeenAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public string SampleMessage { get; set; } = string.Empty;
    public string? SampleTraceId { get; set; }
}

public class LogPatternExtractionResult
{
    public string PatternFingerprint { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<string> ExtractedParameters { get; set; } = [];
}

public class LogPatternAnalysisResult
{
    public int TotalLogsAnalyzed { get; set; }
    public int PatternsFound { get; set; }
    public List<LogPatternDto> Patterns { get; set; } = [];
}

public class RcaAnalysisRequest
{
    public Guid OrganizationId { get; set; }
    public Guid IncidentId { get; set; }
    public TimeSpan? CorrelationWindow { get; set; }
}

public class RcaHypothesisDto
{
    public string Hypothesis { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = "Possible cause"; // "Likely cause", "Possible cause", "Insufficient evidence"
    public double ConfidenceScore { get; set; }
    public List<string> SupportingEvidence { get; set; } = [];
    public List<string> ContradictoryEvidence { get; set; } = [];
    public List<string> RecommendedSteps { get; set; } = [];
    public bool IsVerifiedByOperator { get; set; }
    public string? OperatorNotes { get; set; }
}

public class RcaAnalysisResult
{
    public Guid IncidentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> ImpactedServices { get; set; } = [];
    public List<RcaHypothesisDto> Hypotheses { get; set; } = [];
    public List<string> CorrelatedAnomalies { get; set; } = [];
    public List<string> CorrelatedTraceIds { get; set; } = [];
    public string ProviderName { get; set; } = "Deterministic Heuristic Engine";
    public bool IsDeterministic { get; set; } = true;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CapacityForecastRequest
{
    public Guid OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = "disk_usage_pct";
    public int HorizonMinutes { get; set; } = 60;
    public double AlertThreshold { get; set; } = 90.0;
}

public class CapacityForecastResult
{
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double PredictedValue { get; set; }
    public double TrendSlope { get; set; }
    public int HorizonMinutes { get; set; }
    public DateTime? ProjectedBreachTimeUtc { get; set; }
    public double ConfidenceIntervalLower { get; set; }
    public double ConfidenceIntervalUpper { get; set; }
    public int SampleCount { get; set; }
    public string CalculationMethod { get; set; } = "LinearRegressionOrdinaryLeastSquares";
    public bool WillBreachThreshold { get; set; }
}

public class PredictiveForecastDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double PredictedValue { get; set; }
    public double TrendSlope { get; set; }
    public int ForecastHorizonMinutes { get; set; }
    public DateTime? ProjectedBreachTimeUtc { get; set; }
    public double ConfidenceIntervalLower { get; set; }
    public double ConfidenceIntervalUpper { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class AiUsageDto
{
    public Guid OrganizationId { get; set; }
    public long TokensUsedMonthToDate { get; set; }
    public int InvocationsMonthToDate { get; set; }
    public int MonthlyTokenBudget { get; set; }
    public long RemainingTokens { get; set; }
    public bool IsQuotaExceeded { get; set; }
    public double EstimatedCostUsd { get; set; }
}
