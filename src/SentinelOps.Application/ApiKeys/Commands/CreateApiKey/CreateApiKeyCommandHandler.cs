using MediatR;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.ApiKeys.Commands.CreateApiKey;

public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, ApiKeyCreatedResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IApiKeyService _apiKeyService;
    private readonly ICurrentUserService _currentUserService;

    public CreateApiKeyCommandHandler(
        IApplicationDbContext context,
        IApiKeyService apiKeyService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiKeyCreatedResponse> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        var (rawApiKey, keyPrefix, keyHash) = _apiKeyService.GenerateApiKey();

        DateTime? expiresAt = request.ExpiresInDays.HasValue
            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
            : null;

        var apiKey = new ApiKey
        {
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            ExpiresAtUtc = expiresAt,
            CreatedBy = _currentUserService.UserName ?? "system"
        };

        _context.ApiKeys.Add(apiKey);

        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = _currentUserService.UserId ?? "anonymous",
            UserName = _currentUserService.UserName ?? "system",
            Action = "ApiKeyCreated",
            EntityType = "ApiKey",
            EntityId = apiKey.Id.ToString(),
            Details = $"API Key '{apiKey.Name}' (prefix: {keyPrefix}) created for application '{application.Name}'",
            IpAddress = _currentUserService.IpAddress ?? "unknown",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResponse
        {
            Id = apiKey.Id,
            ApplicationId = apiKey.ApplicationId,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            RawApiKey = rawApiKey,
            CreatedAtUtc = apiKey.CreatedAtUtc,
            ExpiresAtUtc = apiKey.ExpiresAtUtc
        };
    }
}
