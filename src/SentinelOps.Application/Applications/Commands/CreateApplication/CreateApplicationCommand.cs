using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Commands.CreateApplication;

public record CreateApplicationCommand : IRequest<ApplicationDto>
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
