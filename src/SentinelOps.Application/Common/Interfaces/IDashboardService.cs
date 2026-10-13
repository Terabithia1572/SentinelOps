using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<List<DashboardSummaryItemDto>> GetDashboardsAsync(CancellationToken cancellationToken = default);
    Task<DashboardDetailDto?> GetDashboardByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DashboardDetailDto> CreateDashboardAsync(CreateDashboardRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDetailDto?> UpdateDashboardAsync(Guid id, UpdateDashboardRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDashboardAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DashboardDetailDto?> DuplicateDashboardAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DashboardDetailDto> ImportDashboardAsync(ImportDashboardRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDetailDto?> InstantiateTemplateAsync(string templateKey, CancellationToken cancellationToken = default);
}
