using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;

namespace SentinelOps.Application.Common.Interfaces;

public interface IReportingService
{
    List<ReportTypeOptionDto> GetAvailableReportTypes();

    Task<ReportPreviewDto> GenerateReportPreviewAsync(
        GenerateReportRequest request,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportCsvAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportPrintableReportAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportPdfAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default);
}
