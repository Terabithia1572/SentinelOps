using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class OrganizationUsage : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public long LogsIngestedMonthToDate { get; set; }
    public long TracesIngestedMonthToDate { get; set; }
    public long MetricsIngestedMonthToDate { get; set; }
    public long ApiRequestsMonthToDate { get; set; }
    public long EstimatedStorageBytes { get; set; }
    public long AiTokensUsedMonthToDate { get; set; }
    public int AiInvocationsMonthToDate { get; set; }
    public DateTime LastResetAtUtc { get; set; } = DateTime.UtcNow;
}
