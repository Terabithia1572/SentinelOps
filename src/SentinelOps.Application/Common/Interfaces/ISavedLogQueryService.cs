using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface ISavedLogQueryService
{
    Task<List<SavedLogQueryDto>> GetSavedQueriesAsync(Guid? applicationId = null, CancellationToken ct = default);
    Task<SavedLogQueryDto?> GetSavedQueryByIdAsync(Guid id, CancellationToken ct = default);
    Task<SavedLogQueryDto> CreateSavedQueryAsync(CreateSavedLogQueryRequest request, CancellationToken ct = default);
    Task<SavedLogQueryDto?> UpdateSavedQueryAsync(Guid id, UpdateSavedLogQueryRequest request, CancellationToken ct = default);
    Task<bool> DeleteSavedQueryAsync(Guid id, CancellationToken ct = default);
    Task<(byte[] Content, string ContentType, string FileName)> ExportLogsAsync(ExportLogsRequest request, CancellationToken ct = default);
}
