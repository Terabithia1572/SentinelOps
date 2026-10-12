using MediatR;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Identity.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var profile = await _identityService.GetUserByIdAsync(userId);
        if (profile == null)
        {
            throw new KeyNotFoundException("User profile not found.");
        }

        return profile;
    }
}
