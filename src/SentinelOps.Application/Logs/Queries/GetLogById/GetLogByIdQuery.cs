using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Logs.Queries.GetLogById;

public record GetLogByIdQuery(Guid Id) : IRequest<LogEntryDto?>;

public class GetLogByIdQueryHandler : IRequestHandler<GetLogByIdQuery, LogEntryDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetLogByIdQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<LogEntryDto?> Handle(GetLogByIdQuery request, CancellationToken cancellationToken)
    {
        var log = await _context.LogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (log == null)
        {
            return null;
        }

        if (_tenantService.OrganizationId.HasValue)
        {
            var isOwned = await _context.Applications
                .AnyAsync(a => a.Id == log.ApplicationId && a.OrganizationId == _tenantService.OrganizationId.Value, cancellationToken);
            if (!isOwned)
            {
                return null;
            }
        }

        return log.ToDto();
    }
}
