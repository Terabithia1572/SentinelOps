using Microsoft.AspNetCore.Mvc;
using SentinelOps.Api.Authentication;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Services;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Otlp;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[RequireApiKey]
[RequestSizeLimit(10 * 1024 * 1024)] // 10 MB payload limit
public class OtlpIngestionController : ControllerBase
{
    private readonly ITelemetryBuffer _buffer;
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<OtlpIngestionController> _logger;

    public OtlpIngestionController(
        ITelemetryBuffer buffer,
        IEntitlementService entitlementService,
        ILogger<OtlpIngestionController> logger)
    {
        _buffer = buffer;
        _entitlementService = entitlementService;
        _logger = logger;
    }

    [HttpPost("v1/logs")]
    [HttpPost("api/v1/logs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> IngestLogs(
        [FromBody] OtlpLogsPayload payload,
        CancellationToken cancellationToken)
    {
        var orgId = GetAuthenticatedOrganizationId();
        if (orgId.HasValue)
        {
            var (allowed, reason) = await _entitlementService.CheckFeatureAccessAsync(orgId.Value, EntitlementFeature.IngestLogs, cancellationToken);
            if (!allowed)
            {
                _logger.LogWarning("Log ingestion rejected for tenant {OrgId}: {Reason}", orgId.Value, reason);
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.Fail($"Tenant quota exceeded: {reason}"));
            }
        }

        var applicationId = GetAuthenticatedApplicationId();
        var messages = OtlpPayloadMapper.MapLogs(payload, applicationId);

        if (messages.Count > 0)
        {
            await _buffer.PushLogsAsync(messages, cancellationToken);
            if (orgId.HasValue)
            {
                await _entitlementService.RecordUsageAsync(orgId.Value, MeteredMetric.Logs, messages.Count, cancellationToken);
            }
            _logger.LogDebug("Ingested {Count} log records for application {ApplicationId}", messages.Count, applicationId);
        }

        return Ok(new { });
    }

    [HttpPost("v1/traces")]
    [HttpPost("api/v1/traces")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> IngestTraces(
        [FromBody] OtlpTracesPayload payload,
        CancellationToken cancellationToken)
    {
        var orgId = GetAuthenticatedOrganizationId();
        if (orgId.HasValue)
        {
            var (allowed, reason) = await _entitlementService.CheckFeatureAccessAsync(orgId.Value, EntitlementFeature.IngestTraces, cancellationToken);
            if (!allowed)
            {
                _logger.LogWarning("Trace ingestion rejected for tenant {OrgId}: {Reason}", orgId.Value, reason);
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.Fail($"Tenant quota exceeded: {reason}"));
            }
        }

        var applicationId = GetAuthenticatedApplicationId();
        var traces = OtlpPayloadMapper.MapTraces(payload, applicationId);

        if (traces.Count > 0)
        {
            await _buffer.PushTracesAsync(traces, cancellationToken);
            if (orgId.HasValue)
            {
                await _entitlementService.RecordUsageAsync(orgId.Value, MeteredMetric.Traces, traces.Count, cancellationToken);
            }
            _logger.LogDebug("Ingested {Count} traces for application {ApplicationId}", traces.Count, applicationId);
        }

        return Ok(new { });
    }

    [HttpPost("v1/metrics")]
    [HttpPost("api/v1/metrics")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> IngestMetrics(
        [FromBody] OtlpMetricsPayload payload,
        CancellationToken cancellationToken)
    {
        var orgId = GetAuthenticatedOrganizationId();
        if (orgId.HasValue)
        {
            var (allowed, reason) = await _entitlementService.CheckFeatureAccessAsync(orgId.Value, EntitlementFeature.IngestMetrics, cancellationToken);
            if (!allowed)
            {
                _logger.LogWarning("Metric ingestion rejected for tenant {OrgId}: {Reason}", orgId.Value, reason);
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.Fail($"Tenant quota exceeded: {reason}"));
            }
        }

        var applicationId = GetAuthenticatedApplicationId();
        var metrics = OtlpPayloadMapper.MapMetrics(payload, applicationId);

        if (metrics.Count > 0)
        {
            await _buffer.PushMetricsAsync(metrics, cancellationToken);
            if (orgId.HasValue)
            {
                await _entitlementService.RecordUsageAsync(orgId.Value, MeteredMetric.Metrics, metrics.Count, cancellationToken);
            }
            _logger.LogDebug("Ingested {Count} metric points for application {ApplicationId}", metrics.Count, applicationId);
        }

        return Ok(new { });
    }

    private Guid GetAuthenticatedApplicationId()
    {
        if (HttpContext.Items.TryGetValue(ApiKeyAuthFilter.ApplicationIdItemKey, out var idObj) &&
            idObj is Guid applicationId)
        {
            return applicationId;
        }

        throw new UnauthorizedAccessException("Application context not established for ingestion.");
    }

    private Guid? GetAuthenticatedOrganizationId()
    {
        if (HttpContext.Items.TryGetValue(ApiKeyAuthFilter.OrganizationIdItemKey, out var idObj) &&
            idObj is Guid orgId && orgId != Guid.Empty)
        {
            return orgId;
        }

        return null;
    }
}
