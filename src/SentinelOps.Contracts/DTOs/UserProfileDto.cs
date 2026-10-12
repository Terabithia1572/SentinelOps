namespace SentinelOps.Contracts.DTOs;

public record UserProfileDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
