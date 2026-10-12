using MediatR;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Identity.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
