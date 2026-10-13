namespace SentinelOps.Contracts.DTOs;

public class SystemHealthDto
{
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0-phase6";
    public ComponentHealthDto Database { get; set; } = new();
    public ComponentHealthDto Redis { get; set; } = new();
    public ComponentHealthDto IngestionEngine { get; set; } = new();
    public ComponentHealthDto AlertEvaluator { get; set; } = new();
    public ComponentHealthDto NotificationDispatcher { get; set; } = new();
}

public class ComponentHealthDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy";
    public string Message { get; set; } = "Operational";
    public long LatencyMs { get; set; }
    public Dictionary<string, string> Details { get; set; } = [];
}
