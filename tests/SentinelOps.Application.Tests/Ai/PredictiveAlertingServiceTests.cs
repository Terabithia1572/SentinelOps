using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Services.Ai;
using Xunit;

namespace SentinelOps.Application.Tests.Ai;

public class PredictiveAlertingServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly PredictiveAlertingService _service;

    public PredictiveAlertingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _currentTenantService = Substitute.For<ICurrentTenantService>();
        _service = new PredictiveAlertingService(_context, _currentTenantService, NullLogger<PredictiveAlertingService>.Instance);
    }

    [Fact]
    public async Task ForecastMetricAsync_WhenInsufficientSamples_ReturnsInsufficientSamplesSafely()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var request = new CapacityForecastRequest
        {
            OrganizationId = orgId,
            ApplicationId = appId,
            ServiceName = "cache-service",
            MetricName = "memory_usage_mb",
            HorizonMinutes = 60,
            AlertThreshold = 1000.0
        };

        // No metric records in DB
        var result = await _service.ForecastMetricAsync(request);

        result.CalculationMethod.Should().Be("InsufficientSamples");
        result.WillBreachThreshold.Should().BeFalse();
        result.ProjectedBreachTimeUtc.Should().BeNull();
        result.SampleCount.Should().Be(0);
    }

    [Fact]
    public void CalculateLinearForecast_WhenStableData_ForecastsConstantValueAndNoBreach()
    {
        var now = DateTime.UtcNow;
        var points = new List<(DateTime Time, double Value)>
        {
            (now.AddMinutes(-50), 50.0),
            (now.AddMinutes(-40), 50.0),
            (now.AddMinutes(-30), 50.1),
            (now.AddMinutes(-20), 49.9),
            (now.AddMinutes(-10), 50.0),
            (now, 50.0)
        };

        var result = PredictiveAlertingService.CalculateLinearForecast(points, horizonMinutes: 60, alertThreshold: 90.0);

        result.CurrentValue.Should().Be(50.0);
        result.TrendSlope.Should().BeInRange(-0.01, 0.01);
        result.PredictedValue.Should().BeInRange(49.0, 51.0);
        result.WillBreachThreshold.Should().BeFalse();
        result.ProjectedBreachTimeUtc.Should().BeNull();
        result.CalculationMethod.Should().Be("LinearRegressionOrdinaryLeastSquares");
    }

    [Fact]
    public void CalculateLinearForecast_WhenRapidlyIncreasingTrend_ForecastsBreachAccurately()
    {
        var now = DateTime.UtcNow;
        // Increases 1 unit per minute: 0m=50, 10m=60, 20m=70, 30m=80, 40m=90
        var points = new List<(DateTime Time, double Value)>
        {
            (now.AddMinutes(-40), 50.0),
            (now.AddMinutes(-30), 60.0),
            (now.AddMinutes(-20), 70.0),
            (now.AddMinutes(-10), 80.0),
            (now, 90.0)
        };

        // Threshold = 100.0, Horizon = 30 minutes. Current = 90. Should breach in 10 minutes.
        var result = PredictiveAlertingService.CalculateLinearForecast(points, horizonMinutes: 30, alertThreshold: 100.0);

        result.TrendSlope.Should().BeApproximately(1.0, 0.01);
        result.CurrentValue.Should().Be(90.0);
        result.PredictedValue.Should().Be(120.0);
        result.WillBreachThreshold.Should().BeTrue();
        result.ProjectedBreachTimeUtc.Should().NotBeNull();
        result.ProjectedBreachTimeUtc!.Value.Should().BeCloseTo(now.AddMinutes(10), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CalculateLinearForecast_WhenDecreasingTrend_DoesNotBreachUpperThreshold()
    {
        var now = DateTime.UtcNow;
        var points = new List<(DateTime Time, double Value)>
        {
            (now.AddMinutes(-40), 90.0),
            (now.AddMinutes(-30), 80.0),
            (now.AddMinutes(-20), 70.0),
            (now.AddMinutes(-10), 60.0),
            (now, 50.0)
        };

        var result = PredictiveAlertingService.CalculateLinearForecast(points, horizonMinutes: 60, alertThreshold: 95.0);

        result.TrendSlope.Should().BeLessThan(0);
        result.PredictedValue.Should().BeLessThan(result.CurrentValue);
        result.WillBreachThreshold.Should().BeFalse();
        result.ProjectedBreachTimeUtc.Should().BeNull();
    }

    [Fact]
    public void CalculateLinearForecast_CalculatesConfidenceIntervalsProperly()
    {
        var now = DateTime.UtcNow;
        // Data with variance around slope
        var points = new List<(DateTime Time, double Value)>
        {
            (now.AddMinutes(-50), 20.0),
            (now.AddMinutes(-40), 24.0),
            (now.AddMinutes(-30), 28.0),
            (now.AddMinutes(-20), 35.0),
            (now.AddMinutes(-10), 38.0),
            (now, 45.0)
        };

        var result = PredictiveAlertingService.CalculateLinearForecast(points, horizonMinutes: 30, alertThreshold: 100.0);

        result.ConfidenceIntervalLower.Should().BeLessThan(result.PredictedValue);
        result.ConfidenceIntervalUpper.Should().BeGreaterThan(result.PredictedValue);
    }

    [Fact]
    public async Task ForecastMetricAsync_WhenSufficientDatabaseMetrics_PersistsPredictiveForecast()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _currentTenantService.OrganizationId.Returns(orgId);

        var app = new SentinelOps.Domain.Entities.Application
        {
            Name = "CapacityApp",
            DisplayName = "Capacity App",
            Description = "App for capacity forecast test",
            OrganizationId = orgId
        };
        typeof(SentinelOps.Domain.Entities.Application).GetProperty("Id")!.SetValue(app, appId);
        _context.Applications.Add(app);

        var now = DateTime.UtcNow;
        for (int i = 0; i < 6; i++)
        {
            var metric = new MetricRecord
            {
                ApplicationId = appId,
                ServiceName = "api-gateway",
                EnvironmentName = "Production",
                MetricName = "http_request_rate",
                MetricType = Domain.Enums.MetricType.Gauge,
                Value = 100.0 + (i * 10.0),
                Tags = "{}",
                Timestamp = now.AddMinutes(-30 + (i * 5))
            };
            _context.MetricRecords.Add(metric);
        }
        await _context.SaveChangesAsync();

        var request = new CapacityForecastRequest
        {
            OrganizationId = orgId,
            ApplicationId = appId,
            ServiceName = "api-gateway",
            MetricName = "http_request_rate",
            HorizonMinutes = 30,
            AlertThreshold = 200.0
        };

        var result = await _service.ForecastMetricAsync(request);

        result.SampleCount.Should().Be(6);
        result.CalculationMethod.Should().Be("LinearRegressionOrdinaryLeastSquares");

        var persisted = await _context.PredictiveForecasts.FirstOrDefaultAsync(f => f.ApplicationId == appId);
        persisted.Should().NotBeNull();
        persisted!.MetricName.Should().Be("http_request_rate");
        persisted.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task GetActiveForecastsAsync_TenantIsolation_FiltersByOrganization()
    {
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _context.PredictiveForecasts.AddRange(
            new PredictiveForecast(org1, appId, "orders", "cpu_pct", 50, 70, 0.5, 60, null, 65, 75, "OLS", 10, DateTime.UtcNow.AddMinutes(30)),
            new PredictiveForecast(org2, appId, "billing", "cpu_pct", 80, 95, 0.8, 60, null, 90, 100, "OLS", 10, DateTime.UtcNow.AddMinutes(30))
        );
        await _context.SaveChangesAsync();

        _currentTenantService.OrganizationId.Returns(org1);

        var forecasts = await _service.GetActiveForecastsAsync(appId);

        forecasts.Should().HaveCount(1);
        forecasts[0].OrganizationId.Should().Be(org1);
        forecasts[0].ServiceName.Should().Be("orders");
    }
}
