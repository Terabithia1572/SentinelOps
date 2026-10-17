using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Logs.Queries.GetLogs;

public class GetLogsQueryHandler : IRequestHandler<GetLogsQuery, PagedResponse<LogEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetLogsQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<PagedResponse<LogEntryDto>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var query = _context.LogEntries.AsNoTracking();

        if (_tenantService.OrganizationId.HasValue)
        {
            var tenantOrgId = _tenantService.OrganizationId.Value;
            if (req.ApplicationId.HasValue)
            {
                var isAppInTenant = await _context.Applications
                    .AnyAsync(a => a.Id == req.ApplicationId.Value && a.OrganizationId == tenantOrgId, cancellationToken);
                if (!isAppInTenant)
                {
                    return PagedResponse<LogEntryDto>.Create(new List<LogEntryDto>(), 0, req.Page, req.PageSize);
                }
                query = query.Where(l => l.ApplicationId == req.ApplicationId.Value);
            }
            else
            {
                var tenantAppIds = _context.Applications
                    .Where(a => a.OrganizationId == tenantOrgId)
                    .Select(a => a.Id);
                query = query.Where(l => tenantAppIds.Contains(l.ApplicationId));
            }
        }
        else if (req.ApplicationId.HasValue)
        {
            query = query.Where(l => l.ApplicationId == req.ApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
        {
            query = query.Where(l => l.ServiceName == req.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(req.EnvironmentName))
        {
            query = query.Where(l => l.EnvironmentName == req.EnvironmentName);
        }

        if (!string.IsNullOrWhiteSpace(req.Severity) && Enum.TryParse<Severity>(req.Severity, true, out var parsedSeverity))
        {
            query = query.Where(l => l.Severity == parsedSeverity);
        }

        if (!string.IsNullOrWhiteSpace(req.TraceId))
        {
            query = query.Where(l => l.TraceId == req.TraceId);
        }

        if (req.FromUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp >= req.FromUtc.Value);
        }

        if (req.ToUtc.HasValue)
        {
            query = query.Where(l => l.Timestamp <= req.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim();
            query = query.Where(l => l.Message.Contains(term) ||
                                     l.MessageTemplate.Contains(term) ||
                                     (l.ExceptionDetail != null && l.ExceptionDetail.Contains(term)) ||
                                     l.Properties.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(req.PropertyFilter))
        {
            var filter = req.PropertyFilter.Trim();
            if (filter.Contains('='))
            {
                var parts = filter.Split('=', 2);
                var key = parts[0].Trim();
                var val = parts[1].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                {
                    query = query.Where(l => l.Properties.Contains(key) && l.Properties.Contains(val));
                }
            }
            else
            {
                query = query.Where(l => l.Properties.Contains(filter));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = req.SortDirection?.ToLowerInvariant() == "asc"
            ? query.OrderBy(l => l.Timestamp)
            : query.OrderByDescending(l => l.Timestamp);

        var page = req.Page > 0 ? req.Page : 1;
        var pageSize = req.PageSize is > 0 and <= 200 ? req.PageSize : 50;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => l.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResponse<LogEntryDto>.Create(items, totalCount, page, pageSize);
    }
}
