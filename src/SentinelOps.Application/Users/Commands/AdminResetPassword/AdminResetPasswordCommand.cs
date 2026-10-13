using MediatR;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Application.Users.Commands.AdminResetPassword;

public record AdminResetPasswordCommand(
    string UserId,
    string NewPassword
) : IRequest<(bool Success, IEnumerable<string> Errors)>;

public class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, (bool Success, IEnumerable<string> Errors)>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AdminResetPasswordCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? "system";
        var (success, errors) = await _identityService.AdminResetPasswordAsync(request.UserId, request.NewPassword, currentUserId);

        await _auditService.RecordAsync(
            action: "User.PasswordResetByAdmin",
            targetEntityType: "User",
            targetEntityId: request.UserId,
            outcome: success ? "Success" : "Failure",
            details: success
                ? "Password was reset by administrator."
                : $"Failed to reset password: {string.Join("; ", errors)}",
            metadata: new { TargetUserId = request.UserId, ResetBy = currentUserId }, // Absolutely NO password here
            cancellationToken: cancellationToken);

        return (success, errors);
    }
}
