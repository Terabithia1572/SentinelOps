using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class RefreshToken : Entity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? ipAddress = null, string? replacedBy = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReplacedByTokenHash = replacedBy;
    }
}
