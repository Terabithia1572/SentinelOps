using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/status")]
[AllowAnonymous]
public class PublicStatusController : ControllerBase
{
    private readonly IStatusPageService _statusPageService;

    public PublicStatusController(IStatusPageService statusPageService)
    {
        _statusPageService = statusPageService;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<PublicStatusPageDto>>> GetPublicStatus(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(ApiResponse<PublicStatusPageDto>.FailureResponse("Status page slug is required."));
        }

        var statusPage = await _statusPageService.GetPublicStatusPageBySlugAsync(slug, ct);
        if (statusPage == null)
        {
            return NotFound(ApiResponse<PublicStatusPageDto>.FailureResponse("Public status page not found or private."));
        }

        return Ok(ApiResponse<PublicStatusPageDto>.SuccessResponse(statusPage));
    }
}
