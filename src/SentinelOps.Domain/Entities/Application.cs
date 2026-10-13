using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class Application : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ApiKeyHash { get; set; } = string.Empty;

    public ICollection<Environment> Environments { get; set; } = new List<Environment>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<ApplicationMember> Members { get; set; } = new List<ApplicationMember>();
}

