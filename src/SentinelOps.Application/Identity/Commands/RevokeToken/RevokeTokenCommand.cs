using MediatR;

namespace SentinelOps.Application.Identity.Commands.RevokeToken;

public record RevokeTokenCommand(string RefreshToken) : IRequest<bool>;
