using MediatR;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using RefreshTokenEntity = SentinelOps.Domain.Entities.RefreshToken;

namespace SentinelOps.Application.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public LoginCommandHandler(
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

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (success, userId, userName, email, fullName, roles) = await _identityService.CheckPasswordSignInAsync(
            request.EmailOrUserName,
            request.Password);

        var ipAddress = _currentUserService.IpAddress ?? "unknown";

        if (!success)
        {
            _context.AuditEvents.Add(new AuditEvent
            {
                UserId = "anonymous",
                UserName = request.EmailOrUserName,
                Action = "UserLoginFailed",
                EntityType = "User",
                EntityId = request.EmailOrUserName,
                Details = "Invalid credentials provided",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var (accessToken, expiration) = await _tokenService.GenerateAccessTokenAsync(
            userId,
            userName,
            email,
            roles);

        var (rawRefreshToken, tokenHash, refreshExp) = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = refreshExp,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            UserName = userName,
            Action = "UserLoginSuccess",
            EntityType = "User",
            EntityId = userId,
            Details = "User successfully logged in",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            UserId = userId,
            Token = accessToken,
            RefreshToken = rawRefreshToken,
            Expiration = expiration,
            UserName = userName,
            Email = email,
            FullName = fullName,
            Roles = roles.ToList()
        };
    }
}
