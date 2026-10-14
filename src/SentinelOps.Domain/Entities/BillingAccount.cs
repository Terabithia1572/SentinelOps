using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class BillingAccount : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string BillingEmail { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string Currency { get; set; } = "USD";

    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }

    // External Provider Tokens (NEVER store raw credit card numbers or CVVs)
    public string? PaymentProviderCustomerId { get; set; }
    public string? PaymentMethodSummary { get; set; } // e.g. "Visa ending in 4242"
    public bool IsExemptFromTax { get; set; } = false;
}
