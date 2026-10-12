using MediatR;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Domain.Entities;
using RefreshTokenEntity = SentinelOps.Domain.Entities.RefreshToken;

namespace SentinelOps.Application.Identity.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var role = !string.IsNullOrWhiteSpace(request.Role) && Roles.All.Contains(request.Role)
            ? request.Role
            : Roles.Viewer;

        var (success, userId, errors) = await _identityService.CreateUserAsync(
            request.UserName,
            request.Email,
            request.FullName,
            request.Password,
            role);

        if (!success)
        {
            var errorMessage = string.Join("; ", errors);
            throw new InvalidOperationException($"Registration failed: {errorMessage}");
        }

        var rolesList = new List<string> { role };
        var (accessToken, expiration) = await _tokenService.GenerateAccessTokenAsync(
            userId,
            request.UserName,
            request.Email,
            rolesList);

        var (rawRefreshToken, tokenHash, refreshExp) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = refreshExp,
            CreatedByIp = _currentUserService.IpAddress ?? "unknown"
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            UserName = request.UserName,
            Action = "UserRegistered",
            EntityType = "User",
            EntityId = userId,
            Details = $"User registered with role {role}",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            UserId = userId,
            Token = accessToken,
            RefreshToken = rawRefreshToken,
            Expiration = expiration,
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            Roles = rolesList
        };
    }
}
