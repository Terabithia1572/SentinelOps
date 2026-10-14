using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class SaaSInvoice : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid? SubscriptionId { get; set; }
    public OrganizationSubscription? Subscription { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "USD";

    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime DueDateUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    // External Gateway References
    public string? PaymentProviderInvoiceId { get; set; }
    public string? PaymentProviderPaymentIntentId { get; set; }
    public string BillingReason { get; set; } = "SubscriptionCycle"; // "SubscriptionCycle", "PlanUpgrade", "ManualSettlement"

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
