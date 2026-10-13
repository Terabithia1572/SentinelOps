using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Audit.Queries.GetAuditSummary;

public record GetAuditSummaryQuery : IRequest<AuditSummaryDto>;

public class GetAuditSummaryQueryHandler : IRequestHandler<GetAuditSummaryQuery, AuditSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetAuditSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditSummaryDto> Handle(GetAuditSummaryQuery request, CancellationToken cancellationToken)
    {
        var cutoff24h = DateTime.UtcNow.AddHours(-24);

        var recentEvents = _context.AuditEvents.AsNoTracking().Where(a => a.Timestamp >= cutoff24h);

        var totalEvents24h = await recentEvents.CountAsync(cancellationToken);
        var failureCount24h = await recentEvents.CountAsync(a => a.Outcome.ToLower() == "failure", cancellationToken);

        var topActions = await recentEvents
            .GroupBy(a => a.Action)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopFacetCountDto { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var topActors = await recentEvents
            .Where(a => !string.IsNullOrEmpty(a.UserName))
            .GroupBy(a => a.UserName!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopFacetCountDto { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new AuditSummaryDto
        {
            TotalEvents24h = totalEvents24h,
            FailureCount24h = failureCount24h,
            TopActions = topActions,
            TopActors = topActors
        };
    }
}
