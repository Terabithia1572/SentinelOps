namespace SentinelOps.Contracts.Requests;

public class GetAuditEventsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public string? Actor { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public string? Outcome { get; set; } // "Success", "Failure", "Warning"
    public string? ApplicationScope { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
