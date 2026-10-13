namespace SentinelOps.Contracts.DTOs;

public class ReportPreviewDto
{
    public string ReportType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public Dictionary<string, object> SummaryMetrics { get; set; } = [];
    public List<string> ColumnHeaders { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}

public class ReportTypeOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
