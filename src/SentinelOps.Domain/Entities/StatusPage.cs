using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class StatusPage : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? HeaderNotice { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool AutoRollupEnabled { get; set; } = true;

    /// <summary>
    /// JSON array of StatusPageComponent items: { id, name, description, applicationId, serviceName, statusOverride }
    /// </summary>
    public string ComponentsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of scheduled maintenance items: { id, title, startTimeUtc, endTimeUtc, status, affectedComponentIds, description }
    /// </summary>
    public string MaintenanceWindowsJson { get; set; } = "[]";

    public ICollection<StatusPageIncident> Incidents { get; set; } = new List<StatusPageIncident>();

    public StatusPage()
    {
    }

    public StatusPage(
        Guid organizationId,
        string name,
        string slug,
        string? description = null,
        string? logoUrl = null,
        string? supportUrl = null,
        bool isPublic = true,
        bool autoRollupEnabled = true,
        string componentsJson = "[]",
        string maintenanceWindowsJson = "[]")
    {
        OrganizationId = organizationId;
        Name = name;
        Slug = slug.Trim().ToLowerInvariant();
        Description = description;
        LogoUrl = logoUrl;
        SupportUrl = supportUrl;
        IsPublic = isPublic;
        AutoRollupEnabled = autoRollupEnabled;
        ComponentsJson = string.IsNullOrWhiteSpace(componentsJson) ? "[]" : componentsJson;
        MaintenanceWindowsJson = string.IsNullOrWhiteSpace(maintenanceWindowsJson) ? "[]" : maintenanceWindowsJson;
    }
}
