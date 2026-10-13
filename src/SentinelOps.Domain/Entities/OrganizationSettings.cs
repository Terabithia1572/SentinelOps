using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class OrganizationSettings : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public bool AllowMemberInvites { get; set; } = true;
    public bool RequireTwoFactor { get; set; } = false;
    public int DefaultRetentionDays { get; set; } = 30;
}
