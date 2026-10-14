using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Ai;

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AiOptions> _options;

    public AiProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<AiOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    public IAiProvider GetProvider(string? providerId = null)
    {
        var target = providerId ?? _options.Value.DefaultProvider;

        if (string.Equals(target, "Disabled", StringComparison.OrdinalIgnoreCase) || !_options.Value.Enabled)
        {
            return _serviceProvider.GetRequiredService<DisabledAiProvider>();
        }

        if (string.Equals(target, "ExternalLlm", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<ExternalLlmProvider>();
        }

        return _serviceProvider.GetRequiredService<DeterministicAiProvider>();
    }

    public async Task<IAiProvider> GetProviderForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            return _serviceProvider.GetRequiredService<DisabledAiProvider>();
        }

        if (organizationId == Guid.Empty)
        {
            return GetProvider();
        }

        var dbContext = _serviceProvider.GetRequiredService<IApplicationDbContext>();
        var settings = await dbContext.OrganizationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

        if (settings != null)
        {
            if (!settings.IsAiEnabled)
            {
                return _serviceProvider.GetRequiredService<DisabledAiProvider>();
            }

            return GetProvider(settings.PreferredAiProvider);
        }

        return GetProvider();
    }
}
