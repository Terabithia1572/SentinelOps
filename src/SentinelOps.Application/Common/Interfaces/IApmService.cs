using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IApmService
{
    Task<ServiceMapDto> GetServiceMapAsync(string timeRange = "1h", string? environment = null, CancellationToken cancellationToken = default);
    Task<ServiceDependenciesDto> GetServiceDependenciesAsync(string serviceName, string timeRange = "1h", CancellationToken cancellationToken = default);
    Task<List<EndpointPerformanceDto>> GetEndpointsAsync(string timeRange = "1h", string? service = null, string? search = null, CancellationToken cancellationToken = default);
    Task<EndpointBreakdownDto?> GetEndpointBreakdownAsync(string endpointKey, string timeRange = "1h", CancellationToken cancellationToken = default);
    Task<List<ExternalDependencyDto>> GetExternalDependenciesAsync(string timeRange = "1h", CancellationToken cancellationToken = default);
}
