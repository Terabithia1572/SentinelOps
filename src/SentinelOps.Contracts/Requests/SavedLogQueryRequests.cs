namespace SentinelOps.Contracts.Requests;

public record CreateSavedLogQueryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ApplicationId { get; init; }
    public string? SearchTerm { get; init; }
    public string? Severity { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public string? TraceId { get; init; }
    public string? PropertyFiltersJson { get; init; }
    public string TimeRange { get; init; } = "1h";
    public bool IsShared { get; init; } = true;
    public List<string>? Tags { get; init; }
}

public record UpdateSavedLogQueryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SearchTerm { get; init; }
    public string? Severity { get; init; }
    public string? ServiceName { get; init; }
    public string? EnvironmentName { get; init; }
    public string? TraceId { get; init; }
    public string? PropertyFiltersJson { get; init; }
    public string TimeRange { get; init; } = "1h";
    public bool IsShared { get; init; } = true;
    public List<string>? Tags { get; init; }
}

public record ExportLogsRequest : GetLogsRequest
{
    public string Format { get; init; } = "json"; // "json" or "csv"
    public int MaxRecords { get; init; } = 1000;
}
