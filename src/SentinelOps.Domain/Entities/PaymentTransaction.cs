using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class PaymentTransaction : Entity
{
    public Guid OrganizationId { get; set; }
    public Guid InvoiceId { get; set; }
    public SaaSInvoice Invoice { get; set; } = null!;

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    // Provider Info
    public string? PaymentProviderTransactionId { get; set; }
    public string PaymentProvider { get; set; } = "Sandbox"; // "Sandbox", "Stripe", "Manual"
    public string? FailureReason { get; set; }

    // Privileged Administrative Settlement Fields (audited & immutable)
    public string? SettledByUserId { get; set; }
    public string? SettledReason { get; set; }
}
