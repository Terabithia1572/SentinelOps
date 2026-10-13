namespace SentinelOps.Contracts.Requests;

public class UpdateRetentionPolicyRequest
{
    public int LogsRetentionDays { get; set; } = 30;
    public int TracesRetentionDays { get; set; } = 14;
    public int MetricsRetentionDays { get; set; } = 30;
    public int AlertEvaluationsRetentionDays { get; set; } = 30;
    public int ResolvedIncidentsRetentionDays { get; set; } = 90;
    public int NotificationsRetentionDays { get; set; } = 14;
    public int AuditLogsRetentionDays { get; set; } = 365;
}

public class ExecuteRetentionCleanupRequest
{
    public bool Confirm { get; set; }
    public string? ConfirmationCode { get; set; }
    public int BatchSize { get; set; } = 5000;
}
