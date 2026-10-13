using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class SystemSetting : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = "system";
}
