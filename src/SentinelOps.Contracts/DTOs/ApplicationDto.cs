namespace SentinelOps.Contracts.DTOs;

public record ApplicationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public IReadOnlyList<EnvironmentDto> Environments { get; init; } = [];
    public IReadOnlyList<ServiceDto> Services { get; init; } = [];
}
