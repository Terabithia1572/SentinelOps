using FluentAssertions;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class IncidentTests
{
    [Fact]
    public void IncidentLifecycle_ValidTransitions_EmitsAuditedTimelineEvents()
    {
        // Arrange
        var incident = new Incident
        {
            IncidentNumber = "INC-2026-0001",
            Title = "High Database Error Rate",
            Description = "Errors exceeded 15% in order-service",
            Severity = Severity.Critical,
            Status = IncidentStatus.Open,
            ApplicationId = Guid.NewGuid()
        };

        // Act 1: Acknowledge
        incident.Acknowledge("usr_1", "Alice");
        incident.AcknowledgedAtUtc.Should().NotBeNull();
        incident.AcknowledgedBy.Should().Be("Alice");
        incident.TimelineEvents.Should().ContainSingle(e => e.EventType == IncidentTimelineEventType.Acknowledged);

        // Act 2: Investigate
        incident.Investigate("usr_1", "Alice");
        incident.Status.Should().Be(IncidentStatus.Investigating);

        // Act 3: Add Comment
        incident.AddComment("usr_2", "Bob", "Investigating DB connection pool exhaustion.");
        incident.Comments.Should().ContainSingle(c => c.UserName == "Bob");

        // Act 4: Mitigate
        incident.Mitigate("usr_1", "Alice", "Scaled DB read replicas.");
        incident.Status.Should().Be(IncidentStatus.Mitigated);
        incident.MitigatedAtUtc.Should().NotBeNull();

        // Act 5: Resolve
        incident.Resolve("usr_1", "Alice", "Connection pool exhausted due to unindexed query", "Added index on orders.created_at");
        incident.Status.Should().Be(IncidentStatus.Resolved);
        incident.ResolvedAtUtc.Should().NotBeNull();
        incident.RootCauseSummary.Should().NotBeNull();

        // Act 6: Close
        incident.Close("usr_1", "Alice", "Closed following 24h stability verification");
        incident.Status.Should().Be(IncidentStatus.Closed);
        incident.ClosedAtUtc.Should().NotBeNull();

        // Assert
        incident.TimelineEvents.Count.Should().Be(6);
    }

    [Fact]
    public void Resolve_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var incident = new Incident
        {
            Status = IncidentStatus.Closed
        };

        // Act
        var act = () => incident.Resolve("usr_1", "Alice", "Root cause", "Notes");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void Reopen_WhenClosed_TransitionsToOpen()
    {
        // Arrange
        var incident = new Incident
        {
            Status = IncidentStatus.Closed,
            ClosedAtUtc = DateTime.UtcNow
        };

        // Act
        incident.Reopen("usr_1", "Alice", "Error recurred after deployment");

        // Assert
        incident.Status.Should().Be(IncidentStatus.Open);
        incident.ClosedAtUtc.Should().BeNull();
        incident.TimelineEvents.Should().Contain(e => e.EventType == IncidentTimelineEventType.Reopened);
    }

    [Fact]
    public void Assign_UpdatesAssigneeAndEmitsTimelineEvent()
    {
        // Arrange
        var incident = new Incident();

        // Act
        incident.Assign("usr_3", "Charlie", "usr_1", "Alice");

        // Assert
        incident.AssignedToUserId.Should().Be("usr_3");
        incident.AssignedToUserName.Should().Be("Charlie");
        incident.TimelineEvents.Should().Contain(e => e.EventType == IncidentTimelineEventType.Assigned && e.ActorName == "Alice");
    }
}
