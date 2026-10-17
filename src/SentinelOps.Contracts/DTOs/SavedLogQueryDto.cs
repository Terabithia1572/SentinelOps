namespace SentinelOps.Contracts.DTOs;

public record SavedLogQueryDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ApplicationId { get; init; }
    public string? ApplicationName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SearchTerm { get; init; }
    public string? Severity { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public string? TraceId { get; init; }
    public string? PropertyFiltersJson { get; init; }
    public string TimeRange { get; init; } = "1h";
    public string? CreatedByUserId { get; init; }
    public string? CreatedByUserName { get; init; }
    public bool IsShared { get; init; } = true;
    public List<string> Tags { get; init; } = [];
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
