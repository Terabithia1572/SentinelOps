using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Users.Queries.GetApplicationMembers;

public record GetApplicationMembersQuery(Guid ApplicationId) : IRequest<List<ApplicationMemberDto>>;

public class GetApplicationMembersQueryHandler : IRequestHandler<GetApplicationMembersQuery, List<ApplicationMemberDto>>
{
    private readonly IApplicationDbContext _context;

    public GetApplicationMembersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ApplicationMemberDto>> Handle(GetApplicationMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _context.ApplicationMembers
            .AsNoTracking()
            .Include(m => m.Application)
            .Where(m => m.ApplicationId == request.ApplicationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new ApplicationMemberDto
            {
                Id = m.Id,
                ApplicationId = m.ApplicationId,
                ApplicationName = m.Application.DisplayName.Length > 0 ? m.Application.DisplayName : m.Application.Name,
                UserId = m.UserId,
                UserName = m.UserId,
                Role = m.Role,
                CreatedAtUtc = m.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return members;
    }
}
