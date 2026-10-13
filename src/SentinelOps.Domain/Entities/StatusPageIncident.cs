using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class StatusPageIncident : Entity
{
    public Guid StatusPageId { get; set; }
    public StatusPage? StatusPage { get; set; }

    public Guid? LinkedIncidentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Investigating"; // Investigating, Identified, Monitoring, Resolved
    public string Severity { get; set; } = "Minor"; // Degraded, Minor, Major, Critical

    /// <summary>
    /// JSON array of string component IDs affected by this incident.
    /// </summary>
    public string AffectedComponentIdsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of updates: { id, status, message, createdAtUtc, postedBy }
    /// </summary>
    public string UpdatesJson { get; set; } = "[]";

    public DateTime? ResolvedAtUtc { get; set; }

    public StatusPageIncident()
    {
    }

    public StatusPageIncident(
        Guid statusPageId,
        string title,
        string status = "Investigating",
        string severity = "Minor",
        Guid? linkedIncidentId = null,
        string affectedComponentIdsJson = "[]",
        string updatesJson = "[]")
    {
        StatusPageId = statusPageId;
        Title = title;
        Status = status;
        Severity = severity;
        LinkedIncidentId = linkedIncidentId;
        AffectedComponentIdsJson = string.IsNullOrWhiteSpace(affectedComponentIdsJson) ? "[]" : affectedComponentIdsJson;
        UpdatesJson = string.IsNullOrWhiteSpace(updatesJson) ? "[]" : updatesJson;
    }
}
