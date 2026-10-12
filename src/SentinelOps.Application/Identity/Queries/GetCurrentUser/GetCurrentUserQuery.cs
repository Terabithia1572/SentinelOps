using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Identity.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserProfileDto>;
