using MediatR;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Users.Commands.UpdateUserRole;

public record UpdateUserRoleCommand(string UserId, List<string> Roles) : IRequest<(bool Success, IEnumerable<string> Errors)>;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, (bool Success, IEnumerable<string> Errors)>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public UpdateUserRoleCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? "system";
        var (success, errors) = await _identityService.UpdateUserRolesAsync(request.UserId, request.Roles, currentUserId);

        await _auditService.RecordAsync(
            action: "User.RoleUpdated",
            targetEntityType: "User",
            targetEntityId: request.UserId,
            outcome: success ? "Success" : "Failure",
            details: success
                ? $"Roles updated to: {string.Join(", ", request.Roles)}"
                : $"Failed to update roles: {string.Join("; ", errors)}",
            metadata: new { TargetUserId = request.UserId, NewRoles = request.Roles, Success = success },
            cancellationToken: cancellationToken);

        return (success, errors);
    }
}
