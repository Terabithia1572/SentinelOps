namespace SentinelOps.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc => DateTime.UtcNow;
}
