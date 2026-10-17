using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Traces.Queries.GetTraceById;

public record GetTraceByIdQuery(string TraceId) : IRequest<TraceDetailDto?>;

public class GetTraceByIdQueryHandler : IRequestHandler<GetTraceByIdQuery, TraceDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetTraceByIdQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<TraceDetailDto?> Handle(GetTraceByIdQuery request, CancellationToken cancellationToken)
    {
        var trace = await _context.TraceRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TraceId == request.TraceId, cancellationToken);

        if (trace == null)
        {
            return null;
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == trace.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value, cancellationToken);
            if (!isOwned)
            {
                return null;
            }
        }

        var spans = await _context.SpanRecords
            .AsNoTracking()
            .Where(s => s.TraceId == request.TraceId)
            .OrderBy(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var logs = await _context.LogEntries
            .AsNoTracking()
            .Where(l => l.TraceId == request.TraceId)
            .OrderBy(l => l.Timestamp)
            .Select(l => l.ToDto())
            .ToListAsync(cancellationToken);

        var totalDurationMs = Math.Max(trace.Duration.TotalMilliseconds, 1.0);
        var traceStart = trace.StartedAtUtc;

        // Build parent-to-child map to calculate hierarchical depth
        var spanDict = spans.ToDictionary(s => s.SpanId, s => s);
        int CalculateDepth(string? parentId, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(parentId) || !spanDict.ContainsKey(parentId) || !visited.Add(parentId))
            {
                return 0;
            }
            return 1 + CalculateDepth(spanDict[parentId].ParentSpanId, visited);
        }

        var detailedSpans = new List<SpanDetailDto>(spans.Count);
        var maxDepth = 0;
        var hasMissingParents = false;

        var slowestSpanIds = spans
            .OrderByDescending(s => s.Duration.TotalMilliseconds)
            .Take(3)
            .Select(s => s.SpanId)
            .ToHashSet();

        foreach (var span in spans)
        {
            var isOrphan = !string.IsNullOrEmpty(span.ParentSpanId) && !spanDict.ContainsKey(span.ParentSpanId);
            if (isOrphan) hasMissingParents = true;

            var depth = CalculateDepth(span.ParentSpanId, new HashSet<string>());
            if (depth > maxDepth) maxDepth = depth;

            var offsetMs = Math.Max(0, (span.StartedAtUtc - traceStart).TotalMilliseconds);
            var durationMs = span.Duration.TotalMilliseconds;

            var offsetPercent = Math.Clamp((offsetMs / totalDurationMs) * 100.0, 0.0, 99.5);
            var durationPercent = Math.Clamp((durationMs / totalDurationMs) * 100.0, 0.5, 100.0 - offsetPercent);

            detailedSpans.Add(new SpanDetailDto
            {
                Id = span.Id,
                SpanId = span.SpanId,
                TraceId = span.TraceId,
                ParentSpanId = span.ParentSpanId,
                OperationName = span.OperationName,
                ServiceName = span.ServiceName,
                DurationMs = durationMs,
                Status = span.Status,
                StartedAtUtc = span.StartedAtUtc,
                Attributes = span.Attributes,
                Depth = depth,
                OffsetMs = offsetMs,
                OffsetPercent = Math.Round(offsetPercent, 2),
                DurationPercent = Math.Round(durationPercent, 2),
                IsOrphanedParent = isOrphan,
                IsSlowestSpan = slowestSpanIds.Contains(span.SpanId)
            });
        }

        var services = spans
            .Select(s => s.ServiceName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        return new TraceDetailDto
        {
            Trace = trace.ToDto(),
            Spans = detailedSpans,
            CorrelatedLogs = logs,
            Services = services,
            MaxDepth = maxDepth,
            TotalDurationMs = totalDurationMs,
            SlowestSpanIds = slowestSpanIds.ToList(),
            HasMissingParents = hasMissingParents
        };
    }
}
