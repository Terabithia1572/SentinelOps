using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Commands.CreateService;

public record CreateServiceCommand(
    Guid ApplicationId,
    string Name,
    string DisplayName) : IRequest<ServiceDto>;
