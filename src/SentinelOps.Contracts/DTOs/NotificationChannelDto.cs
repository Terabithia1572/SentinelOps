namespace SentinelOps.Contracts.DTOs;

public record NotificationChannelDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public Guid? ApplicationId { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public record InAppNotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? LinkUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
