using MediatR;

namespace SentinelOps.Application.Applications.Commands.DeleteApplication;

public record DeleteApplicationCommand(Guid Id) : IRequest<bool>;
