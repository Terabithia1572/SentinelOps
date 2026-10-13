using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApmController : ControllerBase
{
    private readonly IApmService _apmService;

    public ApmController(IApmService apmService)
    {
        _apmService = apmService;
    }

    [HttpGet("service-map")]
    public async Task<ActionResult<ApiResponse<ServiceMapDto>>> GetServiceMap(
        [FromQuery] string timeRange = "1h",
        [FromQuery] string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var map = await _apmService.GetServiceMapAsync(timeRange, environment, cancellationToken);
        return Ok(ApiResponse<ServiceMapDto>.SuccessResponse(map));
    }

    [HttpGet("services/{serviceName}/dependencies")]
    public async Task<ActionResult<ApiResponse<ServiceDependenciesDto>>> GetServiceDependencies(
        string serviceName,
        [FromQuery] string timeRange = "1h",
        CancellationToken cancellationToken = default)
    {
        var deps = await _apmService.GetServiceDependenciesAsync(serviceName, timeRange, cancellationToken);
        return Ok(ApiResponse<ServiceDependenciesDto>.SuccessResponse(deps));
    }

    [HttpGet("endpoints")]
    public async Task<ActionResult<ApiResponse<List<EndpointPerformanceDto>>>> GetEndpoints(
        [FromQuery] string timeRange = "1h",
        [FromQuery] string? service = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var endpoints = await _apmService.GetEndpointsAsync(timeRange, service, search, cancellationToken);
        return Ok(ApiResponse<List<EndpointPerformanceDto>>.SuccessResponse(endpoints));
    }

    [HttpGet("endpoints/{endpointKey}/breakdown")]
    public async Task<ActionResult<ApiResponse<EndpointBreakdownDto>>> GetEndpointBreakdown(
        string endpointKey,
        [FromQuery] string timeRange = "1h",
        CancellationToken cancellationToken = default)
    {
        var breakdown = await _apmService.GetEndpointBreakdownAsync(endpointKey, timeRange, cancellationToken);
        if (breakdown == null)
        {
            return NotFound(ApiResponse<EndpointBreakdownDto>.FailureResponse("Endpoint breakdown not found."));
        }
        return Ok(ApiResponse<EndpointBreakdownDto>.SuccessResponse(breakdown));
    }

    [HttpGet("external-dependencies")]
    public async Task<ActionResult<ApiResponse<List<ExternalDependencyDto>>>> GetExternalDependencies(
        [FromQuery] string timeRange = "1h",
        CancellationToken cancellationToken = default)
    {
        var externals = await _apmService.GetExternalDependenciesAsync(timeRange, cancellationToken);
        return Ok(ApiResponse<List<ExternalDependencyDto>>.SuccessResponse(externals));
    }
}
