using MediatR;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.ApiKeys.Commands.CreateApiKey;

public record CreateApiKeyCommand(
    Guid ApplicationId,
    string Name,
    int? ExpiresInDays = null) : IRequest<ApiKeyCreatedResponse>;
