namespace SentinelOps.Contracts.DTOs;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public int MembersCount { get; set; }
    public int ApplicationsCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class OrganizationDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public List<OrganizationMemberDto> Members { get; set; } = new();
    public List<OrganizationInvitationDto> PendingInvitations { get; set; } = new();
    public OrganizationSettingsDto Settings { get; set; } = new();
    public OrganizationUsageDto Usage { get; set; } = new();
}

public class OrganizationMemberDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
}

public class OrganizationInvitationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string InvitedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsPending { get; set; }
}

public class OrganizationSettingsDto
{
    public bool AllowMemberInvites { get; set; } = true;
    public bool RequireTwoFactor { get; set; }
    public int DefaultRetentionDays { get; set; } = 30;
    public string PlanTier { get; set; } = "Pro";
    public int MaxApplications { get; set; } = 20;
    public int MaxMembers { get; set; } = 25;
    public int MaxCustomDashboards { get; set; } = 15;
    public long MaxMonthlyLogs { get; set; } = 1_000_000;
    public long MaxMonthlyTraces { get; set; } = 500_000;
}

public class OrganizationUsageDto
{
    public long LogsIngestedMonthToDate { get; set; }
    public long TracesIngestedMonthToDate { get; set; }
    public long MetricsIngestedMonthToDate { get; set; }
    public long ApiRequestsMonthToDate { get; set; }
    public long EstimatedStorageBytes { get; set; }
    public DateTime LastResetAtUtc { get; set; }
}

public class OrganizationQuotaStatusDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string PlanTier { get; set; } = "Pro";

    public long LogsUsed { get; set; }
    public long LogsQuota { get; set; }
    public double LogsPercentUsed { get; set; }
    public bool IsLogsWarning { get; set; }
    public bool IsLogsExceeded { get; set; }

    public long TracesUsed { get; set; }
    public long TracesQuota { get; set; }
    public double TracesPercentUsed { get; set; }
    public bool IsTracesWarning { get; set; }
    public bool IsTracesExceeded { get; set; }

    public int ApplicationsUsed { get; set; }
    public int ApplicationsQuota { get; set; }
    public double ApplicationsPercentUsed { get; set; }

    public int DashboardsUsed { get; set; }
    public int DashboardsQuota { get; set; }
    public double DashboardsPercentUsed { get; set; }

    public int MembersUsed { get; set; }
    public int MembersQuota { get; set; }
    public double MembersPercentUsed { get; set; }

    public long EstimatedStorageBytes { get; set; }
    public bool HasAnyQuotaWarning { get; set; }
    public bool HasAnyQuotaExceeded { get; set; }
}

public class UpdateOrganizationQuotaRequest
{
    public string PlanTier { get; set; } = "Pro";
    public int MaxApplications { get; set; } = 20;
    public int MaxMembers { get; set; } = 25;
    public int MaxCustomDashboards { get; set; } = 15;
    public long MaxMonthlyLogs { get; set; } = 1_000_000;
    public long MaxMonthlyTraces { get; set; } = 500_000;
}
