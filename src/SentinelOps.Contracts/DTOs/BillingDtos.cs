namespace SentinelOps.Contracts.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPriceUsd { get; set; }
    public decimal AnnualPriceUsd { get; set; }
    public int MaxApplications { get; set; }
    public int MaxMembers { get; set; }
    public int MaxCustomDashboards { get; set; }
    public long MaxMonthlyLogs { get; set; }
    public long MaxMonthlyTraces { get; set; }
    public long MaxMonthlyMetrics { get; set; }
    public int RetentionDays { get; set; }
    public int MaxMonthlyAiTokens { get; set; }
    public bool AllowCustomReports { get; set; }
    public bool AllowSloAlerts { get; set; }
    public bool AllowApiAccess { get; set; }
    public bool AllowDedicatedSupport { get; set; }
}

public class OrganizationSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanTier { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingInterval { get; set; } = string.Empty;
    public decimal MonthlyPriceUsd { get; set; }
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? GracePeriodEndsAtUtc { get; set; }
    public DateTime? CanceledAtUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public bool IsInTrial => Status == "Trialing" && TrialEndsAtUtc.HasValue && TrialEndsAtUtc.Value > DateTime.UtcNow;
    public int DaysRemainingInTrial => TrialEndsAtUtc.HasValue && TrialEndsAtUtc.Value > DateTime.UtcNow
        ? (int)Math.Ceiling((TrialEndsAtUtc.Value - DateTime.UtcNow).TotalDays)
        : 0;
    public string ConcurrencyToken { get; set; } = string.Empty;
    public List<SubscriptionHistoryDto> History { get; set; } = new();
}

public class SubscriptionHistoryDto
{
    public Guid Id { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
    public DateTime TimestampUtc { get; set; }
}

public class BillingAccountDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string BillingEmail { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string Currency { get; set; } = "USD";
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? PaymentMethodSummary { get; set; }
    public bool HasPaymentMethod => !string.IsNullOrEmpty(PaymentMethodSummary);
    public bool IsExemptFromTax { get; set; }
}

public class SaaSInvoiceDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime DueDateUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string BillingReason { get; set; } = string.Empty;
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    public List<PaymentTransactionDto> Transactions { get; set; } = new();
}

public class InvoiceLineItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentProvider { get; set; } = string.Empty;
    public string? PaymentProviderTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public string? SettledByUserId { get; set; }
    public string? SettledReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
