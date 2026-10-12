namespace SentinelOps.Contracts.DTOs;

public record ServiceDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
