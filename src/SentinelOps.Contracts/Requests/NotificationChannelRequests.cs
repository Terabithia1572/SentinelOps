namespace SentinelOps.Contracts.Requests;

public record CreateNotificationChannelRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "Email";
    public string Destination { get; init; } = string.Empty;
    public string? WebhookSecret { get; init; }
    public string? CustomHeadersJson { get; init; }
    public Guid? ApplicationId { get; init; }
}

public record UpdateNotificationChannelRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public string? WebhookSecret { get; init; }
    public string? CustomHeadersJson { get; init; }
    public bool IsEnabled { get; init; } = true;
}
