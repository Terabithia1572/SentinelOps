namespace SentinelOps.Application.Common.Interfaces;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        string targetEntityType,
        string targetEntityId,
        string outcome = "Success",
        string? details = null,
        string? applicationScope = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
