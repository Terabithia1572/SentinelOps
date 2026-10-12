using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Applications.Queries.GetApplications;

public record GetApplicationsQuery : IRequest<IReadOnlyList<ApplicationDto>>;
