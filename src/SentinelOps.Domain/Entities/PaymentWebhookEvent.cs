using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class PaymentWebhookEvent : Entity
{
    public string Provider { get; set; } = "Sandbox"; // "Sandbox", "Stripe"
    public string EventId { get; set; } = string.Empty; // Provider event id (deduplication key)
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;

    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SignatureHash { get; set; }
}
