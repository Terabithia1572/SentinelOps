namespace SentinelOps.Contracts.DTOs;

public record EnvironmentDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string EnvironmentType { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
