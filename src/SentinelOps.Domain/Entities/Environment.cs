using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class Environment : Entity
{
    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public bool IsActive { get; set; } = true;
}
