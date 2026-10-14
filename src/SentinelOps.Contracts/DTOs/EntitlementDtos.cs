namespace SentinelOps.Contracts.DTOs;

public enum EntitlementFeature
{
    RegisterApplication,
    InviteMember,
    CreateDashboard,
    IngestLogs,
    IngestTraces,
    IngestMetrics,
    ExecuteAiInference,
    CustomReports,
    SloAlerts,
    ApiAccess,
    DedicatedSupport
}

public enum MeteredMetric
{
    Logs,
    Traces,
    Metrics,
    AiTokens,
    ApiRequests
}

public class TenantEntitlementsDto
{
    public Guid OrganizationId { get; set; }
    public string PlanTier { get; set; } = "Starter";
    public string SubscriptionStatus { get; set; } = "Active";
    public bool IsSubscriptionActive { get; set; } = true;
    public int DaysUntilPeriodEnd { get; set; }
    public bool CanRegisterApplication { get; set; } = true;
    public bool CanInviteMember { get; set; } = true;
    public bool CanCreateDashboard { get; set; } = true;
    public bool CanIngestTelemetry { get; set; } = true;
    public bool CanExecuteAi { get; set; } = true;
    public bool AllowCustomReports { get; set; }
    public bool AllowSloAlerts { get; set; }
    public bool AllowApiAccess { get; set; } = true;
    public bool AllowDedicatedSupport { get; set; }
    public List<string> InactiveReasons { get; set; } = new();
}

public class TenantUsageSummaryDto
{
    public Guid OrganizationId { get; set; }
    public string PlanTier { get; set; } = "Starter";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public int DaysRemainingInPeriod { get; set; }

    // Telemetry & Compute Metrics
    public long LogsUsed { get; set; }
    public long LogsQuota { get; set; }
    public double LogsPercent { get; set; }
    public bool LogsExceeded => LogsUsed >= LogsQuota && LogsQuota > 0;

    public long TracesUsed { get; set; }
    public long TracesQuota { get; set; }
    public double TracesPercent { get; set; }
    public bool TracesExceeded => TracesUsed >= TracesQuota && TracesQuota > 0;

    public long MetricsUsed { get; set; }
    public long MetricsQuota { get; set; }
    public double MetricsPercent { get; set; }
    public bool MetricsExceeded => MetricsUsed >= MetricsQuota && MetricsQuota > 0;

    public long AiTokensUsed { get; set; }
    public long AiTokensQuota { get; set; }
    public double AiTokensPercent { get; set; }
    public bool AiTokensExceeded => AiTokensUsed >= AiTokensQuota && AiTokensQuota > 0;

    // Entity Count Metrics
    public int ApplicationsUsed { get; set; }
    public int ApplicationsQuota { get; set; }
    public double ApplicationsPercent { get; set; }
    public bool ApplicationsExceeded => ApplicationsUsed >= ApplicationsQuota && ApplicationsQuota > 0;

    public int MembersUsed { get; set; }
    public int MembersQuota { get; set; }
    public double MembersPercent { get; set; }
    public bool MembersExceeded => MembersUsed >= MembersQuota && MembersQuota > 0;

    public int DashboardsUsed { get; set; }
    public int DashboardsQuota { get; set; }
    public double DashboardsPercent { get; set; }
    public bool DashboardsExceeded => DashboardsUsed >= DashboardsQuota && DashboardsQuota > 0;

    public bool HasAnyQuotaWarning => LogsPercent >= 80.0 || TracesPercent >= 80.0 || MetricsPercent >= 80.0 ||
                                      AiTokensPercent >= 80.0 || ApplicationsPercent >= 80.0 || MembersPercent >= 80.0 || DashboardsPercent >= 80.0;

    public bool HasAnyQuotaExceeded => LogsExceeded || TracesExceeded || MetricsExceeded ||
                                       AiTokensExceeded || ApplicationsExceeded || MembersExceeded || DashboardsExceeded;
}
