using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class OrganizationSubscription : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;
    public BillingInterval BillingInterval { get; set; } = BillingInterval.Monthly;

    public DateTime CurrentPeriodStartUtc { get; set; } = DateTime.UtcNow;
    public DateTime CurrentPeriodEndUtc { get; set; } = DateTime.UtcNow.AddMonths(1);

    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? GracePeriodEndsAtUtc { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; } = false;

    // External Provider Reference (e.g. Stripe subscription ID sub_xxx)
    public string? PaymentProviderSubscriptionId { get; set; }

    // Concurrency protection
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<SubscriptionHistory> History { get; set; } = new List<SubscriptionHistory>();

    public bool CanTransitionTo(SubscriptionStatus newStatus)
    {
        if (Status == newStatus) return true;

        return (Status, newStatus) switch
        {
            // Trialing can activate, expire, or cancel
            (SubscriptionStatus.Trialing, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.Trialing, SubscriptionStatus.Expired) => true,
            (SubscriptionStatus.Trialing, SubscriptionStatus.Canceled) => true,

            // Active can fall past-due, be canceled, or enter grace period
            (SubscriptionStatus.Active, SubscriptionStatus.PastDue) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.GracePeriod) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.Canceled) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.Suspended) => true,

            // PastDue can recover to Active, enter GracePeriod, be Suspended or Canceled
            (SubscriptionStatus.PastDue, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.PastDue, SubscriptionStatus.GracePeriod) => true,
            (SubscriptionStatus.PastDue, SubscriptionStatus.Suspended) => true,
            (SubscriptionStatus.PastDue, SubscriptionStatus.Canceled) => true,

            // GracePeriod can recover to Active, be Suspended, or Canceled
            (SubscriptionStatus.GracePeriod, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.GracePeriod, SubscriptionStatus.Suspended) => true,
            (SubscriptionStatus.GracePeriod, SubscriptionStatus.Canceled) => true,

            // Suspended can reactivate to Active, be Canceled, or Expire
            (SubscriptionStatus.Suspended, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.Suspended, SubscriptionStatus.Canceled) => true,
            (SubscriptionStatus.Suspended, SubscriptionStatus.Expired) => true,

            // Canceled can reactivate if authorized
            (SubscriptionStatus.Canceled, SubscriptionStatus.Active) => true,

            _ => false
        };
    }

    public void TransitionTo(SubscriptionStatus newStatus, string? reason = null, string? changedByUserId = null)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Illegal subscription transition from {Status} to {newStatus}.");
        }

        var prevStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        ConcurrencyToken = Guid.NewGuid().ToString("N");

        if (newStatus == SubscriptionStatus.Canceled && CanceledAtUtc == null)
        {
            CanceledAtUtc = DateTime.UtcNow;
        }

        History.Add(new SubscriptionHistory
        {
            SubscriptionId = Id,
            OrganizationId = OrganizationId,
            PreviousStatus = prevStatus,
            NewStatus = newStatus,
            NewPlanId = PlanId,
            Reason = reason ?? $"Status transitioned to {newStatus}",
            ChangedByUserId = changedByUserId,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
