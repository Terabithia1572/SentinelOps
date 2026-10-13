using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class OrganizationMember : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
