using MediatR;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
