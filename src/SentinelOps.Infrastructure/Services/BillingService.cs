using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly IApplicationDbContext _context;
    private readonly ITaxCalculationService _taxService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IApplicationDbContext context,
        ITaxCalculationService taxService,
        IAuditService auditService,
        ILogger<BillingService> logger)
    {
        _context = context;
        _taxService = taxService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<BillingAccountDto?> GetBillingAccountAsync(Guid orgId, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null) return null;

        var account = await _context.BillingAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.OrganizationId == orgId, ct);

        if (account == null)
        {
            var org = await _context.Organizations.FindAsync([orgId], ct);
            if (org == null) return null;

            account = new BillingAccount
            {
                OrganizationId = orgId,
                BillingEmail = "billing@example.com",
                CompanyName = org.Name,
                Currency = "USD"
            };
            _context.BillingAccounts.Add(account);
            await _context.SaveChangesAsync(ct);
        }

        return MapAccountToDto(account);
    }

    public async Task<(bool Success, BillingAccountDto? Account, string? Error)> UpdateBillingAccountAsync(
        Guid orgId,
        UpdateBillingAccountRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null || (member.Role != OrganizationRole.Owner && member.Role != OrganizationRole.Admin))
        {
            return (false, null, "Only an organization Owner or Admin can update billing information.");
        }

        var account = await _context.BillingAccounts
            .FirstOrDefaultAsync(b => b.OrganizationId == orgId, ct);

        if (account == null)
        {
            account = new BillingAccount { OrganizationId = orgId };
            _context.BillingAccounts.Add(account);
        }

        account.BillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail) ? account.BillingEmail : request.BillingEmail.Trim();
        account.CompanyName = request.CompanyName?.Trim();
        account.TaxId = request.TaxId?.Trim();
        account.BillingAddress = request.BillingAddress?.Trim();
        account.BillingCity = request.BillingCity?.Trim();
        account.BillingCountry = request.BillingCountry?.Trim();
        account.BillingPostalCode = request.BillingPostalCode?.Trim();
        account.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "BillingAccount.Updated",
            targetEntityType: "BillingAccount",
            targetEntityId: account.Id.ToString(),
            outcome: "Success",
            details: $"Billing contact and tax information updated for organization {orgId}.",
            cancellationToken: ct
        );

        return (true, MapAccountToDto(account), null);
    }

    public async Task<IEnumerable<SaaSInvoiceDto>> GetInvoicesAsync(Guid orgId, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null) return Enumerable.Empty<SaaSInvoiceDto>();

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Transactions.OrderByDescending(t => t.CreatedAtUtc))
            .Where(i => i.OrganizationId == orgId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(ct);

        return invoices.Select(MapInvoiceToDto);
    }

    public async Task<SaaSInvoiceDto?> GetInvoiceByIdAsync(Guid orgId, Guid invoiceId, string userId, CancellationToken ct = default)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId, ct);

        if (member == null) return null;

        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.Transactions.OrderByDescending(t => t.CreatedAtUtc))
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.OrganizationId == orgId, ct);

        if (invoice == null) return null;

        return MapInvoiceToDto(invoice);
    }

    public async Task<SaaSInvoiceDto> GenerateInvoiceAsync(
        Guid orgId,
        string reason,
        List<InvoiceLineItemDto> items,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations
            .Include(o => o.BillingAccount)
            .Include(o => o.Subscription)
            .FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new InvalidOperationException($"Organization {orgId} not found.");

        var count = await _context.Invoices.CountAsync(ct) + 1;
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{count:D5}";

        var subtotal = items.Sum(i => i.Amount);
        var isExempt = org.BillingAccount?.IsExemptFromTax ?? false;
        var (_, taxAmount) = _taxService.CalculateTax(subtotal, org.BillingAccount?.BillingCountry, org.BillingAccount?.BillingPostalCode, isExempt);

        var totalDue = subtotal + taxAmount;

        var invoice = new SaaSInvoice
        {
            OrganizationId = orgId,
            SubscriptionId = org.Subscription?.Id,
            InvoiceNumber = invoiceNumber,
            Status = totalDue > 0 ? InvoiceStatus.Open : InvoiceStatus.Paid,
            AmountDue = totalDue,
            AmountPaid = totalDue > 0 ? 0m : 0m,
            Currency = org.BillingAccount?.Currency ?? "USD",
            PeriodStartUtc = DateTime.UtcNow,
            PeriodEndUtc = DateTime.UtcNow.AddMonths(1),
            DueDateUtc = DateTime.UtcNow.AddDays(14),
            BillingReason = reason,
            PaidAtUtc = totalDue == 0 ? DateTime.UtcNow : null
        };

        foreach (var item in items)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                InvoiceId = invoice.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            });
        }

        if (taxAmount > 0)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                InvoiceId = invoice.Id,
                Description = $"Tax / VAT ({org.BillingAccount?.BillingCountry ?? "Standard"})",
                Quantity = 1,
                UnitPrice = taxAmount,
                Amount = taxAmount
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(ct);

        return MapInvoiceToDto(invoice);
    }

    public async Task<(bool Success, string? Error)> SettleInvoiceAdministrativelyAsync(
        Guid orgId,
        Guid invoiceId,
        SettleInvoiceRequest request,
        string adminUserId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return (false, "A mandatory audit reason is required for administrative settlement.");
        }

        var invoice = await _context.Invoices
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.OrganizationId == orgId, ct);

        if (invoice == null)
        {
            return (false, "Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return (false, "Invoice is already paid.");
        }

        var amountToSettle = request.AmountSettled > 0 ? request.AmountSettled : invoice.AmountDue;

        var transaction = new PaymentTransaction
        {
            OrganizationId = orgId,
            InvoiceId = invoice.Id,
            Status = PaymentTransactionStatus.Succeeded,
            Amount = amountToSettle,
            Currency = invoice.Currency,
            PaymentProvider = "Manual",
            PaymentProviderTransactionId = string.IsNullOrWhiteSpace(request.PaymentReference)
                ? $"MANUAL-{Guid.NewGuid():N}"
                : request.PaymentReference.Trim(),
            SettledByUserId = adminUserId,
            SettledReason = request.Reason.Trim()
        };

        invoice.Transactions.Add(transaction);
        _context.PaymentTransactions.Add(transaction);
        invoice.AmountPaid += amountToSettle;

        if (invoice.AmountPaid >= invoice.AmountDue)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAtUtc = DateTime.UtcNow;
        }

        invoice.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.RecordAsync(
            action: "Invoice.SettledAdministratively",
            targetEntityType: "SaaSInvoice",
            targetEntityId: invoice.Id.ToString(),
            outcome: "Success",
            details: $"Invoice {invoice.InvoiceNumber} settled manually by admin {adminUserId}. Reason: {request.Reason}. Amount: {amountToSettle} {invoice.Currency}.",
            cancellationToken: ct
        );

        return (true, null);
    }

    private static BillingAccountDto MapAccountToDto(BillingAccount a) => new()
    {
        Id = a.Id,
        OrganizationId = a.OrganizationId,
        BillingEmail = a.BillingEmail,
        CompanyName = a.CompanyName,
        TaxId = a.TaxId,
        Currency = a.Currency,
        BillingAddress = a.BillingAddress,
        BillingCity = a.BillingCity,
        BillingCountry = a.BillingCountry,
        BillingPostalCode = a.BillingPostalCode,
        PaymentMethodSummary = a.PaymentMethodSummary,
        IsExemptFromTax = a.IsExemptFromTax
    };

    private static SaaSInvoiceDto MapInvoiceToDto(SaaSInvoice i) => new()
    {
        Id = i.Id,
        OrganizationId = i.OrganizationId,
        InvoiceNumber = i.InvoiceNumber,
        Status = i.Status.ToString(),
        AmountDue = i.AmountDue,
        AmountPaid = i.AmountPaid,
        Currency = i.Currency,
        PeriodStartUtc = i.PeriodStartUtc,
        PeriodEndUtc = i.PeriodEndUtc,
        DueDateUtc = i.DueDateUtc,
        PaidAtUtc = i.PaidAtUtc,
        BillingReason = i.BillingReason,
        LineItems = i.LineItems.Select(li => new InvoiceLineItemDto
        {
            Id = li.Id,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            Amount = li.Amount
        }).ToList(),
        Transactions = i.Transactions.Select(t => new PaymentTransactionDto
        {
            Id = t.Id,
            Status = t.Status.ToString(),
            Amount = t.Amount,
            Currency = t.Currency,
            PaymentProvider = t.PaymentProvider,
            PaymentProviderTransactionId = t.PaymentProviderTransactionId,
            FailureReason = t.FailureReason,
            SettledByUserId = t.SettledByUserId,
            SettledReason = t.SettledReason,
            CreatedAtUtc = t.CreatedAtUtc
        }).ToList()
    };
}
