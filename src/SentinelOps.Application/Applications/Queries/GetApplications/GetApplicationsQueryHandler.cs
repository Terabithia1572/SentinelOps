using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplications;

public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public GetApplicationsQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Applications.AsNoTracking();

        if (_tenantService.OrganizationId.HasValue)
        {
            var orgId = _tenantService.OrganizationId.Value;
            query = query.Where(a => a.OrganizationId == orgId);
        }

        var applications = await query
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return applications.Select(a => a.ToDto()).ToList();
    }
}
