using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Services;

public class RetentionService : IRetentionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<RetentionService> _logger;

    private const string SettingKey = "Platform.DataRetentionPolicy";

    public RetentionService(
        IApplicationDbContext context,
        IAuditService auditService,
        ILogger<RetentionService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RetentionPolicyDto> GetRetentionPolicyAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);

        if (setting != null)
        {
            try
            {
                var policy = JsonSerializer.Deserialize<RetentionPolicyDto>(setting.Value);
                if (policy != null) return policy;
            }
            catch
            {
                // Fallback to default
            }
        }

        return new RetentionPolicyDto();
    }

    public async Task<RetentionPolicyDto> UpdateRetentionPolicyAsync(
        UpdateRetentionPolicyRequest request,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var policy = new RetentionPolicyDto
        {
            LogsRetentionDays = Math.Max(1, request.LogsRetentionDays),
            TracesRetentionDays = Math.Max(1, request.TracesRetentionDays),
            MetricsRetentionDays = Math.Max(1, request.MetricsRetentionDays),
            AlertEvaluationsRetentionDays = Math.Max(1, request.AlertEvaluationsRetentionDays),
            ResolvedIncidentsRetentionDays = Math.Max(1, request.ResolvedIncidentsRetentionDays),
            NotificationsRetentionDays = Math.Max(1, request.NotificationsRetentionDays),
            AuditLogsRetentionDays = Math.Max(30, request.AuditLogsRetentionDays), // Min 30 days for audit
            LastExecutedUtc = DateTime.UtcNow,
            LastExecutedBy = updatedBy
        };

        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);

        var json = JsonSerializer.Serialize(policy);

        if (setting == null)
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Key = SettingKey,
                Value = json,
                Description = "System-wide data retention periods in days",
                UpdatedBy = updatedBy
            });
        }
        else
        {
            setting.Value = json;
            setting.UpdatedBy = updatedBy;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            action: "Retention.PolicyUpdated",
            targetEntityType: "SystemSetting",
            targetEntityId: SettingKey,
            details: $"Data retention policy updated: Logs={policy.LogsRetentionDays}d, Traces={policy.TracesRetentionDays}d, Metrics={policy.MetricsRetentionDays}d",
            metadata: policy,
            cancellationToken: cancellationToken);

        return policy;
    }

    public async Task<RetentionDryRunResultDto> PreviewRetentionCleanupAsync(
        CancellationToken cancellationToken = default)
    {
        var policy = await GetRetentionPolicyAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var logsCutoff = now.AddDays(-policy.LogsRetentionDays);
        var tracesCutoff = now.AddDays(-policy.TracesRetentionDays);
        var metricsCutoff = now.AddDays(-policy.MetricsRetentionDays);
        var alertEvalsCutoff = now.AddDays(-policy.AlertEvaluationsRetentionDays);
        var incidentsCutoff = now.AddDays(-policy.ResolvedIncidentsRetentionDays);
        var notificationsCutoff = now.AddDays(-policy.NotificationsRetentionDays);
        var auditCutoff = now.AddDays(-policy.AuditLogsRetentionDays);

        var logsCount = await _context.LogEntries.AsNoTracking().CountAsync(l => l.Timestamp < logsCutoff, cancellationToken);
        var oldestLog = await _context.LogEntries.AsNoTracking().OrderBy(l => l.Timestamp).Select(l => (DateTime?)l.Timestamp).FirstOrDefaultAsync(cancellationToken);

        var tracesCount = await _context.TraceRecords.AsNoTracking().CountAsync(t => t.StartedAtUtc < tracesCutoff, cancellationToken);
        var oldestTrace = await _context.TraceRecords.AsNoTracking().OrderBy(t => t.StartedAtUtc).Select(t => (DateTime?)t.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var spansCount = await _context.SpanRecords.AsNoTracking().CountAsync(s => s.StartedAtUtc < tracesCutoff, cancellationToken);
        var oldestSpan = await _context.SpanRecords.AsNoTracking().OrderBy(s => s.StartedAtUtc).Select(s => (DateTime?)s.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var metricsCount = await _context.MetricRecords.AsNoTracking().CountAsync(m => m.Timestamp < metricsCutoff, cancellationToken);
        var oldestMetric = await _context.MetricRecords.AsNoTracking().OrderBy(m => m.Timestamp).Select(m => (DateTime?)m.Timestamp).FirstOrDefaultAsync(cancellationToken);

        var alertEvalsCount = await _context.AlertEvaluations.AsNoTracking().CountAsync(e => e.EvaluatedAtUtc < alertEvalsCutoff, cancellationToken);
        var oldestAlertEval = await _context.AlertEvaluations.AsNoTracking().OrderBy(e => e.EvaluatedAtUtc).Select(e => (DateTime?)e.EvaluatedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var incidentsCount = await _context.Incidents.AsNoTracking().CountAsync(i => i.ResolvedAtUtc != null && i.ResolvedAtUtc.Value < incidentsCutoff, cancellationToken);
        var oldestIncident = await _context.Incidents.AsNoTracking().Where(i => i.ResolvedAtUtc != null).OrderBy(i => i.ResolvedAtUtc).Select(i => i.ResolvedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var notifsCount = await _context.NotificationOutboxes.AsNoTracking().CountAsync(n => n.CreatedAtUtc < notificationsCutoff, cancellationToken);
        var oldestNotif = await _context.NotificationOutboxes.AsNoTracking().OrderBy(n => n.CreatedAtUtc).Select(n => (DateTime?)n.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var auditCount = await _context.AuditEvents.AsNoTracking().CountAsync(a => a.Timestamp < auditCutoff, cancellationToken);
        var oldestAudit = await _context.AuditEvents.AsNoTracking().OrderBy(a => a.Timestamp).Select(a => (DateTime?)a.Timestamp).FirstOrDefaultAsync(cancellationToken);

        var total = logsCount + tracesCount + spansCount + metricsCount + alertEvalsCount + incidentsCount + notifsCount + auditCount;

        return new RetentionDryRunResultDto
        {
            EstimatedAtUtc = now,
            TotalEstimatedRecordsToDelete = total,
            Logs = new RetentionTableEstimateDto
            {
                TableName = "LogEntries",
                RetentionDays = policy.LogsRetentionDays,
                CutoffUtc = logsCutoff,
                EstimatedCount = logsCount,
                OldestRecordUtc = oldestLog
            },
            Traces = new RetentionTableEstimateDto
            {
                TableName = "TraceRecords",
                RetentionDays = policy.TracesRetentionDays,
                CutoffUtc = tracesCutoff,
                EstimatedCount = tracesCount,
                OldestRecordUtc = oldestTrace
            },
            Spans = new RetentionTableEstimateDto
            {
                TableName = "SpanRecords",
                RetentionDays = policy.TracesRetentionDays,
                CutoffUtc = tracesCutoff,
                EstimatedCount = spansCount,
                OldestRecordUtc = oldestSpan
            },
            Metrics = new RetentionTableEstimateDto
            {
                TableName = "MetricRecords",
                RetentionDays = policy.MetricsRetentionDays,
                CutoffUtc = metricsCutoff,
                EstimatedCount = metricsCount,
                OldestRecordUtc = oldestMetric
            },
            AlertEvaluations = new RetentionTableEstimateDto
            {
                TableName = "AlertEvaluations",
                RetentionDays = policy.AlertEvaluationsRetentionDays,
                CutoffUtc = alertEvalsCutoff,
                EstimatedCount = alertEvalsCount,
                OldestRecordUtc = oldestAlertEval
            },
            ResolvedIncidents = new RetentionTableEstimateDto
            {
                TableName = "Incidents (Resolved)",
                RetentionDays = policy.ResolvedIncidentsRetentionDays,
                CutoffUtc = incidentsCutoff,
                EstimatedCount = incidentsCount,
                OldestRecordUtc = oldestIncident
            },
            Notifications = new RetentionTableEstimateDto
            {
                TableName = "NotificationOutboxes",
                RetentionDays = policy.NotificationsRetentionDays,
                CutoffUtc = notificationsCutoff,
                EstimatedCount = notifsCount,
                OldestRecordUtc = oldestNotif
            },
            AuditLogs = new RetentionTableEstimateDto
            {
                TableName = "AuditEvents",
                RetentionDays = policy.AuditLogsRetentionDays,
                CutoffUtc = auditCutoff,
                EstimatedCount = auditCount,
                OldestRecordUtc = oldestAudit
            }
        };
    }

    public async Task<RetentionExecutionResultDto> ExecuteRetentionCleanupAsync(
        ExecuteRetentionCleanupRequest request,
        string executedBy,
        CancellationToken cancellationToken = default)
    {
        if (!request.Confirm)
        {
            return new RetentionExecutionResultDto
            {
                Success = false,
                Message = "Execution cancelled: Confirmation was not provided."
            };
        }

        var policy = await GetRetentionPolicyAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var logsCutoff = now.AddDays(-policy.LogsRetentionDays);
        var tracesCutoff = now.AddDays(-policy.TracesRetentionDays);
        var metricsCutoff = now.AddDays(-policy.MetricsRetentionDays);
        var alertEvalsCutoff = now.AddDays(-policy.AlertEvaluationsRetentionDays);
        var incidentsCutoff = now.AddDays(-policy.ResolvedIncidentsRetentionDays);
        var notificationsCutoff = now.AddDays(-policy.NotificationsRetentionDays);
        var auditCutoff = now.AddDays(-policy.AuditLogsRetentionDays);

        var deletedCounts = new Dictionary<string, long>();

        // Bounded deletions via ExecuteDeleteAsync (with fallback for InMemory provider)
        try
        {
            deletedCounts["LogEntries"] = await DeleteRangeAsync(_context.LogEntries.Where(l => l.Timestamp < logsCutoff), _context.LogEntries, cancellationToken);
            deletedCounts["SpanRecords"] = await DeleteRangeAsync(_context.SpanRecords.Where(s => s.StartedAtUtc < tracesCutoff), _context.SpanRecords, cancellationToken);
            deletedCounts["TraceRecords"] = await DeleteRangeAsync(_context.TraceRecords.Where(t => t.StartedAtUtc < tracesCutoff), _context.TraceRecords, cancellationToken);
            deletedCounts["MetricRecords"] = await DeleteRangeAsync(_context.MetricRecords.Where(m => m.Timestamp < metricsCutoff), _context.MetricRecords, cancellationToken);
            deletedCounts["AlertEvaluations"] = await DeleteRangeAsync(_context.AlertEvaluations.Where(e => e.EvaluatedAtUtc < alertEvalsCutoff), _context.AlertEvaluations, cancellationToken);
            deletedCounts["NotificationOutboxes"] = await DeleteRangeAsync(_context.NotificationOutboxes.Where(n => n.CreatedAtUtc < notificationsCutoff), _context.NotificationOutboxes, cancellationToken);
            deletedCounts["InAppNotifications"] = await DeleteRangeAsync(_context.InAppNotifications.Where(n => n.CreatedAtUtc < notificationsCutoff), _context.InAppNotifications, cancellationToken);
            deletedCounts["AuditEvents"] = await DeleteRangeAsync(_context.AuditEvents.Where(a => a.Timestamp < auditCutoff), _context.AuditEvents, cancellationToken);

            var totalDeleted = deletedCounts.Values.Sum();

            // Record execution in policy
            policy.LastExecutedUtc = now;
            policy.LastExecutedBy = executedBy;
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);
            if (setting != null)
            {
                setting.Value = JsonSerializer.Serialize(policy);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _auditService.RecordAsync(
                action: "Retention.CleanupExecuted",
                targetEntityType: "System",
                targetEntityId: "DataRetention",
                details: $"Purged {totalDeleted:N0} expired records across {deletedCounts.Count} tables.",
                metadata: deletedCounts,
                cancellationToken: cancellationToken);

            return new RetentionExecutionResultDto
            {
                Success = true,
                Message = $"Successfully purged {totalDeleted:N0} expired telemetry and operational records.",
                ExecutedAtUtc = now,
                DeletedCountsByTable = deletedCounts,
                TotalDeletedRecords = totalDeleted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during data retention cleanup execution.");
            return new RetentionExecutionResultDto
            {
                Success = false,
                Message = $"Cleanup failed: {ex.Message}",
                ExecutedAtUtc = now,
                DeletedCountsByTable = deletedCounts,
                TotalDeletedRecords = deletedCounts.Values.Sum()
            };
        }
    }

    private async Task<long> DeleteRangeAsync<T>(IQueryable<T> query, DbSet<T> dbSet, CancellationToken ct) where T : class
    {
        try
        {
            return await query.ExecuteDeleteAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // InMemory EF Core provider does not support ExecuteDeleteAsync
            var items = await query.ToListAsync(ct);
            if (items.Count > 0)
            {
                dbSet.RemoveRange(items);
                await _context.SaveChangesAsync(ct);
            }
            return items.Count;
        }
    }
}
