using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class HealthCheckResult : Entity
{
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    public string Description { get; set; } = string.Empty;
    public double ResponseTimeMs { get; set; }
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}
