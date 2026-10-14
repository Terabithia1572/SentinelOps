using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Metrics.Queries.GetMetricsTimeSeries;

public record GetMetricsTimeSeriesQuery(GetMetricsRequest Request) : IRequest<MetricSeriesResponse>;

public class GetMetricsTimeSeriesQueryHandler : IRequestHandler<GetMetricsTimeSeriesQuery, MetricSeriesResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetMetricsTimeSeriesQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<MetricSeriesResponse> Handle(GetMetricsTimeSeriesQuery request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var toUtc = req.ToUtc ?? DateTime.UtcNow;
        var fromUtc = req.FromUtc ?? toUtc.AddHours(-1);
        var bucketMinutes = Math.Clamp(req.BucketMinutes > 0 ? req.BucketMinutes : 5, 1, 1440);

        var query = _context.TraceRecords
            .AsNoTracking()
            .Where(t => t.StartedAtUtc >= fromUtc && t.StartedAtUtc <= toUtc);

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (req.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == req.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return new MetricSeriesResponse
                    {
                        Points = [],
                        AvgLatencyMs = 0,
                        P95LatencyMs = 0,
                        OverallErrorRate = 0,
                        TotalThroughputRps = 0,
                        TotalRequests = 0
                    };
                }
                query = query.Where(t => t.ApplicationId == req.ApplicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(t => tenantAppIds.Contains(t.ApplicationId));
            }
        }
        else if (req.ApplicationId.HasValue)
        {
            query = query.Where(t => t.ApplicationId == req.ApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
        {
            query = query.Where(t => t.ServiceName == req.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(req.EnvironmentName))
        {
            query = query.Where(t => t.EnvironmentName == req.EnvironmentName);
        }

        // Project only the required columns for aggregate calculation to avoid memory bloat
        var traceSamples = await query
            .Select(t => new
            {
                t.StartedAtUtc,
                DurationMs = t.Duration.TotalMilliseconds,
                t.HasErrors
            })
            .ToListAsync(cancellationToken);

        if (traceSamples.Count == 0)
        {
            return new MetricSeriesResponse
            {
                Points = [],
                AvgLatencyMs = 0,
                P95LatencyMs = 0,
                OverallErrorRate = 0,
                TotalThroughputRps = 0,
                TotalRequests = 0
            };
        }

        // Group into time buckets
        var bucketDuration = TimeSpan.FromMinutes(bucketMinutes);
        var grouped = traceSamples
            .GroupBy(t =>
            {
                var diff = t.StartedAtUtc.Ticks - fromUtc.Ticks;
                var bucketIndex = Math.Max(0, diff / bucketDuration.Ticks);
                return new DateTime(fromUtc.Ticks + bucketIndex * bucketDuration.Ticks, DateTimeKind.Utc);
            })
            .OrderBy(g => g.Key)
            .ToList();

        var points = new List<MetricTimeSeriesPointDto>(grouped.Count);

        foreach (var group in grouped)
        {
            var bucketStart = group.Key;
            var tracesInBucket = group.ToList();
            var totalCount = tracesInBucket.Count;
            var errorCount = tracesInBucket.Count(t => t.HasErrors);
            var errorRate = totalCount > 0 ? Math.Round((errorCount / (double)totalCount) * 100.0, 2) : 0.0;

            var bucketSeconds = bucketDuration.TotalSeconds;
            var rps = bucketSeconds > 0 ? Math.Round(totalCount / bucketSeconds, 2) : 0.0;

            var durations = tracesInBucket.Select(t => t.DurationMs).OrderBy(d => d).ToList();
            var avgDuration = Math.Round(durations.Average(), 2);
            var p50 = Math.Round(GetPercentile(durations, 0.50), 2);
            var p95 = Math.Round(GetPercentile(durations, 0.95), 2);
            var p99 = Math.Round(GetPercentile(durations, 0.99), 2);

            points.Add(new MetricTimeSeriesPointDto
            {
                Timestamp = bucketStart,
                RequestsPerSecond = rps,
                ErrorRatePercent = errorRate,
                AvgDurationMs = avgDuration,
                P50DurationMs = p50,
                P95DurationMs = p95,
                P99DurationMs = p99,
                TotalRequests = totalCount,
                ErrorRequests = errorCount
            });
        }

        var allDurations = traceSamples.Select(t => t.DurationMs).OrderBy(d => d).ToList();
        var totalReqs = traceSamples.Count;
        var totalErrs = traceSamples.Count(t => t.HasErrors);
        var totalWindowSeconds = Math.Max(1.0, (toUtc - fromUtc).TotalSeconds);

        return new MetricSeriesResponse
        {
            Points = points,
            AvgLatencyMs = Math.Round(allDurations.Average(), 2),
            P95LatencyMs = Math.Round(GetPercentile(allDurations, 0.95), 2),
            OverallErrorRate = Math.Round((totalErrs / (double)totalReqs) * 100.0, 2),
            TotalThroughputRps = Math.Round(totalReqs / totalWindowSeconds, 2),
            TotalRequests = totalReqs
        };
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }
}
