namespace SentinelOps.Contracts.DTOs;

public class OnboardingStatusDto
{
    public bool IsCompleted { get; set; }
    public string CurrentStep { get; set; } = "Organization"; // "Organization", "Application", "SdkIntegration", "TelemetryVerification", "Completed"
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationSlug { get; set; }
    public string? PlanTier { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public bool HasApiKey { get; set; }
    public string? ApiKeyPrefix { get; set; }
    public bool HasReceivedTelemetry { get; set; }
    public DateTime? FirstTelemetryReceivedAtUtc { get; set; }
}

public class OnboardingProvisionRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string? OrganizationSlug { get; set; }
    public string PlanTier { get; set; } = "Starter"; // "Starter", "Pro", "Enterprise"
    public string ApplicationName { get; set; } = string.Empty;
    public string? ApplicationDisplayName { get; set; }
    public string EnvironmentName { get; set; } = "Production";
}

public class OnboardingProvisionedResponse
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationSlug { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public Guid ApiKeyId { get; set; }
    public string RawApiKey { get; set; } = string.Empty; // Returned ONCE upon creation - never stored raw
    public string KeyPrefix { get; set; } = string.Empty;
    public string PlanTier { get; set; } = "Starter";
    public string SubscriptionStatus { get; set; } = "Trialing";
    public DateTime? TrialEndsAtUtc { get; set; }
    public string SdkSnippetCSharp { get; set; } = string.Empty;
    public string SdkSnippetNodeJs { get; set; } = string.Empty;
    public string SdkSnippetPython { get; set; } = string.Empty;
    public string OtelCollectorYaml { get; set; } = string.Empty;
}

public class FirstTelemetryVerificationResponse
{
    public Guid ApplicationId { get; set; }
    public bool HasReceivedTelemetry { get; set; }
    public int LogsCount { get; set; }
    public int TracesCount { get; set; }
    public int MetricsCount { get; set; }
    public DateTime? FirstReceivedAtUtc { get; set; }
    public DateTime? LastReceivedAtUtc { get; set; }
}
