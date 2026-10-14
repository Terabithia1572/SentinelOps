using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class SubscriptionPlan : Entity
{
    public string Tier { get; set; } = "Starter"; // "Starter", "Pro", "Enterprise"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Demonstration pricing (clearly labeled as demonstration)
    public decimal MonthlyPriceUsd { get; set; }
    public decimal AnnualPriceUsd { get; set; }

    // Entitlements & Hard Quotas
    public int MaxApplications { get; set; } = 3;
    public int MaxMembers { get; set; } = 3;
    public int MaxCustomDashboards { get; set; } = 3;
    public long MaxMonthlyLogs { get; set; } = 100_000;
    public long MaxMonthlyTraces { get; set; } = 50_000;
    public long MaxMonthlyMetrics { get; set; } = 100_000;
    public int RetentionDays { get; set; } = 7;
    public int MaxMonthlyAiTokens { get; set; } = 10_000;

    // Feature Flags & Entitlements
    public bool AllowCustomReports { get; set; } = false;
    public bool AllowSloAlerts { get; set; } = false;
    public bool AllowApiAccess { get; set; } = true;
    public bool AllowDedicatedSupport { get; set; } = false;
}
