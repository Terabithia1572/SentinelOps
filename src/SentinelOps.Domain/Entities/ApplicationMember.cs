using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class ApplicationMember : Entity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin", "Operator", "Viewer"

    public string? AddedByUserId { get; set; }
}
