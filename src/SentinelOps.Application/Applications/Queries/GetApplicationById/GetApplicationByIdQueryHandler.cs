using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplicationById;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDto?>
{
    private readonly IApplicationDbContext _context;

    public GetApplicationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Environments)
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        return application?.ToDto();
    }
}
