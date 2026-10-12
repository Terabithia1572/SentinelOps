using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using RefreshTokenEntity = SentinelOps.Domain.Entities.RefreshToken;

namespace SentinelOps.Application.Identity.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly ITokenService _tokenService;
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IIdentityService identityService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _tokenService = tokenService;
        _identityService = identityService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        var ipAddress = _currentUserService.IpAddress ?? "unknown";

        if (existingToken != null && existingToken.RevokedAtUtc != null)
        {
            // Breach detection: A revoked token is being reused. Revoke all active tokens for this user.
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == existingToken.UserId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke(ipAddress, "Revoked due to token replay attack detection");
            }

            _context.AuditEvents.Add(new AuditEvent
            {
                UserId = existingToken.UserId,
                UserName = "System",
                Action = "RefreshTokenReuseBreachDetected",
                EntityType = "RefreshToken",
                EntityId = existingToken.Id.ToString(),
                Details = "A previously revoked refresh token was submitted. All active sessions invalidated.",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token reuse detected. All active sessions have been invalidated.");
        }

        if (existingToken == null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await _identityService.GetUserByIdAsync(existingToken.UserId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        // Generate new refresh token for rotation
        var (newRawRefreshToken, newTokenHash, refreshExp) = _tokenService.GenerateRefreshToken();

        // Revoke the old token and link to replacement
        existingToken.Revoke(ipAddress, newTokenHash);

        // Add the new replacement token
        var newRefreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAtUtc = refreshExp,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);

        // Generate new access token
        var (accessToken, expiration) = await _tokenService.GenerateAccessTokenAsync(
            user.Id,
            user.UserName,
            user.Email,
            user.Roles);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = user.Id,
            UserName = user.UserName,
            Action = "RefreshTokenRotated",
            EntityType = "RefreshToken",
            EntityId = existingToken.Id.ToString(),
            Details = "Refresh token rotated successfully",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            Token = accessToken,
            RefreshToken = newRawRefreshToken,
            Expiration = expiration,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            Roles = user.Roles
        };
    }
}
