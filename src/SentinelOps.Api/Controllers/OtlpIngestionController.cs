using Microsoft.AspNetCore.Mvc;
using SentinelOps.Api.Authentication;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Services;
using SentinelOps.Contracts.Otlp;

namespace SentinelOps.Api.Controllers;

[ApiController]
[RequireApiKey]
[RequestSizeLimit(10 * 1024 * 1024)] // 10 MB payload limit
public class OtlpIngestionController : ControllerBase
{
    private readonly ITelemetryBuffer _buffer;
    private readonly ILogger<OtlpIngestionController> _logger;

    public OtlpIngestionController(
        ITelemetryBuffer buffer,
        ILogger<OtlpIngestionController> logger)
    {
        _buffer = buffer;
        _logger = logger;
    }

    [HttpPost("v1/logs")]
    [HttpPost("api/v1/logs")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestLogs(
        [FromBody] OtlpLogsPayload payload,
        CancellationToken cancellationToken)
    {
        var applicationId = GetAuthenticatedApplicationId();
        var messages = OtlpPayloadMapper.MapLogs(payload, applicationId);

        if (messages.Count > 0)
        {
            await _buffer.PushLogsAsync(messages, cancellationToken);
            _logger.LogDebug("Ingested {Count} log records for application {ApplicationId}", messages.Count, applicationId);
        }

        return Ok(new { });
    }

    [HttpPost("v1/traces")]
    [HttpPost("api/v1/traces")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestTraces(
        [FromBody] OtlpTracesPayload payload,
        CancellationToken cancellationToken)
    {
        var applicationId = GetAuthenticatedApplicationId();
        var traces = OtlpPayloadMapper.MapTraces(payload, applicationId);

        if (traces.Count > 0)
        {
            await _buffer.PushTracesAsync(traces, cancellationToken);
            _logger.LogDebug("Ingested {Count} traces for application {ApplicationId}", traces.Count, applicationId);
        }

        return Ok(new { });
    }

    [HttpPost("v1/metrics")]
    [HttpPost("api/v1/metrics")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestMetrics(
        [FromBody] OtlpMetricsPayload payload,
        CancellationToken cancellationToken)
    {
        var applicationId = GetAuthenticatedApplicationId();
        var metrics = OtlpPayloadMapper.MapMetrics(payload, applicationId);

        if (metrics.Count > 0)
        {
            await _buffer.PushMetricsAsync(metrics, cancellationToken);
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
}
