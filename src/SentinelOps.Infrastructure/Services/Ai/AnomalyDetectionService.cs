using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Ai;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public const int MinSampleRequirement = 5;
    public const double DefaultSensitivityThreshold = 2.5; // Z-score or MAD threshold

    public AnomalyDetectionService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ILogger<AnomalyDetectionService> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<AnomalyDetectionResult> EvaluateSignalAsync(AnomalyEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId ?? request.OrganizationId;
        var samples = request.HistoricalValues ?? [];

        // 1. Guard against insufficient samples to avoid misleading statistical results
        if (samples.Count < MinSampleRequirement)
        {
            return new AnomalyDetectionResult
            {
                IsAnomaly = false,
                BaselineValue = samples.Count > 0 ? samples.Average() : request.CurrentValue,
                ObservedValue = request.CurrentValue,
                DeviationScore = 0.0,
                ConfidenceScore = 0.0,
                Severity = "Information",
                DetectionMethod = "InsufficientSamples",
                Explanation = $"Insufficient historical data ({samples.Count} samples, minimum {MinSampleRequirement} required) to calculate a statistically sound baseline for signal '{request.SignalType}'."
            };
        }

        // 2. Statistical Computations (Z-Score & Robust Median/MAD)
        var mean = samples.Average();
        var variance = samples.Select(x => Math.Pow(x - mean, 2)).Sum() / (samples.Count - 1);
        var stdDev = Math.Sqrt(variance);

        // Robust Median & MAD
        var sorted = samples.OrderBy(x => x).ToList();
        var median = GetMedian(sorted);
        var absoluteDeviations = samples.Select(x => Math.Abs(x - median)).OrderBy(x => x).ToList();
        var mad = GetMedian(absoluteDeviations);
        var normalizedMad = mad * 1.4826; // Consistency constant for normal distribution

        double zScore = 0.0;
        if (stdDev > 1e-6)
        {
            zScore = Math.Abs(request.CurrentValue - mean) / stdDev;
        }

        double madScore = 0.0;
        if (normalizedMad > 1e-6)
        {
            madScore = Math.Abs(request.CurrentValue - median) / normalizedMad;
        }

        // Ensemble decision: Primary score is the robust MAD score, cross-checked with Z-Score
        var deviationScore = normalizedMad > 1e-6 ? madScore : zScore;
        var baselineValue = normalizedMad > 1e-6 ? median : mean;

        bool isAnomaly = deviationScore >= DefaultSensitivityThreshold;

        // Confidence calibration based on sample size and method agreement
        double sampleConfidenceFactor = Math.Min(1.0, samples.Count / 30.0);
        double agreementFactor = (madScore >= DefaultSensitivityThreshold && zScore >= DefaultSensitivityThreshold) ? 1.0 : 0.85;
        double confidenceScore = isAnomaly ? Math.Round(Math.Clamp(0.65 + (sampleConfidenceFactor * 0.25) * agreementFactor, 0.50, 0.98), 2) : 0.0;

        string severity = "Information";
        if (isAnomaly)
        {
            if (deviationScore >= 5.0) severity = "Critical";
            else if (deviationScore >= 3.5) severity = "Error";
            else severity = "Warning";
        }

        var detectionMethod = normalizedMad > 1e-6 ? "RobustMedianMAD" : "RollingZScore";
        var explanation = GenerateExplanation(request.SignalType, request.ServiceName, request.CurrentValue, baselineValue, stdDev, deviationScore, detectionMethod, samples.Count, isAnomaly);

        var result = new AnomalyDetectionResult
        {
            IsAnomaly = isAnomaly,
            BaselineValue = Math.Round(baselineValue, 3),
            ObservedValue = Math.Round(request.CurrentValue, 3),
            DeviationScore = Math.Round(deviationScore, 2),
            ConfidenceScore = confidenceScore,
            Severity = severity,
            DetectionMethod = detectionMethod,
            Explanation = explanation
        };

        // 3. Persist and check suppression / deduplication
        if (isAnomaly && orgId != Guid.Empty && request.ApplicationId != Guid.Empty)
        {
            await PersistAnomalyIfCooldownClearedAsync(orgId, request, result, cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<TelemetryAnomalyDto>> GetActiveAnomaliesAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;

        var query = _context.TelemetryAnomalies.AsNoTracking();

        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            query = query.Where(a => a.OrganizationId == orgId.Value);
        }

        if (applicationId != Guid.Empty)
        {
            query = query.Where(a => a.ApplicationId == applicationId);
        }

        var entities = await query
            .Where(a => a.Status == "Active")
            .OrderByDescending(a => a.DetectionTimestamp)
            .Take(50)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<bool> ResolveAnomalyAsync(Guid anomalyId, string? resolvedBy, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;
        var query = _context.TelemetryAnomalies.Where(a => a.Id == anomalyId);

        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            query = query.Where(a => a.OrganizationId == orgId.Value);
        }

        var anomaly = await query.FirstOrDefaultAsync(cancellationToken);
        if (anomaly == null) return false;

        anomaly.Resolve(resolvedBy ?? "operator");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task PersistAnomalyIfCooldownClearedAsync(
        Guid orgId,
        AnomalyEvaluationRequest request,
        AnomalyDetectionResult result,
        CancellationToken cancellationToken)
    {
        var recentAnomaly = await _context.TelemetryAnomalies
            .Where(a => a.OrganizationId == orgId &&
                        a.ApplicationId == request.ApplicationId &&
                        a.ServiceName == request.ServiceName &&
                        a.SignalType == request.SignalType &&
                        a.Status == "Active")
            .OrderByDescending(a => a.DetectionTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        // Deduplication & Cooldown: if an active anomaly was created in the last 15 minutes, or is suppressed, skip duplicate insert
        if (recentAnomaly != null)
        {
            if (recentAnomaly.SuppressedUntilUtc.HasValue && DateTime.UtcNow < recentAnomaly.SuppressedUntilUtc.Value)
            {
                _logger.LogInformation("Anomaly suppressed for {Service}:{Signal} until {Time}", request.ServiceName, request.SignalType, recentAnomaly.SuppressedUntilUtc);
                return;
            }

            if (DateTime.UtcNow - recentAnomaly.DetectionTimestamp < TimeSpan.FromMinutes(15))
            {
                _logger.LogDebug("Anomaly duplicate suppressed within 15m cooldown window for {Service}:{Signal}", request.ServiceName, request.SignalType);
                return;
            }
        }

        var severityEnum = result.Severity switch
        {
            "Critical" => Severity.Critical,
            "Error" => Severity.Error,
            "Warning" => Severity.Warning,
            _ => Severity.Information
        };

        var entity = new TelemetryAnomaly(
            orgId,
            request.ApplicationId,
            request.ServiceName,
            request.SignalType,
            result.BaselineValue,
            result.ObservedValue,
            result.DeviationScore,
            result.ConfidenceScore,
            severityEnum,
            result.DetectionMethod,
            result.Explanation);

        _context.TelemetryAnomalies.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static double GetMedian(List<double> sorted)
    {
        if (sorted.Count == 0) return 0;
        int mid = sorted.Count / 2;
        return (sorted.Count % 2 != 0) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static string GenerateExplanation(
        string signalType,
        string serviceName,
        double observed,
        double baseline,
        double stdDev,
        double deviationScore,
        string method,
        int sampleCount,
        bool isAnomaly)
    {
        if (!isAnomaly)
        {
            return $"Observed {signalType} of {observed:F2} is within the expected baseline range (baseline: {baseline:F2} ±{stdDev:F2}) across {sampleCount} samples.";
        }

        var direction = observed > baseline ? "surged above" : "dropped below";
        return $"Anomaly detected in '{serviceName}': observed {signalType} of {observed:F2} {direction} expected baseline of {baseline:F2} (deviation score: {deviationScore:F2} via {method}, calibrated from {sampleCount} samples).";
    }

    private static TelemetryAnomalyDto MapToDto(TelemetryAnomaly a) => new()
    {
        Id = a.Id,
        OrganizationId = a.OrganizationId,
        ApplicationId = a.ApplicationId,
        ServiceName = a.ServiceName,
        SignalType = a.SignalType,
        DetectionTimestamp = a.DetectionTimestamp,
        BaselineValue = a.BaselineValue,
        ObservedValue = a.ObservedValue,
        DeviationScore = a.DeviationScore,
        ConfidenceScore = a.ConfidenceScore,
        Severity = a.Severity.ToString(),
        DetectionMethod = a.DetectionMethod,
        Explanation = a.Explanation,
        Status = a.Status,
        ResolvedAtUtc = a.ResolvedAtUtc
    };
}
