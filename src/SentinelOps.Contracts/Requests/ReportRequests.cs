namespace SentinelOps.Contracts.Requests;

public class GenerateReportRequest
{
    public string ReportType { get; set; } = "Performance"; // Performance, ErrorsAndExceptions, IncidentHistory, AlertActivity, SloCompliance, ApplicationHealth
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public string? TimeRange { get; set; } // "24h", "7d", "30d", "custom"
    public Guid? ApplicationId { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ServiceName { get; set; }
}

public class ExportReportRequest : GenerateReportRequest
{
    public string Format { get; set; } = "csv"; // "csv", "pdf", "html"
}
