using FluentAssertions;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;

namespace SentinelOps.Domain.Tests.Entities;

public class SubscriptionDomainTests
{
    [Fact]
    public void OrganizationSubscription_DefaultState_IsTrialing()
    {
        var subscription = new OrganizationSubscription
        {
            OrganizationId = Guid.NewGuid(),
            PlanId = Guid.NewGuid()
        };

        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.CancelAtPeriodEnd.Should().BeFalse();
        subscription.ConcurrencyToken.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Expired, true)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Canceled, true)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Suspended, false)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.GracePeriod, false)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.PastDue, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.GracePeriod, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Canceled, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.GracePeriod, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Suspended, true)]
    [InlineData(SubscriptionStatus.GracePeriod, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.GracePeriod, SubscriptionStatus.Suspended, true)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Canceled, true)]
    [InlineData(SubscriptionStatus.Expired, SubscriptionStatus.Active, false)]
    public void CanTransitionTo_EnforcesLegalTransitions(
        SubscriptionStatus currentStatus,
        SubscriptionStatus targetStatus,
        bool expectedAllowed)
    {
        var subscription = new OrganizationSubscription
        {
            OrganizationId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Status = currentStatus
        };

        var allowed = subscription.CanTransitionTo(targetStatus);
        allowed.Should().Be(expectedAllowed);
    }

    [Fact]
    public void TransitionTo_LegalTransition_UpdatesStatusAndAddsHistory()
    {
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = new OrganizationSubscription
        {
            OrganizationId = orgId,
            PlanId = planId,
            Status = SubscriptionStatus.Trialing
        };

        var initialToken = subscription.ConcurrencyToken;

        subscription.TransitionTo(SubscriptionStatus.Active, "Payment confirmed", "user-123");

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.ConcurrencyToken.Should().NotBe(initialToken);
        subscription.History.Should().HaveCount(1);

        var history = subscription.History.First();
        history.PreviousStatus.Should().Be(SubscriptionStatus.Trialing);
        history.NewStatus.Should().Be(SubscriptionStatus.Active);
        history.Reason.Should().Be("Payment confirmed");
        history.ChangedByUserId.Should().Be("user-123");
    }

    [Fact]
    public void TransitionTo_IllegalTransition_ThrowsInvalidOperationException()
    {
        var subscription = new OrganizationSubscription
        {
            OrganizationId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Trialing
        };

        var act = () => subscription.TransitionTo(SubscriptionStatus.Suspended, "Invalid skip");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Illegal subscription transition*");
    }

    [Fact]
    public void TransitionTo_Canceled_SetsCanceledAtUtc()
    {
        var subscription = new OrganizationSubscription
        {
            OrganizationId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active
        };

        subscription.TransitionTo(SubscriptionStatus.Canceled, "User requested cancellation");

        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.CanceledAtUtc.Should().NotBeNull();
        subscription.CanceledAtUtc.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
