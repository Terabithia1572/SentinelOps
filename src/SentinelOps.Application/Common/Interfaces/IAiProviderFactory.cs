namespace SentinelOps.Application.Common.Interfaces;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string? providerId = null);
    Task<IAiProvider> GetProviderForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
