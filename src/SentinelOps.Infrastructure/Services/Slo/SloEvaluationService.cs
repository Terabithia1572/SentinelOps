using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Slo;

public class SloEvaluationService : ISloEvaluationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SloEvaluationService> _logger;

    public SloEvaluationService(
        IApplicationDbContext context,
        ILogger<SloEvaluationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> EvaluateAllPendingSlosAsync(CancellationToken cancellationToken = default)
    {
        var slos = await _context.ServiceLevelObjectives
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        int evaluated = 0;
        var utcNow = DateTime.UtcNow;

        foreach (var slo in slos)
        {
            try
            {
                await EvaluateSloInternalAsync(slo, utcNow, cancellationToken);
                evaluated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate SLO {SloId} ({SloName})", slo.Id, slo.Name);
            }
        }

        if (evaluated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return evaluated;
    }

    public async Task<SloEvaluationResult> EvaluateSloAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        var slo = await _context.ServiceLevelObjectives.FindAsync([sloId], cancellationToken);
        if (slo == null)
        {
            return new SloEvaluationResult(sloId, false, 0.0, 0.0, 0.0, false, "SLO not found.");
        }

        var utcNow = DateTime.UtcNow;
        var eval = await EvaluateSloInternalAsync(slo, utcNow, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new SloEvaluationResult(
            slo.Id,
            true,
            eval.CompliancePercentage,
            eval.ErrorBudgetRemaining,
            eval.BurnRate,
            eval.IsBreached,
            $"Evaluated {slo.Name}: Compliance {eval.CompliancePercentage:F2}%, Error Budget {eval.ErrorBudgetRemaining:F2}%."
        );
    }

    private async Task<SloEvaluation> EvaluateSloInternalAsync(
        ServiceLevelObjective slo,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var fromUtc = slo.EvaluationWindow switch
        {
            SloEvaluationWindow.Rolling24Hours => utcNow.AddHours(-24),
            SloEvaluationWindow.Rolling7Days => utcNow.AddDays(-7),
            SloEvaluationWindow.Rolling30Days => utcNow.AddDays(-30),
            _ => utcNow.AddDays(-7)
        };

        var query = _context.TraceRecords
            .Where(t => t.ApplicationId == slo.ApplicationId &&
                        t.ServiceName == slo.ServiceName &&
                        t.StartedAtUtc >= fromUtc &&
                        t.StartedAtUtc <= utcNow);

        if (!string.IsNullOrWhiteSpace(slo.EnvironmentName))
        {
            query = query.Where(t => t.EnvironmentName == slo.EnvironmentName);
        }

        long totalRequests = await query.CountAsync(cancellationToken);
        long goodRequests;

        if (slo.IndicatorType == SloIndicatorType.Availability)
        {
            goodRequests = await query.CountAsync(t => !t.HasErrors, cancellationToken);
        }
        else // Latency SLO
        {
            double maxDurationMs = slo.LatencyThresholdMs ?? 500.0;
            var maxTimeSpan = TimeSpan.FromMilliseconds(maxDurationMs);
            goodRequests = await query.CountAsync(t => t.Duration <= maxTimeSpan && !t.HasErrors, cancellationToken);
        }

        var evaluation = slo.RecordEvaluation(totalRequests, goodRequests, utcNow);
        _context.SloEvaluations.Add(evaluation);

        return evaluation;
    }
}
