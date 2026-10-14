namespace SentinelOps.Contracts.Requests;

public class CreateCheckoutSessionRequest
{
    public string TargetTier { get; set; } = "Pro";
    public string BillingInterval { get; set; } = "Monthly"; // "Monthly" or "Annual"
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CreatePortalSessionRequest
{
    public string? ReturnUrl { get; set; }
}

public class RefundTransactionRequest
{
    public decimal? Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
