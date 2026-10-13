using FluentAssertions;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class ServiceLevelObjectiveTests
{
    [Fact]
    public void RecordEvaluation_WithZeroRequests_Returns100PercentCompliance()
    {
        // Arrange
        var slo = new ServiceLevelObjective
        {
            Name = "API Availability",
            ServiceName = "order-service",
            TargetPercentage = 99.9,
            IndicatorType = SloIndicatorType.Availability
        };

        // Act
        var eval = slo.RecordEvaluation(0, 0, DateTime.UtcNow);

        // Assert
        eval.CompliancePercentage.Should().Be(100.0);
        eval.ErrorBudgetRemaining.Should().Be(100.0);
        eval.BurnRate.Should().Be(0.0);
        eval.IsBreached.Should().BeFalse();
        slo.IsBreached.Should().BeFalse();
    }

    [Fact]
    public void RecordEvaluation_WithMeetingTarget_CalculatesAccurateBudgetAndBurnRate()
    {
        // Arrange
        var slo = new ServiceLevelObjective
        {
            Name = "API Availability",
            ServiceName = "order-service",
            TargetPercentage = 99.0, // 1% allowed errors
            IndicatorType = SloIndicatorType.Availability
        };

        // Act: 10,000 total requests, 9,950 good requests (50 errors = 0.5% error rate, half of allowed budget)
        var eval = slo.RecordEvaluation(10000, 9950, DateTime.UtcNow);

        // Assert
        eval.CompliancePercentage.Should().Be(99.5);
        eval.ErrorBudgetRemaining.Should().Be(50.0); // 50% of budget remaining
        eval.BurnRate.Should().Be(0.5); // Consuming budget at 0.5x rate
        eval.IsBreached.Should().BeFalse();
    }

    [Fact]
    public void RecordEvaluation_WhenBreachingTarget_MarksBreachedAndDepletesBudget()
    {
        // Arrange
        var slo = new ServiceLevelObjective
        {
            Name = "Order Service 99.9% Availability",
            ServiceName = "order-service",
            TargetPercentage = 99.9, // 0.1% allowed errors = 10 allowed bad requests per 10,000
            IndicatorType = SloIndicatorType.Availability
        };

        // Act: 10,000 requests, 20 bad requests (0.2% error rate = double the allowed error fraction)
        var eval = slo.RecordEvaluation(10000, 9980, DateTime.UtcNow);

        // Assert
        eval.CompliancePercentage.Should().Be(99.8);
        eval.BurnRate.Should().Be(2.0); // 2x burn rate
        eval.ErrorBudgetRemaining.Should().BeLessThanOrEqualTo(0.0);
        eval.IsBreached.Should().BeTrue();
        slo.IsBreached.Should().BeTrue();
    }
}
