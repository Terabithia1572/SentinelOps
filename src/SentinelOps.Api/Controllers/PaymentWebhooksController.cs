using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/billing/webhooks")]
[AllowAnonymous]
public class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentWebhookHandler _webhookHandler;

    public PaymentWebhooksController(IPaymentWebhookHandler webhookHandler)
    {
        _webhookHandler = webhookHandler;
    }

    [HttpPost("{provider}")]
    public async Task<ActionResult<WebhookEventReceiptDto>> HandleWebhook(
        string provider,
        CancellationToken ct)
    {
        // Read raw body stream directly to preserve byte accuracy for HMAC-SHA256 signature verification
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawPayload = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequest(new WebhookEventReceiptDto
            {
                Received = false,
                Provider = provider,
                Message = "Webhook payload cannot be empty."
            });
        }

        // Stripe sends "Stripe-Signature", general providers use "X-Payment-Signature"
        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault()
                              ?? Request.Headers["X-Payment-Signature"].FirstOrDefault();

        var (success, receipt, error) = await _webhookHandler.ProcessWebhookAsync(provider, rawPayload, signatureHeader, ct);

        if (!success)
        {
            return BadRequest(receipt);
        }

        return Ok(receipt);
    }
}
