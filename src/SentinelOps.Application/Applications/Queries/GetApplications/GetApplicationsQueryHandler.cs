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
    private readonly ICurrentUserService _currentUserService;

    public GetApplicationsQueryHandler(
        IApplicationDbContext context,
        ICurrentTenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(SentinelOps.Domain.Constants.Roles.Admin);
        var isOperator = _currentUserService.IsInRole(SentinelOps.Domain.Constants.Roles.Operator);
        var query = _context.Applications.AsNoTracking();

        if (_tenantService.OrganizationId.HasValue && _tenantService.OrganizationId.Value != Guid.Empty)
        {
            var requestedOrgId = _tenantService.OrganizationId.Value;

            if (!string.IsNullOrEmpty(currentUserId) && !isAdmin)
            {
                var isMember = await _context.OrganizationMembers.AsNoTracking()
                    .AnyAsync(m => m.OrganizationId == requestedOrgId && m.UserId == currentUserId, cancellationToken);

                if (!isMember)
                {
                    // User does not belong to the requested organization -> isolate access
                    return [];
                }
            }

            query = query.Where(a => a.OrganizationId == requestedOrgId);
        }
        else if (!string.IsNullOrEmpty(currentUserId))
        {
            var userOrgIds = await _context.OrganizationMembers.AsNoTracking()
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.OrganizationId)
                .ToListAsync(cancellationToken);

            if (userOrgIds.Count > 0)
            {
                if (isAdmin || isOperator)
                {
                    query = query.Where(a => a.OrganizationId == null || userOrgIds.Contains(a.OrganizationId.Value));
                }
                else
                {
                    query = query.Where(a => a.OrganizationId.HasValue && userOrgIds.Contains(a.OrganizationId.Value));
                }
            }
            else if (!isAdmin && !isOperator)
            {
                return [];
            }
        }

        var applications = await query
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return applications.Select(a => a.ToDto()).ToList();
    }
}
