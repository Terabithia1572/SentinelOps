using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.ApiKeys.Commands.RevokeApiKey;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RevokeApiKeyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.KeyId, cancellationToken);

        if (apiKey == null || !apiKey.IsActive)
        {
            return false;
        }

        apiKey.Revoke();

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ApiKeyRevoked",
            EntityType = "ApiKey",
            EntityId = apiKey.Id.ToString(),
            Details = $"API Key '{apiKey.Name}' (prefix: {apiKey.KeyPrefix}) revoked",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
