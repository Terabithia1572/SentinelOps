using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Infrastructure.Services.Payments;

public class DisabledPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Disabled";
    public bool IsConfigured => false;

    public Task<(bool Success, CheckoutSessionDto? Session, string? Error)> CreateCheckoutSessionAsync(
        Guid orgId,
        string userEmail,
        CreateCheckoutSessionRequest request,
        CancellationToken ct = default)
    {
        return Task.FromResult<(bool, CheckoutSessionDto?, string?)>(
            (false, null, "Payment provider is not configured. External payment gateway integration is BLOCKED/DISABLED."));
    }

    public Task<(bool Success, CustomerPortalSessionDto? Session, string? Error)> CreateCustomerPortalSessionAsync(
        Guid orgId,
        CreatePortalSessionRequest request,
        CancellationToken ct = default)
    {
        return Task.FromResult<(bool, CustomerPortalSessionDto?, string?)>(
            (false, null, "Payment provider is not configured. Customer portal is BLOCKED/DISABLED."));
    }

    public Task<RefundResultDto> RefundTransactionAsync(
        Guid orgId,
        string transactionId,
        RefundTransactionRequest request,
        CancellationToken ct = default)
    {
        return Task.FromResult(new RefundResultDto
        {
            Success = false,
            Error = "Payment provider is not configured. Refund is BLOCKED/DISABLED."
        });
    }
}
