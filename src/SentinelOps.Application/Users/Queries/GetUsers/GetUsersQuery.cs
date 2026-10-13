using MediatR;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Users.Queries.GetUsers;

public record GetUsersQuery(
    string? Search,
    string? Role,
    string? Status,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResponse<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<PagedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _identityService.GetUsersAsync(
            request.Search,
            request.Role,
            request.Status,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize);
    }
}
