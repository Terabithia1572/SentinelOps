namespace SentinelOps.Contracts.Requests;

public record GetTracesRequest
{
    public Guid? ApplicationId { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public bool? HasErrors { get; init; }
    public double? MinDurationMs { get; init; }
    public double? MaxDurationMs { get; init; }
    public string? Search { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
