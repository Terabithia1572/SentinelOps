using FluentAssertions;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class AlertRuleTests
{
    [Fact]
    public void Evaluate_WhenThresholdExceededAndConsecutiveCountMet_TransitionsToFiring()
    {
        // Arrange
        var rule = new AlertRule
        {
            Name = "High Error Rate",
            ConditionType = AlertConditionType.ErrorRateAboveThreshold,
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 5.0,
            ConsecutiveCountRequired = 2,
            Status = AlertStatus.Normal
        };
        var utcNow = DateTime.UtcNow;

        // Act - First evaluation (consecutive count = 1 < 2)
        bool firedFirst = rule.Evaluate(6.2, utcNow, out var eventFirst);

        // Assert first
        firedFirst.Should().BeFalse();
        rule.Status.Should().Be(AlertStatus.Pending);
        rule.CurrentConsecutiveFailures.Should().Be(1);
        eventFirst.Should().BeNull();

        // Act - Second evaluation (consecutive count = 2 >= 2)
        bool firedSecond = rule.Evaluate(7.5, utcNow.AddMinutes(1), out var eventSecond);

        // Assert second
        firedSecond.Should().BeTrue();
        rule.Status.Should().Be(AlertStatus.Firing);
        rule.CurrentConsecutiveFailures.Should().Be(2);
        eventSecond.Should().NotBeNull();
        eventSecond!.NewStatus.Should().Be(AlertStatus.Firing);
        eventSecond.MeasuredValue.Should().Be(7.5);
    }

    [Fact]
    public void Evaluate_WhenFiringAndWithinCooldown_SuppressesNotificationEvent()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var rule = new AlertRule
        {
            Name = "P95 High Latency",
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 500.0,
            ConsecutiveCountRequired = 1,
            CooldownMinutes = 15,
            Status = AlertStatus.Firing,
            LastFiredAtUtc = utcNow
        };

        // Act - Evaluate 5 minutes later (still inside 15m cooldown)
        bool fired = rule.Evaluate(620.0, utcNow.AddMinutes(5), out var alertEvent);

        // Assert
        fired.Should().BeFalse(); // Notification event suppressed due to cooldown
        rule.Status.Should().Be(AlertStatus.Firing);
        alertEvent.Should().BeNull();
    }

    [Fact]
    public void Evaluate_WhenFiringAndMetricRecovers_TransitionsToResolved()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var rule = new AlertRule
        {
            Name = "Error Rate",
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 5.0,
            ConsecutiveCountRequired = 1,
            Status = AlertStatus.Firing,
            LastFiredAtUtc = utcNow.AddMinutes(-10)
        };

        // Act - Value drops back down to 1.2%
        bool stateChanged = rule.Evaluate(1.2, utcNow, out var alertEvent);

        // Assert
        stateChanged.Should().BeTrue();
        rule.Status.Should().Be(AlertStatus.Resolved);
        rule.CurrentConsecutiveFailures.Should().Be(0);
        alertEvent.Should().NotBeNull();
        alertEvent!.NewStatus.Should().Be(AlertStatus.Resolved);
    }

    [Fact]
    public void Evaluate_WhenSuppressed_RemainsSuppressedUntilExpiry()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var rule = new AlertRule
        {
            Name = "Database Latency",
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 100.0,
            SuppressedUntilUtc = utcNow.AddHours(2)
        };

        // Act
        bool fired = rule.Evaluate(250.0, utcNow, out var alertEvent);

        // Assert
        fired.Should().BeFalse();
        rule.Status.Should().Be(AlertStatus.Suppressed);
        alertEvent.Should().BeNull();
    }

    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, 10.0, 15.0, true)]
    [InlineData(ComparisonOperator.GreaterThan, 10.0, 5.0, false)]
    [InlineData(ComparisonOperator.LessThan, 10.0, 5.0, true)]
    [InlineData(ComparisonOperator.LessThan, 10.0, 15.0, false)]
    [InlineData(ComparisonOperator.Equal, 10.0, 10.0, true)]
    [InlineData(ComparisonOperator.Equal, 10.0, 10.5, false)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 10.0, 10.0, true)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 10.0, 10.0, true)]
    public void Evaluate_ComparisonOperators_EvaluatedCorrectly(
        ComparisonOperator op,
        double threshold,
        double measured,
        bool shouldTrigger)
    {
        // Arrange
        var rule = new AlertRule
        {
            Name = "Op Test",
            Operator = op,
            Threshold = threshold,
            ConsecutiveCountRequired = 1
        };

        // Act
        rule.Evaluate(measured, DateTime.UtcNow, out _);

        // Assert
        if (shouldTrigger)
        {
            rule.Status.Should().Be(AlertStatus.Firing);
        }
        else
        {
            rule.Status.Should().Be(AlertStatus.Normal);
        }
    }

    [Fact]
    public void Acknowledge_WhenFiring_TransitionsToAcknowledged()
    {
        // Arrange
        var rule = new AlertRule
        {
            Name = "Service Health",
            Status = AlertStatus.Firing
        };

        // Act
        rule.Acknowledge("operator1", DateTime.UtcNow);

        // Assert
        rule.Status.Should().Be(AlertStatus.Acknowledged);
        rule.Events.Should().ContainSingle(e => e.NewStatus == AlertStatus.Acknowledged);
    }
}
