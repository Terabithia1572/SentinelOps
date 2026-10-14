using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IPaymentWebhookHandler
{
    Task<(bool Success, WebhookEventReceiptDto Receipt, string? Error)> ProcessWebhookAsync(
        string provider,
        string rawPayload,
        string? signatureHeader,
        CancellationToken ct = default);
}
