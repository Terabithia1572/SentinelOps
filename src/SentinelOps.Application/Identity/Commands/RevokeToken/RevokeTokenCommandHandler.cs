using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Identity.Commands.RevokeToken;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RevokeTokenCommandHandler(
        ITokenService tokenService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _tokenService = tokenService;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return false;
        }

        var ipAddress = _currentUserService.IpAddress ?? "unknown";
        existingToken.Revoke(ipAddress);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = existingToken.UserId,
            UserName = _currentUserService.UserName ?? "system",
            Action = "RefreshTokenRevoked",
            EntityType = "RefreshToken",
            EntityId = existingToken.Id.ToString(),
            Details = "Refresh token explicitly revoked",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
