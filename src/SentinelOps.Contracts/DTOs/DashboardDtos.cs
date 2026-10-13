namespace SentinelOps.Contracts.DTOs;

public class DashboardSummaryItemDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public string? CreatedByUserName { get; set; }
    public int RefreshIntervalSeconds { get; set; }
    public string DefaultTimeRange { get; set; } = "1h";
    public List<string> Tags { get; set; } = [];
    public int WidgetCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class DashboardDetailDto : DashboardSummaryItemDto
{
    public string LayoutJson { get; set; } = "[]";
    public List<DashboardWidgetDto> Widgets { get; set; } = [];
}

public class DashboardWidgetDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; } = 4;
    public int H { get; set; } = 3;
    public string ConfigJson { get; set; } = "{}";
}

public class CreateDashboardRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public string DefaultTimeRange { get; set; } = "1h";
    public List<string>? Tags { get; set; }
    public string? LayoutJson { get; set; }
}

public class UpdateDashboardRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public string DefaultTimeRange { get; set; } = "1h";
    public List<string>? Tags { get; set; }
    public string? LayoutJson { get; set; }
}

public class ImportDashboardRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LayoutJson { get; set; } = "[]";
    public List<string>? Tags { get; set; }
}
