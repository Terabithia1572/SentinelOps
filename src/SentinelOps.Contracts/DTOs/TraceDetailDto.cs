namespace SentinelOps.Contracts.DTOs;

public record TraceDetailDto
{
    public TraceDto Trace { get; init; } = default!;
    public IReadOnlyList<SpanDetailDto> Spans { get; init; } = [];
    public IReadOnlyList<LogEntryDto> CorrelatedLogs { get; init; } = [];
    public IReadOnlyList<string> Services { get; init; } = [];
    public int MaxDepth { get; init; }
    public double TotalDurationMs { get; init; }
}
