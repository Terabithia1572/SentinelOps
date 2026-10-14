using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IPredictiveAlertingService
{
    Task<CapacityForecastResult> ForecastMetricAsync(CapacityForecastRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PredictiveForecastDto>> GetActiveForecastsAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
