using MediatR;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Identity.Commands.Login;

public record LoginCommand(string EmailOrUserName, string Password) : IRequest<AuthResponse>;
