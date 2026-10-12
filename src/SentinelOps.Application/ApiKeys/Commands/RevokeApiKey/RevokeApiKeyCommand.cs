using MediatR;

namespace SentinelOps.Application.ApiKeys.Commands.RevokeApiKey;

public record RevokeApiKeyCommand(Guid KeyId) : IRequest<bool>;
