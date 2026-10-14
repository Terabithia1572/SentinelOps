using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Payments;

public class SandboxPaymentProvider : IPaymentProvider
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<SandboxPaymentProvider> _logger;

    public string ProviderName => "Sandbox";
    public bool IsConfigured => true;

    public SandboxPaymentProvider(
        IApplicationDbContext context,
        IAuditService auditService,
        ILogger<SandboxPaymentProvider> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(bool Success, CheckoutSessionDto? Session, string? Error)> CreateCheckoutSessionAsync(
        Guid orgId,
        string userEmail,
        CreateCheckoutSessionRequest request,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        if (org == null)
        {
            return (false, null, "Organization not found.");
        }

        var plan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Tier.ToLower() == request.TargetTier.ToLower() && p.IsActive, ct);

        if (plan == null)
        {
            return (false, null, $"Plan tier '{request.TargetTier}' does not exist or is inactive.");
        }

        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var checkoutUrl = string.IsNullOrWhiteSpace(request.SuccessUrl)
            ? $"https://checkout.sentinelops.local/session/{sessionId}"
            : $"{request.SuccessUrl}?session_id={sessionId}";

        var session = new CheckoutSessionDto
        {
            SessionId = sessionId,
            CheckoutUrl = checkoutUrl,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(24),
            Provider = ProviderName
        };

        _logger.LogInformation("Sandbox checkout session created {SessionId} for Org {OrgId}, Plan {Tier}",
            sessionId, orgId, plan.Tier);

        return (true, session, null);
    }

    public async Task<(bool Success, CustomerPortalSessionDto? Session, string? Error)> CreateCustomerPortalSessionAsync(
        Guid orgId,
        CreatePortalSessionRequest request,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        if (org == null)
        {
            return (false, null, "Organization not found.");
        }

        var portalUrl = string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? $"https://billing.sentinelops.local/portal/{orgId}"
            : $"{request.ReturnUrl}?portal_org_id={orgId}";

        return (true, new CustomerPortalSessionDto
        {
            PortalUrl = portalUrl,
            Provider = ProviderName
        }, null);
    }

    public async Task<RefundResultDto> RefundTransactionAsync(
        Guid orgId,
        string transactionId,
        RefundTransactionRequest request,
        CancellationToken ct = default)
    {
        var txn = await _context.PaymentTransactions
            .Include(t => t.Invoice)
            .FirstOrDefaultAsync(t => (t.PaymentProviderTransactionId == transactionId || t.Id.ToString() == transactionId) && t.OrganizationId == orgId, ct);

        if (txn == null)
        {
            return new RefundResultDto
            {
                Success = false,
                Error = "Transaction not found for this organization."
            };
        }

        if (txn.Status != PaymentTransactionStatus.Succeeded)
        {
            return new RefundResultDto
            {
                Success = false,
                Error = $"Cannot refund transaction with status '{txn.Status}'."
            };
        }

        var refundAmount = request.Amount ?? txn.Amount;
        if (refundAmount <= 0 || refundAmount > txn.Amount)
        {
            return new RefundResultDto
            {
                Success = false,
                Error = $"Invalid refund amount {refundAmount}. Maximum refundable amount is {txn.Amount}."
            };
        }

        var refundId = $"re_test_{Guid.NewGuid():N}";

        var refundTxn = new PaymentTransaction
        {
            OrganizationId = orgId,
            InvoiceId = txn.InvoiceId,
            Status = PaymentTransactionStatus.Refunded,
            Amount = -refundAmount,
            Currency = txn.Currency,
            PaymentProvider = ProviderName,
            PaymentProviderTransactionId = refundId,
            SettledReason = string.IsNullOrWhiteSpace(request.Reason) ? "Customer refund" : request.Reason.Trim()
        };

        _context.PaymentTransactions.Add(refundTxn);

        if (txn.Invoice != null)
        {
            txn.Invoice.AmountPaid = Math.Max(0, txn.Invoice.AmountPaid - refundAmount);
            if (txn.Invoice.AmountPaid < txn.Invoice.AmountDue)
            {
                txn.Invoice.Status = InvoiceStatus.Open;
            }
            txn.Invoice.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Payment.Refunded",
            targetEntityType: "PaymentTransaction",
            targetEntityId: refundTxn.Id.ToString(),
            outcome: "Success",
            details: $"Refund of {refundAmount} {txn.Currency} processed for transaction {txn.PaymentProviderTransactionId}. Reason: {request.Reason}.",
            cancellationToken: ct
        );

        return new RefundResultDto
        {
            Success = true,
            RefundId = refundId,
            Amount = refundAmount,
            Currency = txn.Currency,
            Status = "Refunded"
        };
    }
}
