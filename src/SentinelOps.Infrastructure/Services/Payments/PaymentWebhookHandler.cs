using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Payments;

public class PaymentWebhookHandler : IPaymentWebhookHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentWebhookVerifier _verifier;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentWebhookHandler> _logger;

    public PaymentWebhookHandler(
        IApplicationDbContext context,
        IPaymentWebhookVerifier verifier,
        IAuditService auditService,
        IConfiguration config,
        ILogger<PaymentWebhookHandler> logger)
    {
        _context = context;
        _verifier = verifier;
        _auditService = auditService;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, WebhookEventReceiptDto Receipt, string? Error)> ProcessWebhookAsync(
        string provider,
        string rawPayload,
        string? signatureHeader,
        CancellationToken ct = default)
    {
        var secret = _config["PaymentSettings:WebhookSecret"] ?? "whsec_sandbox_test_secret_key_12345";
        var tolerance = int.TryParse(_config["PaymentSettings:ToleranceSeconds"], out var tol) ? tol : 300;

        // 1. Signature & Freshness Verification
        var (isValid, sigError, timestamp) = _verifier.VerifySignature(rawPayload, signatureHeader, secret, tolerance);
        if (!isValid)
        {
            _logger.LogWarning("Webhook signature verification failed for provider {Provider}: {Error}", provider, sigError);
            return (false, new WebhookEventReceiptDto
            {
                Received = false,
                Provider = provider,
                Message = sigError ?? "Invalid signature."
            }, sigError);
        }

        // 2. Parse Event Metadata
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawPayload);
        }
        catch (Exception ex)
        {
            return (false, new WebhookEventReceiptDto { Received = false, Provider = provider, Message = "Malformed JSON payload." }, ex.Message);
        }

        if (root == null)
        {
            return (false, new WebhookEventReceiptDto { Received = false, Provider = provider, Message = "Empty JSON payload." }, "Empty payload.");
        }

        var eventId = root["id"]?.ToString() ?? root["eventId"]?.ToString();
        var eventType = root["type"]?.ToString() ?? root["eventType"]?.ToString();

        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(eventType))
        {
            return (false, new WebhookEventReceiptDto { Received = false, Provider = provider, Message = "Missing 'id' or 'type' in webhook event." }, "Missing id or type.");
        }

        // 3. Deduplication Check
        var existingEvent = await _context.PaymentWebhookEvents
            .FirstOrDefaultAsync(w => w.Provider == provider && w.EventId == eventId, ct);

        if (existingEvent != null && existingEvent.IsProcessed)
        {
            _logger.LogInformation("Webhook event {EventId} ({EventType}) already processed. Deduplicating safely.", eventId, eventType);
            return (true, new WebhookEventReceiptDto
            {
                Received = true,
                Provider = provider,
                EventId = eventId,
                EventType = eventType,
                IsDuplicate = true,
                Message = "Event previously processed and deduplicated."
            }, null);
        }

        var webhookRecord = existingEvent ?? new PaymentWebhookEvent
        {
            Provider = provider,
            EventId = eventId,
            EventType = eventType,
            PayloadJson = rawPayload,
            SignatureHash = signatureHeader,
            IsProcessed = false
        };

        if (existingEvent == null)
        {
            _context.PaymentWebhookEvents.Add(webhookRecord);
        }

        // 4. Handle Specific Event Types
        var dataObj = root["data"]?["object"] ?? root["data"] ?? root;

        try
        {
            switch (eventType.ToLowerInvariant())
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(dataObj, provider, eventId, ct);
                    break;

                case "invoice.payment_succeeded":
                case "invoice.paid":
                    await HandleInvoicePaymentSucceededAsync(dataObj, provider, eventId, ct);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(dataObj, provider, eventId, ct);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(dataObj, provider, eventId, ct);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type {EventType}. Recorded without state changes.", eventType);
                    break;
            }

            webhookRecord.IsProcessed = true;
            webhookRecord.ProcessedAtUtc = DateTime.UtcNow;
            webhookRecord.ErrorMessage = null;

            await _context.SaveChangesAsync(ct);

            await _auditService.RecordAsync(
                action: "Payment.WebhookProcessed",
                targetEntityType: "PaymentWebhookEvent",
                targetEntityId: eventId,
                outcome: "Success",
                details: $"Webhook event {eventId} of type '{eventType}' processed from {provider}.",
                cancellationToken: ct
            );

            return (true, new WebhookEventReceiptDto
            {
                Received = true,
                Provider = provider,
                EventId = eventId,
                EventType = eventType,
                IsDuplicate = false,
                Message = "Webhook event processed successfully."
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook event {EventId} ({EventType})", eventId, eventType);
            webhookRecord.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(ct);
            return (false, new WebhookEventReceiptDto { Received = false, Provider = provider, EventId = eventId, EventType = eventType, Message = ex.Message }, ex.Message);
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(JsonNode dataObj, string provider, string eventId, CancellationToken ct)
    {
        var orgIdStr = dataObj["metadata"]?["organizationId"]?.ToString()
                       ?? dataObj["organizationId"]?.ToString()
                       ?? dataObj["client_reference_id"]?.ToString();

        if (!Guid.TryParse(orgIdStr, out var orgId)) return;

        var targetTier = dataObj["metadata"]?["tier"]?.ToString() ?? dataObj["tier"]?.ToString() ?? "Pro";
        var providerSubId = dataObj["subscription"]?.ToString() ?? dataObj["subscriptionId"]?.ToString();

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.Organization)
            .ThenInclude(o => o.Settings)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub == null) return;

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Tier.ToLower() == targetTier.ToLower() && p.IsActive, ct);

        if (plan == null && targetTier.Equals("Pro", StringComparison.OrdinalIgnoreCase))
        {
            plan = new SubscriptionPlan
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Tier = "Pro",
                Name = "Professional Plan",
                Description = "Full-featured observability, distributed tracing, and statistical anomaly detection for growing engineering organizations.",
                IsActive = true,
                MonthlyPriceUsd = 49m,
                AnnualPriceUsd = 490m,
                MaxApplications = 20,
                MaxMembers = 25,
                MaxCustomDashboards = 15,
                MaxMonthlyLogs = 1_000_000,
                MaxMonthlyTraces = 500_000,
                MaxMonthlyMetrics = 1_000_000,
                RetentionDays = 30,
                MaxMonthlyAiTokens = 100_000,
                AllowCustomReports = true,
                AllowSloAlerts = true,
                AllowApiAccess = true,
                AllowDedicatedSupport = false
            };
            _context.SubscriptionPlans.Add(plan);
        }

        if (plan != null)
        {
            sub.PlanId = plan.Id;
            sub.Plan = plan;
            if (sub.Organization.Settings != null)
            {
                sub.Organization.Settings.PlanTier = plan.Tier;
                sub.Organization.Settings.MaxApplications = plan.MaxApplications;
                sub.Organization.Settings.MaxMembers = plan.MaxMembers;
                sub.Organization.Settings.MaxCustomDashboards = plan.MaxCustomDashboards;
                sub.Organization.Settings.MaxMonthlyLogs = plan.MaxMonthlyLogs;
                sub.Organization.Settings.MaxMonthlyTraces = plan.MaxMonthlyTraces;
                sub.Organization.Settings.DefaultRetentionDays = plan.RetentionDays;
                sub.Organization.Settings.MaxMonthlyAiTokens = plan.MaxMonthlyAiTokens;
            }
        }

        if (!string.IsNullOrEmpty(providerSubId))
        {
            sub.PaymentProviderSubscriptionId = providerSubId;
        }

        if (sub.Status != SubscriptionStatus.Active)
        {
            sub.TransitionTo(SubscriptionStatus.Active, $"Activated via checkout webhook from {provider}", "Webhook");
        }

        TrackHistories(sub);
    }

    private async Task HandleInvoicePaymentSucceededAsync(JsonNode dataObj, string provider, string eventId, CancellationToken ct)
    {
        var invoiceNumber = dataObj["number"]?.ToString() ?? dataObj["invoiceNumber"]?.ToString();
        var providerInvoiceId = dataObj["id"]?.ToString();
        var orgIdStr = dataObj["metadata"]?["organizationId"]?.ToString() ?? dataObj["organizationId"]?.ToString();

        SaaSInvoice? invoice = null;

        if (!string.IsNullOrEmpty(providerInvoiceId))
        {
            invoice = await _context.Invoices
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.PaymentProviderInvoiceId == providerInvoiceId, ct);
        }

        if (invoice == null && !string.IsNullOrEmpty(invoiceNumber))
        {
            invoice = await _context.Invoices
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);
        }

        if (invoice == null && Guid.TryParse(orgIdStr, out var orgId))
        {
            invoice = await _context.Invoices
                .Include(i => i.Transactions)
                .OrderByDescending(i => i.CreatedAtUtc)
                .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Status == InvoiceStatus.Open, ct);
        }

        if (invoice == null) return;

        var amountPaid = dataObj["amount_paid"]?.GetValue<decimal>() ?? invoice.AmountDue;
        if (amountPaid > 1000 && invoice.AmountDue <= 1000)
        {
            // Some providers send amounts in cents (e.g. 4900 -> 49.00)
            amountPaid = amountPaid / 100m;
        }

        var txn = new PaymentTransaction
        {
            OrganizationId = invoice.OrganizationId,
            InvoiceId = invoice.Id,
            Status = PaymentTransactionStatus.Succeeded,
            Amount = amountPaid > 0 ? amountPaid : invoice.AmountDue,
            Currency = invoice.Currency,
            PaymentProvider = provider,
            PaymentProviderTransactionId = eventId,
            SettledReason = $"Automated webhook payment confirmation ({provider})"
        };

        invoice.Transactions.Add(txn);
        _context.PaymentTransactions.Add(txn);
        invoice.AmountPaid += txn.Amount;

        if (invoice.AmountPaid >= invoice.AmountDue)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAtUtc = DateTime.UtcNow;
        }
        invoice.UpdatedAtUtc = DateTime.UtcNow;

        // If subscription is in PastDue or GracePeriod, recover it to Active
        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == invoice.OrganizationId, ct);

        if (sub != null && (sub.Status == SubscriptionStatus.PastDue || sub.Status == SubscriptionStatus.GracePeriod))
        {
            sub.TransitionTo(SubscriptionStatus.Active, "Subscription recovered after successful payment confirmation.", "Webhook");
            sub.GracePeriodEndsAtUtc = null;
            TrackHistories(sub);
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(JsonNode dataObj, string provider, string eventId, CancellationToken ct)
    {
        var orgIdStr = dataObj["metadata"]?["organizationId"]?.ToString() ?? dataObj["organizationId"]?.ToString();
        if (!Guid.TryParse(orgIdStr, out var orgId)) return;

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub != null && sub.Status == SubscriptionStatus.Active)
        {
            sub.TransitionTo(SubscriptionStatus.PastDue, $"Payment failed on external provider {provider}.", "Webhook");
            sub.GracePeriodEndsAtUtc = DateTime.UtcNow.AddDays(7);
            TrackHistories(sub);

            // Queue notification outbox
            _context.NotificationOutboxes.Add(new NotificationOutbox
            {
                EventType = NotificationEventType.SystemCritical,
                Subject = "Action Required: Payment Failed for SentinelOps Subscription",
                Message = $"A recurring payment attempt failed for your organization. Please update your payment method within 7 days to avoid service interruption.",
                Status = NotificationStatus.Pending,
                RetryCount = 0,
                PayloadJson = "{}"
            });
        }
    }

    private async Task HandleSubscriptionDeletedAsync(JsonNode dataObj, string provider, string eventId, CancellationToken ct)
    {
        var orgIdStr = dataObj["metadata"]?["organizationId"]?.ToString() ?? dataObj["organizationId"]?.ToString();
        if (!Guid.TryParse(orgIdStr, out var orgId)) return;

        var sub = await _context.OrganizationSubscriptions
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        if (sub != null && sub.Status != SubscriptionStatus.Canceled)
        {
            sub.TransitionTo(SubscriptionStatus.Canceled, $"Subscription terminated by external provider {provider}.", "Webhook");
            TrackHistories(sub);
        }
    }

    private void TrackHistories(OrganizationSubscription sub)
    {
        if (_context is DbContext db)
        {
            foreach (var h in sub.History)
            {
                var entry = db.Entry(h);
                if (entry.State == EntityState.Modified || entry.State == EntityState.Detached)
                {
                    entry.State = EntityState.Added;
                }
            }
        }
    }
}
