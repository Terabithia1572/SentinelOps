using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.ApiKeys.Queries.GetApiKeys;

public record GetApiKeysQuery(Guid ApplicationId) : IRequest<IReadOnlyList<ApiKeyDto>>;
