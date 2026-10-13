using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class OrganizationInvitation : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string InvitedByUserId { get; set; } = string.Empty;

    public bool IsActive => AcceptedAtUtc == null && RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;

    public void Accept()
    {
        AcceptedAtUtc = DateTime.UtcNow;
    }

    public void Revoke()
    {
        RevokedAtUtc = DateTime.UtcNow;
    }
}
