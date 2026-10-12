using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplicationById;

public record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto?>;
