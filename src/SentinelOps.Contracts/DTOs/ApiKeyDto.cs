namespace SentinelOps.Contracts.DTOs;

public record ApiKeyDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
    public DateTime? LastUsedAtUtc { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
