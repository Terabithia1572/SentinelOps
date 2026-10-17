namespace SentinelOps.Contracts.Requests;

public record GetLogsRequest
{
    public Guid? ApplicationId { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public string? Severity { get; init; }
    public string? TraceId { get; init; }
    public string? Search { get; init; }
    public string? PropertyFilter { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string SortBy { get; init; } = "Timestamp";
    public string SortDirection { get; init; } = "desc";
}
