using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class OrganizationSettings : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public bool AllowMemberInvites { get; set; } = true;
    public bool RequireTwoFactor { get; set; } = false;
    public int DefaultRetentionDays { get; set; } = 30;

    // SaaS Quotas & Tier
    public string PlanTier { get; set; } = "Pro"; // Starter, Pro, Enterprise
    public int MaxApplications { get; set; } = 20;
    public int MaxMembers { get; set; } = 25;
    public int MaxCustomDashboards { get; set; } = 15;
    public long MaxMonthlyLogs { get; set; } = 1_000_000;
    public long MaxMonthlyTraces { get; set; } = 500_000;
}
