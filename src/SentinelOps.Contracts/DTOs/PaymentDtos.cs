namespace SentinelOps.Contracts.DTOs;

public class CheckoutSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Provider { get; set; } = "Sandbox";
}

public class CustomerPortalSessionDto
{
    public string PortalUrl { get; set; } = string.Empty;
    public string Provider { get; set; } = "Sandbox";
}

public class RefundResultDto
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class WebhookEventReceiptDto
{
    public bool Received { get; set; } = true;
    public string Provider { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public string Message { get; set; } = string.Empty;
}
