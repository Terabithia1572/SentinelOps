using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class IncidentRca : Entity
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public string IncidentSummary { get; set; } = string.Empty;
    public string ImpactedServicesJson { get; set; } = "[]";
    public string HypothesesJson { get; set; } = "[]";
    public string CorrelatedAnomaliesJson { get; set; } = "[]";
    public string CorrelatedTraceIdsJson { get; set; } = "[]";
    public string GeneratedByProvider { get; set; } = "Deterministic Heuristic Engine";
    public bool IsDeterministic { get; set; } = true;
    public string OperatorVerificationStatus { get; set; } = "Unverified"; // Unverified, Confirmed, Rejected
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? OperatorNotes { get; set; }

    public IncidentRca()
    {
    }

    public IncidentRca(
        Guid organizationId,
        Guid incidentId,
        string incidentSummary,
        string impactedServicesJson,
        string hypothesesJson,
        string correlatedAnomaliesJson,
        string correlatedTraceIdsJson,
        string generatedByProvider,
        bool isDeterministic)
    {
        OrganizationId = organizationId;
        IncidentId = incidentId;
        IncidentSummary = incidentSummary;
        ImpactedServicesJson = impactedServicesJson;
        HypothesesJson = hypothesesJson;
        CorrelatedAnomaliesJson = correlatedAnomaliesJson;
        CorrelatedTraceIdsJson = correlatedTraceIdsJson;
        GeneratedByProvider = generatedByProvider;
        IsDeterministic = isDeterministic;
        OperatorVerificationStatus = "Unverified";
    }

    public void Verify(string operatorName, bool isConfirmed, string? notes)
    {
        OperatorVerificationStatus = isConfirmed ? "Confirmed" : "Rejected";
        VerifiedBy = operatorName;
        VerifiedAtUtc = DateTime.UtcNow;
        OperatorNotes = notes;
    }
}
