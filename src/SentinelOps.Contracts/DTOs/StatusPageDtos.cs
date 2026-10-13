namespace SentinelOps.Contracts.DTOs;

public class StatusPageComponentDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? ServiceName { get; set; }
    public string? StatusOverride { get; set; } // "Operational", "Degraded Performance", "Partial Outage", "Major Outage", "Under Maintenance"
    public string ComputedStatus { get; set; } = "Operational";
}

public class StatusPageMaintenanceWindowDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, InProgress, Completed
    public List<string> AffectedComponentIds { get; set; } = new();
}

public class StatusPageIncidentUpdateDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Status { get; set; } = "Investigating"; // Investigating, Identified, Monitoring, Resolved
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string PostedBy { get; set; } = "Operator";
}

public class StatusPageIncidentDto
{
    public Guid Id { get; set; }
    public Guid StatusPageId { get; set; }
    public Guid? LinkedIncidentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Investigating";
    public string Severity { get; set; } = "Minor";
    public List<string> AffectedComponentIds { get; set; } = new();
    public List<StatusPageIncidentUpdateDto> Updates { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class StatusPageDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? HeaderNotice { get; set; }
    public bool IsPublic { get; set; }
    public bool AutoRollupEnabled { get; set; }
    public List<StatusPageComponentDto> Components { get; set; } = new();
    public List<StatusPageMaintenanceWindowDto> MaintenanceWindows { get; set; } = new();
    public int ActiveIncidentsCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class PublicStatusPageDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? HeaderNotice { get; set; }
    public string OverallStatus { get; set; } = "All Systems Operational"; // All Systems Operational, Degraded System Performance, Partial System Outage, Major System Outage, Scheduled Maintenance Active
    public List<StatusPageComponentDto> Components { get; set; } = new();
    public List<StatusPageIncidentDto> ActiveIncidents { get; set; } = new();
    public List<StatusPageIncidentDto> PastIncidents { get; set; } = new();
    public List<StatusPageMaintenanceWindowDto> ScheduledMaintenance { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }
}

public class CreateStatusPageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? HeaderNotice { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool AutoRollupEnabled { get; set; } = true;
    public List<StatusPageComponentDto>? Components { get; set; }
    public List<StatusPageMaintenanceWindowDto>? MaintenanceWindows { get; set; }
}

public class UpdateStatusPageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupportUrl { get; set; }
    public string? HeaderNotice { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool AutoRollupEnabled { get; set; } = true;
    public List<StatusPageComponentDto>? Components { get; set; }
    public List<StatusPageMaintenanceWindowDto>? MaintenanceWindows { get; set; }
}

public class CreateStatusPageIncidentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Investigating";
    public string Severity { get; set; } = "Minor";
    public Guid? LinkedIncidentId { get; set; }
    public List<string>? AffectedComponentIds { get; set; }
    public string InitialMessage { get; set; } = string.Empty;
}

public class AddStatusPageIncidentUpdateRequest
{
    public string Status { get; set; } = "Investigating";
    public string Message { get; set; } = string.Empty;
}
