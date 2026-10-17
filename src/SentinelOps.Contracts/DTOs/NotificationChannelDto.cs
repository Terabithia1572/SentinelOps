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
    public Guid? OrganizationId { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public record NotificationHistoryDto
{
    public Guid Id { get; init; }
    public Guid ChannelId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; }
    public DateTime? NextRetryAtUtc { get; init; }
    public DateTime? LastAttemptedAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TraceId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public record NotificationTestResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime TestedAtUtc { get; init; }
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

