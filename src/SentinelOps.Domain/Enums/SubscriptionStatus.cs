namespace SentinelOps.Domain.Enums;

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    GracePeriod = 4,
    Suspended = 5,
    Canceled = 6,
    Expired = 7
}
