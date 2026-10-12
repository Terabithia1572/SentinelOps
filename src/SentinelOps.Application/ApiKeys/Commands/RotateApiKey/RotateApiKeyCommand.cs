using MediatR;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.ApiKeys.Commands.RotateApiKey;

public record RotateApiKeyCommand(Guid KeyId, int? ExpiresInDays = null) : IRequest<ApiKeyCreatedResponse>;
