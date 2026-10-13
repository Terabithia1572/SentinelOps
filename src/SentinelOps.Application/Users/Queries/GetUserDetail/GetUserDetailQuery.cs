using MediatR;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Users.Queries.GetUserDetail;

public record GetUserDetailQuery(string UserId) : IRequest<UserDetailDto?>;

public class GetUserDetailQueryHandler : IRequestHandler<GetUserDetailQuery, UserDetailDto?>
{
    private readonly IIdentityService _identityService;

    public GetUserDetailQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDetailDto?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUserDetailAsync(request.UserId);
    }
}
