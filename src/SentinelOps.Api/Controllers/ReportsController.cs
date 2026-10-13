using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireViewer")]
public class ReportsController : BaseApiController
{
    private readonly IReportingService _reportingService;
    private readonly IAuditService _auditService;

    public ReportsController(
        IReportingService reportingService,
        IAuditService auditService)
    {
        _reportingService = reportingService;
        _auditService = auditService;
    }

    [HttpGet("types")]
    [ProducesResponseType(typeof(ApiResponse<List<ReportTypeOptionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetReportTypes()
    {
        var types = _reportingService.GetAvailableReportTypes();
        return Ok(ApiResponse<List<ReportTypeOptionDto>>.SuccessResponse(types));
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ApiResponse<ReportPreviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewReport([FromBody] GenerateReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _reportingService.GenerateReportPreviewAsync(request, cancellationToken);
        return Ok(ApiResponse<ReportPreviewDto>.SuccessResponse(result));
    }

    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv([FromBody] ExportReportRequest request, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await _reportingService.ExportCsvAsync(request, cancellationToken);

        await _auditService.RecordAsync(
            action: "Report.ExportedCsv",
            targetEntityType: "Report",
            targetEntityId: request.ReportType,
            details: $"Exported CSV report for {request.ReportType}",
            metadata: new { request.ReportType, request.TimeRange, request.ApplicationId },
            cancellationToken: cancellationToken);

        return File(content, contentType, fileName);
    }

    [HttpPost("export/print")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPrintableReport([FromBody] ExportReportRequest request, CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) = await _reportingService.ExportPrintableReportAsync(request, cancellationToken);

        await _auditService.RecordAsync(
            action: "Report.ExportedPrintable",
            targetEntityType: "Report",
            targetEntityId: request.ReportType,
            details: $"Exported printable HTML report for {request.ReportType}",
            metadata: new { request.ReportType, request.TimeRange, request.ApplicationId },
            cancellationToken: cancellationToken);

        return File(content, contentType, fileName);
    }

    [HttpGet("export/pdf")]
    [HttpPost("export/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? type,
        [FromQuery] string? timeRange,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? appId,
        [FromBody] ExportReportRequest? bodyRequest,
        CancellationToken cancellationToken)
    {
        var request = bodyRequest ?? new ExportReportRequest
        {
            ReportType = type ?? "ExecutiveSummary",
            TimeRange = timeRange ?? "Last24Hours",
            StartUtc = from,
            EndUtc = to,
            ApplicationId = appId
        };

        var (content, contentType, fileName) = await _reportingService.ExportPdfAsync(request, cancellationToken);

        await _auditService.RecordAsync(
            action: "Report.ExportedPdf",
            targetEntityType: "Report",
            targetEntityId: request.ReportType,
            details: $"Exported PDF report for {request.ReportType}",
            metadata: new { request.ReportType, request.TimeRange, request.ApplicationId },
            cancellationToken: cancellationToken);

        return File(content, contentType, fileName);
    }
}
