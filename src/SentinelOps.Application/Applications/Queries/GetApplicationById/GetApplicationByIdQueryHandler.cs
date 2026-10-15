using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplicationById;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetApplicationByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Environments)
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (application == null) return null;

        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(SentinelOps.Domain.Constants.Roles.Admin);

        if (application.OrganizationId.HasValue && !string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
            var appOrgId = application.OrganizationId.Value;
            var isMember = await _context.OrganizationMembers.AsNoTracking()
                .AnyAsync(m => m.OrganizationId == appOrgId && m.UserId == currentUserId, cancellationToken);

            if (!isMember)
            {
                return null; // IDOR defense: caller does not belong to the application's organization
            }
        }

        return application.ToDto();
    }
}
