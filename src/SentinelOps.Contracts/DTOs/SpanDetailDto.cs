namespace SentinelOps.Contracts.DTOs;

public record SpanDetailDto
{
    public Guid Id { get; init; }
    public string SpanId { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public string? ParentSpanId { get; init; }
    public string OperationName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public string Status { get; init; } = "Ok";
    public DateTime StartedAtUtc { get; init; }
    public string Attributes { get; init; } = "{}";
    public int Depth { get; init; }
    public double OffsetMs { get; init; }
    public double OffsetPercent { get; init; }
    public double DurationPercent { get; init; }
    public bool IsOrphanedParent { get; init; }
    public bool IsSlowestSpan { get; init; }
    public bool HasError => !string.Equals(Status, "Ok", StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(Status, "Unset", StringComparison.OrdinalIgnoreCase);
}
