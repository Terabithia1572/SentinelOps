using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface IBillingService
{
    Task<BillingAccountDto?> GetBillingAccountAsync(Guid orgId, string userId, CancellationToken ct = default);

    Task<(bool Success, BillingAccountDto? Account, string? Error)> UpdateBillingAccountAsync(
        Guid orgId,
        UpdateBillingAccountRequest request,
        string userId,
        CancellationToken ct = default);

    Task<IEnumerable<SaaSInvoiceDto>> GetInvoicesAsync(Guid orgId, string userId, CancellationToken ct = default);

    Task<SaaSInvoiceDto?> GetInvoiceByIdAsync(Guid orgId, Guid invoiceId, string userId, CancellationToken ct = default);

    Task<SaaSInvoiceDto> GenerateInvoiceAsync(
        Guid orgId,
        string reason,
        List<InvoiceLineItemDto> items,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> SettleInvoiceAdministrativelyAsync(
        Guid orgId,
        Guid invoiceId,
        SettleInvoiceRequest request,
        string adminUserId,
        CancellationToken ct = default);
}
