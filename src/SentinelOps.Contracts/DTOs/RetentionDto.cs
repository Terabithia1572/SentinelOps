namespace SentinelOps.Contracts.DTOs;

public class RetentionPolicyDto
{
    public int LogsRetentionDays { get; set; } = 30;
    public int TracesRetentionDays { get; set; } = 14;
    public int MetricsRetentionDays { get; set; } = 30;
    public int AlertEvaluationsRetentionDays { get; set; } = 30;
    public int ResolvedIncidentsRetentionDays { get; set; } = 90;
    public int NotificationsRetentionDays { get; set; } = 14;
    public int AuditLogsRetentionDays { get; set; } = 365;
    public DateTime LastExecutedUtc { get; set; }
    public string LastExecutedBy { get; set; } = "system";
}

public class RetentionDryRunResultDto
{
    public DateTime EstimatedAtUtc { get; set; } = DateTime.UtcNow;
    public RetentionTableEstimateDto Logs { get; set; } = new();
    public RetentionTableEstimateDto Traces { get; set; } = new();
    public RetentionTableEstimateDto Spans { get; set; } = new();
    public RetentionTableEstimateDto Metrics { get; set; } = new();
    public RetentionTableEstimateDto AlertEvaluations { get; set; } = new();
    public RetentionTableEstimateDto ResolvedIncidents { get; set; } = new();
    public RetentionTableEstimateDto Notifications { get; set; } = new();
    public RetentionTableEstimateDto AuditLogs { get; set; } = new();
    public long TotalEstimatedRecordsToDelete { get; set; }
}

public class RetentionTableEstimateDto
{
    public string TableName { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public DateTime CutoffUtc { get; set; }
    public long EstimatedCount { get; set; }
    public DateTime? OldestRecordUtc { get; set; }
}

public class RetentionExecutionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, long> DeletedCountsByTable { get; set; } = [];
    public long TotalDeletedRecords { get; set; }
}
