using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class IncidentComment : Entity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
}
