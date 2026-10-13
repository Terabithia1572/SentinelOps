using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IStatusPageService
{
    Task<List<StatusPageDto>> GetStatusPagesAsync(CancellationToken ct = default);
    Task<StatusPageDto?> GetStatusPageByIdAsync(Guid id, CancellationToken ct = default);
    Task<StatusPageDto> CreateStatusPageAsync(CreateStatusPageRequest request, CancellationToken ct = default);
    Task<StatusPageDto?> UpdateStatusPageAsync(Guid id, UpdateStatusPageRequest request, CancellationToken ct = default);
    Task<bool> DeleteStatusPageAsync(Guid id, CancellationToken ct = default);

    Task<StatusPageIncidentDto?> PostIncidentAsync(Guid statusPageId, CreateStatusPageIncidentRequest request, string postedBy, CancellationToken ct = default);
    Task<StatusPageIncidentDto?> AddIncidentUpdateAsync(Guid statusPageId, Guid incidentId, AddStatusPageIncidentUpdateRequest request, string postedBy, CancellationToken ct = default);

    Task<PublicStatusPageDto?> GetPublicStatusPageBySlugAsync(string slug, CancellationToken ct = default);
}
