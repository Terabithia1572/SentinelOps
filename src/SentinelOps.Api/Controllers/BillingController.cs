using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Constants;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IEntitlementService _entitlementService;

    public BillingController(
        IBillingService billingService,
        IPaymentProvider paymentProvider,
        IEntitlementService entitlementService)
    {
        _billingService = billingService;
        _paymentProvider = paymentProvider;
        _entitlementService = entitlementService;
    }

    [HttpGet("entitlements")]
    public async Task<ActionResult<ApiResponse<TenantEntitlementsDto>>> GetEntitlements(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<TenantEntitlementsDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<TenantEntitlementsDto>.Fail("Unauthorized"));

        var entitlements = await _entitlementService.GetEntitlementsAsync(orgId.Value, userId, ct);
        return Ok(ApiResponse<TenantEntitlementsDto>.Ok(entitlements));
    }

    [HttpGet("usage")]
    public async Task<ActionResult<ApiResponse<TenantUsageSummaryDto>>> GetUsage(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<TenantUsageSummaryDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<TenantUsageSummaryDto>.Fail("Unauthorized"));

        var usage = await _entitlementService.GetUsageSummaryAsync(orgId.Value, userId, ct);
        return Ok(ApiResponse<TenantUsageSummaryDto>.Ok(usage));
    }

    [HttpGet("account")]
    public async Task<ActionResult<ApiResponse<BillingAccountDto>>> GetAccount(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<BillingAccountDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<BillingAccountDto>.Fail("Unauthorized"));

        var account = await _billingService.GetBillingAccountAsync(orgId.Value, userId, ct);
        if (account == null)
            return NotFound(ApiResponse<BillingAccountDto>.Fail("Billing account not found."));

        return Ok(ApiResponse<BillingAccountDto>.Ok(account));
    }

    [HttpPut("account")]
    public async Task<ActionResult<ApiResponse<BillingAccountDto>>> UpdateAccount(
        [FromBody] UpdateBillingAccountRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<BillingAccountDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<BillingAccountDto>.Fail("Unauthorized"));

        var (success, account, error) = await _billingService.UpdateBillingAccountAsync(orgId.Value, request, userId, ct);
        if (!success || account == null)
            return BadRequest(ApiResponse<BillingAccountDto>.Fail(error ?? "Failed to update billing account."));

        return Ok(ApiResponse<BillingAccountDto>.Ok(account, "Billing account updated successfully."));
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SaaSInvoiceDto>>>> GetInvoices(
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<IEnumerable<SaaSInvoiceDto>>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<IEnumerable<SaaSInvoiceDto>>.Fail("Unauthorized"));

        var invoices = await _billingService.GetInvoicesAsync(orgId.Value, userId, ct);
        return Ok(ApiResponse<IEnumerable<SaaSInvoiceDto>>.Ok(invoices));
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<SaaSInvoiceDto>>> GetInvoice(
        Guid invoiceId,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<SaaSInvoiceDto>.Fail("Organization ID is required."));

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<SaaSInvoiceDto>.Fail("Unauthorized"));

        var invoice = await _billingService.GetInvoiceByIdAsync(orgId.Value, invoiceId, userId, ct);
        if (invoice == null)
            return NotFound(ApiResponse<SaaSInvoiceDto>.Fail("Invoice not found."));

        return Ok(ApiResponse<SaaSInvoiceDto>.Ok(invoice));
    }

    [HttpPost("invoices/{invoiceId:guid}/settle-admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<bool>>> SettleAdmin(
        Guid invoiceId,
        [FromBody] SettleInvoiceRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<bool>.Fail("Organization ID is required."));

        var adminUserId = GetCurrentUserId() ?? "SystemAdmin";

        var (success, error) = await _billingService.SettleInvoiceAdministrativelyAsync(orgId.Value, invoiceId, request, adminUserId, ct);
        if (!success)
            return BadRequest(ApiResponse<bool>.Fail(error ?? "Failed to settle invoice administratively."));

        return Ok(ApiResponse<bool>.Ok(true, "Invoice settled administratively."));
    }

    [HttpPost("checkout-session")]
    public async Task<ActionResult<ApiResponse<CheckoutSessionDto>>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<CheckoutSessionDto>.Fail("Organization ID is required."));

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "billing-user@sentinelops.local";
        var (success, session, error) = await _paymentProvider.CreateCheckoutSessionAsync(orgId.Value, userEmail, request, ct);
        if (!success || session == null)
            return BadRequest(ApiResponse<CheckoutSessionDto>.Fail(error ?? "Failed to create checkout session."));

        return Ok(ApiResponse<CheckoutSessionDto>.Ok(session));
    }

    [HttpPost("customer-portal")]
    public async Task<ActionResult<ApiResponse<CustomerPortalSessionDto>>> CreateCustomerPortal(
        [FromBody] CreatePortalSessionRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<CustomerPortalSessionDto>.Fail("Organization ID is required."));

        var (success, session, error) = await _paymentProvider.CreateCustomerPortalSessionAsync(orgId.Value, request, ct);
        if (!success || session == null)
            return BadRequest(ApiResponse<CustomerPortalSessionDto>.Fail(error ?? "Failed to create customer portal session."));

        return Ok(ApiResponse<CustomerPortalSessionDto>.Ok(session));
    }

    [HttpPost("transactions/{transactionId}/refund")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<RefundResultDto>>> RefundTransaction(
        string transactionId,
        [FromBody] RefundTransactionRequest request,
        [FromHeader(Name = "X-Organization-Id")] Guid? orgIdHeader,
        [FromQuery] Guid? organizationId,
        CancellationToken ct)
    {
        var orgId = orgIdHeader ?? organizationId;
        if (!orgId.HasValue || orgId.Value == Guid.Empty)
            return BadRequest(ApiResponse<RefundResultDto>.Fail("Organization ID is required."));

        var result = await _paymentProvider.RefundTransactionAsync(orgId.Value, transactionId, request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse<RefundResultDto>.Fail(result.Error ?? "Refund failed."));

        return Ok(ApiResponse<RefundResultDto>.Ok(result, "Transaction refund processed."));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
