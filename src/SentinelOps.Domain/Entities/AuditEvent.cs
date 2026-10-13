using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class AuditEvent : Entity
{
    // Actor information
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ActorId { get => UserId; set => UserId = value; }
    public string? ActorName { get => UserName; set => UserName = value; }

    // Action name (e.g. Auth.LoginSuccess, User.RoleUpdated, ApiKey.Created)
    public string Action { get; set; } = string.Empty;

    // Target information
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string TargetEntityType { get => EntityType; set => EntityType = value; }
    public string TargetEntityId { get => EntityId; set => EntityId = value; }

    // Application scope
    public string? ApplicationScope { get; set; }

    // Outcome: "Success", "Failure", "Warning"
    public string Outcome { get; set; } = "Success";

    // Request context
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Human-readable summary and sanitized structured payload
    public string Details { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }

    // Timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime TimestampUtc { get => Timestamp; set => Timestamp = value; }
}
