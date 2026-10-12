using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.ApiKeys.Commands.RotateApiKey;

public class RotateApiKeyCommandHandler : IRequestHandler<RotateApiKeyCommand, ApiKeyCreatedResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IApiKeyService _apiKeyService;
    private readonly ICurrentUserService _currentUserService;

    public RotateApiKeyCommandHandler(
        IApplicationDbContext context,
        IApiKeyService apiKeyService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiKeyCreatedResponse> Handle(RotateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var existingKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.KeyId, cancellationToken);

        if (existingKey == null || !existingKey.IsActive)
        {
            throw new KeyNotFoundException($"Active API Key with ID '{request.KeyId}' was not found.");
        }

        // Revoke the old key
        existingKey.Revoke();

        // Generate the new key
        var (rawApiKey, keyPrefix, keyHash) = _apiKeyService.GenerateApiKey();

        DateTime? expiresAt = request.ExpiresInDays.HasValue
            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
            : null;

        var newApiKey = new ApiKey
        {
            ApplicationId = existingKey.ApplicationId,
            Name = existingKey.Name,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            ExpiresAtUtc = expiresAt,
            CreatedBy = _currentUserService.UserName ?? "system"
        };

        _context.ApiKeys.Add(newApiKey);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ApiKeyRotated",
            EntityType = "ApiKey",
            EntityId = newApiKey.Id.ToString(),
            Details = $"API Key rotated: OldPrefix={existingKey.KeyPrefix}, NewPrefix={keyPrefix}",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResponse
        {
            Id = newApiKey.Id,
            ApplicationId = newApiKey.ApplicationId,
            Name = newApiKey.Name,
            KeyPrefix = newApiKey.KeyPrefix,
            RawApiKey = rawApiKey,
            CreatedAtUtc = newApiKey.CreatedAtUtc,
            ExpiresAtUtc = newApiKey.ExpiresAtUtc
        };
    }
}
