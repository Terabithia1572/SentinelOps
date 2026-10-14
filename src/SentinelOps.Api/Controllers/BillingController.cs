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

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
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

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
