using MediatR;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Identity.Commands.Register;

public record RegisterCommand(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string ConfirmPassword) : IRequest<AuthResponse>;
