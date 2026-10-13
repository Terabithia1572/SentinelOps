namespace SentinelOps.Contracts.Requests;

public record GetExceptionsRequest
{
    public Guid? ApplicationId { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Limit { get; init; } = 30;
}
