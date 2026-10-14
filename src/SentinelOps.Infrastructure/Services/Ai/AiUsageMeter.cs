using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Ai;

public class AiUsageMeter : IAiUsageMeter
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AiUsageMeter> _logger;

    public AiUsageMeter(IApplicationDbContext context, ILogger<AiUsageMeter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CanExecuteAiOperationAsync(Guid organizationId, int estimatedTokens = 1000, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty) return true;

        var settings = await _context.OrganizationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

        if (settings != null && !settings.IsAiEnabled)
        {
            _logger.LogInformation("AI execution blocked: AI disabled for organization {OrganizationId}", organizationId);
            return false;
        }

        var usage = await _context.OrganizationUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId, cancellationToken);

        if (usage != null && settings != null)
        {
            if (usage.AiTokensUsedMonthToDate + estimatedTokens > settings.MaxMonthlyAiTokens)
            {
                _logger.LogWarning("AI token quota exceeded for organization {OrganizationId}: Used {Used}, Max {Max}",
                    organizationId, usage.AiTokensUsedMonthToDate, settings.MaxMonthlyAiTokens);
                return false;
            }
        }

        return true;
    }

    public async Task RecordUsageAsync(Guid organizationId, int tokensUsed, double estimatedCostUsd = 0.0, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || tokensUsed <= 0) return;

        var usage = await _context.OrganizationUsages
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId, cancellationToken);

        if (usage != null)
        {
            usage.AiTokensUsedMonthToDate += tokensUsed;
            usage.AiInvocationsMonthToDate += 1;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Recorded {Tokens} AI tokens for organization {OrganizationId}. Total month-to-date: {Total}",
                tokensUsed, organizationId, usage.AiTokensUsedMonthToDate);
        }
    }

    public async Task<AiUsageDto> GetUsageAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var settings = await _context.OrganizationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

        var usage = await _context.OrganizationUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId, cancellationToken);

        var maxTokens = settings?.MaxMonthlyAiTokens ?? 100_000;
        var usedTokens = usage?.AiTokensUsedMonthToDate ?? 0;
        var invocations = usage?.AiInvocationsMonthToDate ?? 0;
        var remaining = Math.Max(0, maxTokens - usedTokens);
        var isExceeded = usedTokens >= maxTokens;

        // Estimated cost metric (e.g. $0.002 per 1k tokens)
        var estimatedCost = (usedTokens / 1000.0) * 0.002;

        return new AiUsageDto
        {
            OrganizationId = organizationId,
            TokensUsedMonthToDate = usedTokens,
            InvocationsMonthToDate = invocations,
            MonthlyTokenBudget = maxTokens,
            RemainingTokens = remaining,
            IsQuotaExceeded = isExceeded,
            EstimatedCostUsd = Math.Round(estimatedCost, 4)
        };
    }
}
