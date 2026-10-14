using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Traces.Queries.GetTraces;

public record GetTracesQuery(GetTracesRequest Request) : IRequest<PagedResponse<TraceDto>>;

public class GetTracesQueryHandler : IRequestHandler<GetTracesQuery, PagedResponse<TraceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetTracesQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<PagedResponse<TraceDto>> Handle(GetTracesQuery request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var query = _context.TraceRecords.AsNoTracking();

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (req.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == req.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return PagedResponse<TraceDto>.Create([], 0, req.Page, req.PageSize);
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

        if (req.HasErrors.HasValue)
        {
            query = query.Where(t => t.HasErrors == req.HasErrors.Value);
        }

        if (req.MinDurationMs.HasValue)
        {
            var minDuration = TimeSpan.FromMilliseconds(req.MinDurationMs.Value);
            query = query.Where(t => t.Duration >= minDuration);
        }

        if (req.MaxDurationMs.HasValue)
        {
            var maxDuration = TimeSpan.FromMilliseconds(req.MaxDurationMs.Value);
            query = query.Where(t => t.Duration <= maxDuration);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim();
            query = query.Where(t => t.RootSpanName.Contains(term) || t.TraceId.Contains(term) || t.ServiceName.Contains(term));
        }

        if (req.FromUtc.HasValue)
        {
            query = query.Where(t => t.StartedAtUtc >= req.FromUtc.Value);
        }

        if (req.ToUtc.HasValue)
        {
            query = query.Where(t => t.StartedAtUtc <= req.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = req.Page > 0 ? req.Page : 1;
        var pageSize = req.PageSize is > 0 and <= 100 ? req.PageSize : 25;

        var items = await query
            .OrderByDescending(t => t.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResponse<TraceDto>.Create(items, totalCount, page, pageSize);
    }
}
