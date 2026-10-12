using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Commands.UpdateApplication;

public record UpdateApplicationCommand(
    Guid Id,
    string DisplayName,
    string Description,
    bool IsActive) : IRequest<ApplicationDto>;
