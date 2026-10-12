using MediatR;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Applications.Commands.CreateEnvironment;

public record CreateEnvironmentCommand(
    Guid ApplicationId,
    string Name,
    EnvironmentType EnvironmentType) : IRequest<EnvironmentDto>;
