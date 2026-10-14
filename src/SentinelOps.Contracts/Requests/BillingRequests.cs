namespace SentinelOps.Contracts.Requests;

public class ChangePlanRequest
{
    public string TargetTier { get; set; } = "Pro"; // "Starter", "Pro", "Enterprise"
    public string BillingInterval { get; set; } = "Monthly"; // "Monthly", "Annual"
    public bool Immediate { get; set; } = true;
}

public class CancelSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool Immediate { get; set; } = false;
}

public class UpdateBillingAccountRequest
{
    public string BillingEmail { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
}

public class SettleInvoiceRequest
{
    public string Reason { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public decimal AmountSettled { get; set; }
}
