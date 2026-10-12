using MediatR;

namespace SentinelOps.Application.Applications.Commands.DeleteEnvironment;

public record DeleteEnvironmentCommand(Guid ApplicationId, Guid EnvironmentId) : IRequest<bool>;
