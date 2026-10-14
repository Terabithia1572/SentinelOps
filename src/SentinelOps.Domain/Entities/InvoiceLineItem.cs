using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; set; }
    public SaaSInvoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
