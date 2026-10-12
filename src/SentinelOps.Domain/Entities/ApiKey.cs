using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class ApiKey : Entity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsActive => RevokedAtUtc == null && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > DateTime.UtcNow);

    public void Revoke()
    {
        RevokedAtUtc = DateTime.UtcNow;
    }
}
