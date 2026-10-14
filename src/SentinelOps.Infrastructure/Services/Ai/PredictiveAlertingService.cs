using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Services.Ai;

public class PredictiveAlertingService : IPredictiveAlertingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<PredictiveAlertingService> _logger;

    public const int MinSampleRequirement = 5;

    public PredictiveAlertingService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ILogger<PredictiveAlertingService> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<CapacityForecastResult> ForecastMetricAsync(CapacityForecastRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
        {
            orgId = await _context.Applications
                .Where(a => a.Id == request.ApplicationId)
                .Select(a => a.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken) ?? request.OrganizationId;
        }

        // Query historical metric data points
        var cutoff = DateTime.UtcNow.AddHours(-6);
        var metricRecords = await _context.MetricRecords
            .AsNoTracking()
            .Where(m => m.ApplicationId == request.ApplicationId &&
                        (string.IsNullOrEmpty(request.ServiceName) || m.ServiceName == request.ServiceName) &&
                        m.MetricName == request.MetricName &&
                        m.Timestamp >= cutoff)
            .OrderBy(m => m.Timestamp)
            .Take(500)
            .ToListAsync(cancellationToken);

        // Fallback to synthetic points if database has no live time-series
        var dataPoints = metricRecords.Select(m => (Time: m.Timestamp, Value: m.Value)).ToList();

        if (dataPoints.Count < MinSampleRequirement)
        {
            return new CapacityForecastResult
            {
                ApplicationId = request.ApplicationId,
                ServiceName = request.ServiceName,
                MetricName = request.MetricName,
                CurrentValue = dataPoints.Count > 0 ? dataPoints[^1].Value : 0.0,
                PredictedValue = dataPoints.Count > 0 ? dataPoints[^1].Value : 0.0,
                TrendSlope = 0.0,
                HorizonMinutes = request.HorizonMinutes,
                ProjectedBreachTimeUtc = null,
                ConfidenceIntervalLower = 0.0,
                ConfidenceIntervalUpper = 0.0,
                SampleCount = dataPoints.Count,
                CalculationMethod = "InsufficientSamples",
                WillBreachThreshold = false
            };
        }

        var result = CalculateLinearForecast(dataPoints, request.HorizonMinutes, request.AlertThreshold);
        result.ApplicationId = request.ApplicationId;
        result.ServiceName = request.ServiceName;
        result.MetricName = request.MetricName;

        // Persist forecast to database
        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            var entity = new PredictiveForecast(
                orgId.Value,
                request.ApplicationId,
                request.ServiceName,
                request.MetricName,
                result.CurrentValue,
                result.PredictedValue,
                result.TrendSlope,
                result.HorizonMinutes,
                result.ProjectedBreachTimeUtc,
                result.ConfidenceIntervalLower,
                result.ConfidenceIntervalUpper,
                result.CalculationMethod,
                result.SampleCount,
                DateTime.UtcNow.AddMinutes(Math.Max(15, request.HorizonMinutes)));

            _context.PredictiveForecasts.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public static CapacityForecastResult CalculateLinearForecast(
        List<(DateTime Time, double Value)> points,
        int horizonMinutes,
        double alertThreshold)
    {
        int n = points.Count;
        var t0 = points[0].Time;

        // X = elapsed minutes from t0, Y = measured value
        var x = points.Select(p => (p.Time - t0).TotalMinutes).ToArray();
        var y = points.Select(p => p.Value).ToArray();

        double xMean = x.Average();
        double yMean = y.Average();

        double numerator = 0.0;
        double denominator = 0.0;

        for (int i = 0; i < n; i++)
        {
            numerator += (x[i] - xMean) * (y[i] - yMean);
            denominator += Math.Pow(x[i] - xMean, 2);
        }

        double slope = denominator > 1e-9 ? numerator / denominator : 0.0;
        double intercept = yMean - (slope * xMean);

        double lastX = x[^1];
        double currentValue = y[^1];
        double futureX = lastX + horizonMinutes;
        double predictedValue = intercept + (slope * futureX);

        // Residual variance
        double sumResidualSq = 0.0;
        for (int i = 0; i < n; i++)
        {
            double yHat = intercept + (slope * x[i]);
            sumResidualSq += Math.Pow(y[i] - yHat, 2);
        }

        double se = n > 2 ? Math.Sqrt(sumResidualSq / (n - 2)) : 0.0;
        double marginOfError = 1.96 * se; // 95% confidence interval

        // Breach calculation
        DateTime? projectedBreachTime = null;
        bool willBreach = false;

        if (slope > 1e-6 && alertThreshold > currentValue)
        {
            double breachX = (alertThreshold - intercept) / slope;
            if (breachX >= lastX)
            {
                projectedBreachTime = t0.AddMinutes(breachX);
                double minutesToBreach = breachX - lastX;
                willBreach = minutesToBreach <= horizonMinutes;
            }
        }
        else if (slope < -1e-6 && alertThreshold < currentValue)
        {
            double breachX = (alertThreshold - intercept) / slope;
            if (breachX >= lastX)
            {
                projectedBreachTime = t0.AddMinutes(breachX);
                double minutesToBreach = breachX - lastX;
                willBreach = minutesToBreach <= horizonMinutes;
            }
        }

        return new CapacityForecastResult
        {
            CurrentValue = Math.Round(currentValue, 2),
            PredictedValue = Math.Round(predictedValue, 2),
            TrendSlope = Math.Round(slope, 5),
            HorizonMinutes = horizonMinutes,
            ProjectedBreachTimeUtc = projectedBreachTime,
            ConfidenceIntervalLower = Math.Round(predictedValue - marginOfError, 2),
            ConfidenceIntervalUpper = Math.Round(predictedValue + marginOfError, 2),
            SampleCount = n,
            CalculationMethod = "LinearRegressionOrdinaryLeastSquares",
            WillBreachThreshold = willBreach
        };
    }

    public async Task<IReadOnlyList<PredictiveForecastDto>> GetActiveForecastsAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;
        var now = DateTime.UtcNow;

        var query = _context.PredictiveForecasts.AsNoTracking()
            .Where(f => f.ExpiresAtUtc > now);

        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            query = query.Where(f => f.OrganizationId == orgId.Value);
        }

        if (applicationId != Guid.Empty)
        {
            query = query.Where(f => f.ApplicationId == applicationId);
        }

        var list = await query
            .OrderByDescending(f => f.CalculatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        return list.Select(f => new PredictiveForecastDto
        {
            Id = f.Id,
            OrganizationId = f.OrganizationId,
            ApplicationId = f.ApplicationId,
            ServiceName = f.ServiceName,
            MetricName = f.MetricName,
            CurrentValue = f.CurrentValue,
            PredictedValue = f.PredictedValue,
            TrendSlope = f.TrendSlope,
            ForecastHorizonMinutes = f.ForecastHorizonMinutes,
            ProjectedBreachTimeUtc = f.ProjectedBreachTimeUtc,
            ConfidenceIntervalLower = f.ConfidenceIntervalLower,
            ConfidenceIntervalUpper = f.ConfidenceIntervalUpper,
            CalculationMethod = f.CalculationMethod,
            SampleCount = f.SampleCount,
            CalculatedAtUtc = f.CalculatedAtUtc,
            ExpiresAtUtc = f.ExpiresAtUtc
        }).ToList();
    }
}
