using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplications;

public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetApplicationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = await _context.Applications
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return applications.Select(a => a.ToDto()).ToList();
    }
}
