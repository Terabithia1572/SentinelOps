using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class CustomDashboard : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public int RefreshIntervalSeconds { get; set; } = 30;
    public string DefaultTimeRange { get; set; } = "1h";
    public string TagsJson { get; set; } = "[]";
    public string LayoutJson { get; set; } = "[]";

    public CustomDashboard()
    {
    }

    public CustomDashboard(
        Guid organizationId,
        string title,
        string? description = null,
        bool isDefault = false,
        bool isShared = true,
        string? createdByUserId = null,
        string? createdByUserName = null,
        int refreshIntervalSeconds = 30,
        string defaultTimeRange = "1h",
        string tagsJson = "[]",
        string layoutJson = "[]")
    {
        OrganizationId = organizationId;
        Title = title;
        Description = description;
        IsDefault = isDefault;
        IsShared = isShared;
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName;
        RefreshIntervalSeconds = refreshIntervalSeconds;
        DefaultTimeRange = defaultTimeRange;
        TagsJson = string.IsNullOrWhiteSpace(tagsJson) ? "[]" : tagsJson;
        LayoutJson = string.IsNullOrWhiteSpace(layoutJson) ? "[]" : layoutJson;
    }
}
