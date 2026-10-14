using MediatR;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Application.Logs.Queries.GetLogs;

public record GetLogsQuery(GetLogsRequest Request) : IRequest<PagedResponse<LogEntryDto>>;
