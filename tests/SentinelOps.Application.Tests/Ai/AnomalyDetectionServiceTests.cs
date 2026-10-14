using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Services.Ai;
using Xunit;

namespace SentinelOps.Application.Tests.Ai;

public class AnomalyDetectionServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly AnomalyDetectionService _service;

    public AnomalyDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _currentTenantService = Substitute.For<ICurrentTenantService>();
        _service = new AnomalyDetectionService(_context, _currentTenantService, NullLogger<AnomalyDetectionService>.Instance);
    }

    [Fact]
    public async Task EvaluateSignalAsync_WhenSparseData_ReturnsInsufficientSamplesSafely()
    {
        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            ServiceName = "orders-api",
            SignalType = "ErrorRate",
            CurrentValue = 15.0,
            HistoricalValues = [1.0, 1.2, 1.1] // Only 3 samples (< 5 required)
        };

        var result = await _service.EvaluateSignalAsync(request);

        result.IsAnomaly.Should().BeFalse();
        result.ConfidenceScore.Should().Be(0.0);
        result.DetectionMethod.Should().Be("InsufficientSamples");
        result.Explanation.Should().Contain("Insufficient historical data");
    }

    [Fact]
    public async Task EvaluateSignalAsync_WhenNormalTraffic_DoesNotTriggerAnomaly()
    {
        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            ServiceName = "payment-gateway",
            SignalType = "ResponseLatency",
            CurrentValue = 52.0,
            HistoricalValues = [50.0, 48.0, 53.0, 49.0, 51.0, 52.0, 47.0, 53.0, 50.0, 51.0]
        };

        var result = await _service.EvaluateSignalAsync(request);

        result.IsAnomaly.Should().BeFalse();
        result.DeviationScore.Should().BeLessThan(AnomalyDetectionService.DefaultSensitivityThreshold);
        result.Explanation.Should().Contain("within the expected baseline range");
    }

    [Fact]
    public async Task EvaluateSignalAsync_WhenSuddenErrorSpike_DetectsAnomalyWithHighConfidence()
    {
        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            ServiceName = "auth-service",
            SignalType = "ErrorRate",
            CurrentValue = 42.0, // High spike
            HistoricalValues = [0.1, 0.2, 0.15, 0.1, 0.2, 0.18, 0.12, 0.1, 0.14, 0.19, 0.16, 0.11]
        };

        var result = await _service.EvaluateSignalAsync(request);

        result.IsAnomaly.Should().BeTrue();
        result.DeviationScore.Should().BeGreaterThan(5.0);
        result.Severity.Should().Be("Critical");
        result.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0.70);
        result.Explanation.Should().Contain("surged above expected baseline");
    }

    [Fact]
    public async Task EvaluateSignalAsync_WhenNoisyMetrics_RobustMedianMadHandlesOutliersSafely()
    {
        // Dataset has extreme historical outliers that would distort standard mean/variance,
        // but robust MAD preserves the true typical baseline
        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            ServiceName = "inventory-service",
            SignalType = "CpuUtilization",
            CurrentValue = 88.0,
            HistoricalValues = [20.0, 22.0, 21.0, 19.0, 20.0, 95.0 /* historic outlier */, 21.0, 20.0, 22.0, 20.0]
        };

        var result = await _service.EvaluateSignalAsync(request);

        result.IsAnomaly.Should().BeTrue();
        result.DetectionMethod.Should().Be("RobustMedianMAD");
        result.BaselineValue.Should().BeInRange(19.0, 22.0); // True median
    }

    [Fact]
    public async Task EvaluateSignalAsync_DuplicateSuppression_PreventsExcessiveDbInserts()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var request = new AnomalyEvaluationRequest
        {
            OrganizationId = orgId,
            ApplicationId = appId,
            ServiceName = "checkout-api",
            SignalType = "ErrorRate",
            CurrentValue = 75.0,
            HistoricalValues = [1.0, 1.1, 1.0, 1.2, 0.9, 1.1, 1.0, 1.0, 1.2, 0.8]
        };

        // First evaluation: creates record
        var result1 = await _service.EvaluateSignalAsync(request);
        result1.IsAnomaly.Should().BeTrue();

        var countAfterFirst = await _context.TelemetryAnomalies.CountAsync();
        countAfterFirst.Should().Be(1);

        // Immediate second evaluation for same service/signal: suppressed by 15m cooldown
        var result2 = await _service.EvaluateSignalAsync(request);
        result2.IsAnomaly.Should().BeTrue();

        var countAfterSecond = await _context.TelemetryAnomalies.CountAsync();
        countAfterSecond.Should().Be(1); // Still 1, duplicate suppressed
    }

    [Fact]
    public async Task GetActiveAnomaliesAsync_StrictlyEnforcesTenantIsolation()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var anomaly1 = new TelemetryAnomaly(tenant1, appId, "orders", "ErrorRate", 1.0, 20.0, 4.0, 0.85, Severity.Error, "MAD", "Tenant 1 anomaly");
        var anomaly2 = new TelemetryAnomaly(tenant2, appId, "orders", "ErrorRate", 1.0, 25.0, 5.0, 0.90, Severity.Critical, "MAD", "Tenant 2 anomaly");

        _context.TelemetryAnomalies.AddRange(anomaly1, anomaly2);
        await _context.SaveChangesAsync();

        // Scope to tenant 1
        _currentTenantService.OrganizationId.Returns(tenant1);

        var listTenant1 = await _service.GetActiveAnomaliesAsync(appId);
        listTenant1.Should().HaveCount(1);
        listTenant1[0].Explanation.Should().Contain("Tenant 1 anomaly");

        // Scope to tenant 2
        _currentTenantService.OrganizationId.Returns(tenant2);

        var listTenant2 = await _service.GetActiveAnomaliesAsync(appId);
        listTenant2.Should().HaveCount(1);
        listTenant2[0].Explanation.Should().Contain("Tenant 2 anomaly");
    }

    [Fact]
    public async Task ResolveAnomalyAsync_ResolvesAnomaly_WithOperatorAttribution()
    {
        var orgId = Guid.NewGuid();
        var anomaly = new TelemetryAnomaly(orgId, Guid.NewGuid(), "billing", "Latency", 10.0, 150.0, 4.5, 0.88, Severity.Error, "ZScore", "Spike");
        _context.TelemetryAnomalies.Add(anomaly);
        await _context.SaveChangesAsync();

        _currentTenantService.OrganizationId.Returns(orgId);

        var success = await _service.ResolveAnomalyAsync(anomaly.Id, "oncall-lead");
        success.Should().BeTrue();

        var updated = await _context.TelemetryAnomalies.FindAsync(anomaly.Id);
        updated!.Status.Should().Be("Resolved");
        updated.ResolvedBy.Should().Be("oncall-lead");
        updated.ResolvedAtUtc.Should().NotBeNull();
    }
}
