using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class SubscriptionHistory : Entity
{
    public Guid SubscriptionId { get; set; }
    public OrganizationSubscription Subscription { get; set; } = null!;

    public Guid OrganizationId { get; set; }

    public SubscriptionStatus? PreviousStatus { get; set; }
    public SubscriptionStatus NewStatus { get; set; }

    public Guid? PreviousPlanId { get; set; }
    public Guid NewPlanId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
