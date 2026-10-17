using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class SavedLogQuery : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SearchTerm { get; set; }
    public string? Severity { get; set; }
    public string? ServiceName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? TraceId { get; set; }
    public string? PropertyFiltersJson { get; set; }
    public string TimeRange { get; set; } = "1h";
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public bool IsShared { get; set; } = true;
    public string TagsJson { get; set; } = "[]";

    public SavedLogQuery()
    {
    }

    public SavedLogQuery(
        Guid organizationId,
        string name,
        string? description = null,
        Guid? applicationId = null,
        string? searchTerm = null,
        string? severity = null,
        string? serviceName = null,
        string? environmentName = null,
        string? traceId = null,
        string? propertyFiltersJson = null,
        string timeRange = "1h",
        string? createdByUserId = null,
        string? createdByUserName = null,
        bool isShared = true,
        string tagsJson = "[]")
    {
        OrganizationId = organizationId;
        Name = name;
        Description = description;
        ApplicationId = applicationId;
        SearchTerm = searchTerm;
        Severity = severity;
        ServiceName = serviceName;
        EnvironmentName = environmentName;
        TraceId = traceId;
        PropertyFiltersJson = propertyFiltersJson;
        TimeRange = string.IsNullOrWhiteSpace(timeRange) ? "1h" : timeRange;
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName;
        IsShared = isShared;
        TagsJson = string.IsNullOrWhiteSpace(tagsJson) ? "[]" : tagsJson;
    }
}
