using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class Service : Entity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public bool IsActive { get; set; } = true;
}
