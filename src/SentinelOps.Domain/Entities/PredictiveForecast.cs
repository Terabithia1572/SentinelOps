using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class PredictiveForecast : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string MetricName { get; set; } = "disk_usage_pct";
    public double CurrentValue { get; set; }
    public double PredictedValue { get; set; }
    public double TrendSlope { get; set; }
    public int ForecastHorizonMinutes { get; set; } = 60;
    public DateTime? ProjectedBreachTimeUtc { get; set; }
    public double ConfidenceIntervalLower { get; set; }
    public double ConfidenceIntervalUpper { get; set; }
    public string CalculationMethod { get; set; } = "LinearRegressionOrdinaryLeastSquares";
    public int SampleCount { get; set; }
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    public PredictiveForecast()
    {
    }

    public PredictiveForecast(
        Guid organizationId,
        Guid applicationId,
        string serviceName,
        string metricName,
        double currentValue,
        double predictedValue,
        double trendSlope,
        int forecastHorizonMinutes,
        DateTime? projectedBreachTimeUtc,
        double confidenceIntervalLower,
        double confidenceIntervalUpper,
        string calculationMethod,
        int sampleCount,
        DateTime expiresAtUtc)
    {
        OrganizationId = organizationId;
        ApplicationId = applicationId;
        ServiceName = serviceName;
        MetricName = metricName;
        CurrentValue = currentValue;
        PredictedValue = predictedValue;
        TrendSlope = trendSlope;
        ForecastHorizonMinutes = forecastHorizonMinutes;
        ProjectedBreachTimeUtc = projectedBreachTimeUtc;
        ConfidenceIntervalLower = confidenceIntervalLower;
        ConfidenceIntervalUpper = confidenceIntervalUpper;
        CalculationMethod = calculationMethod;
        SampleCount = sampleCount;
        CalculatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }
}
