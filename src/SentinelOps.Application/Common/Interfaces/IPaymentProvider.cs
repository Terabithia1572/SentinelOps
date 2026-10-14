using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface IPaymentProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }

    Task<(bool Success, CheckoutSessionDto? Session, string? Error)> CreateCheckoutSessionAsync(
        Guid orgId,
        string userEmail,
        CreateCheckoutSessionRequest request,
        CancellationToken ct = default);

    Task<(bool Success, CustomerPortalSessionDto? Session, string? Error)> CreateCustomerPortalSessionAsync(
        Guid orgId,
        CreatePortalSessionRequest request,
        CancellationToken ct = default);

    Task<RefundResultDto> RefundTransactionAsync(
        Guid orgId,
        string transactionId,
        RefundTransactionRequest request,
        CancellationToken ct = default);
}
