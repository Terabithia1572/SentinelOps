using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Alerting;

public class AlertEvaluationService : IAlertEvaluationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AlertEvaluationService> _logger;

    public AlertEvaluationService(
        IApplicationDbContext context,
        ILogger<AlertEvaluationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> EvaluateAllPendingRulesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var pendingRules = await _context.AlertRules
            .Include(r => r.NotificationChannels)
                .ThenInclude(nc => nc.NotificationChannel)
            .Where(r => r.IsEnabled)
            .Where(r => r.LastEvaluatedAtUtc == null ||
                        r.LastEvaluatedAtUtc.Value.AddMinutes(r.EvaluationFrequencyMinutes) <= utcNow)
            .OrderBy(r => r.LastEvaluatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        int evaluatedCount = 0;
        foreach (var rule in pendingRules)
        {
            try
            {
                await EvaluateRuleInternalAsync(rule, utcNow, cancellationToken);
                evaluatedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate alert rule {RuleId} ({RuleName})", rule.Id, rule.Name);
            }
        }

        if (evaluatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return evaluatedCount;
    }

    public async Task<AlertEvaluationResult> EvaluateRuleAsync(Guid alertRuleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.AlertRules
            .Include(r => r.NotificationChannels)
                .ThenInclude(nc => nc.NotificationChannel)
            .FirstOrDefaultAsync(r => r.Id == alertRuleId, cancellationToken);

        if (rule == null)
        {
            return new AlertEvaluationResult(
                alertRuleId,
                false,
                0.0,
                0.0,
                false,
                "Unknown",
                "Unknown",
                false,
                "Alert rule not found."
            );
        }

        var utcNow = DateTime.UtcNow;
        var result = await EvaluateRuleInternalAsync(rule, utcNow, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<AlertEvaluationResult> EvaluateRuleInternalAsync(
        AlertRule rule,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var fromUtc = utcNow.AddMinutes(-Math.Max(1, rule.EvaluationWindowMinutes));
        double measuredValue = await CalculateMeasuredValueAsync(rule, fromUtc, utcNow, cancellationToken);

        var oldStatus = rule.Status;
        bool stateChanged = rule.Evaluate(measuredValue, utcNow, out var alertEvent);

        bool conditionMet = rule.Operator switch
        {
            ComparisonOperator.GreaterThan => measuredValue > rule.Threshold,
            ComparisonOperator.GreaterThanOrEqual => measuredValue >= rule.Threshold,
            ComparisonOperator.LessThan => measuredValue < rule.Threshold,
            ComparisonOperator.LessThanOrEqual => measuredValue <= rule.Threshold,
            ComparisonOperator.Equal => Math.Abs(measuredValue - rule.Threshold) < 0.0001,
            _ => measuredValue > rule.Threshold
        };

        // Persist evaluation record
        var evaluation = new AlertEvaluation
        {
            AlertRuleId = rule.Id,
            EvaluatedAtUtc = utcNow,
            MeasuredValue = measuredValue,
            Threshold = rule.Threshold,
            IsTriggered = conditionMet,
            Details = $"Evaluated '{rule.ConditionType}': measured {measuredValue:F2} vs threshold {rule.Threshold:F2}."
        };
        _context.AlertEvaluations.Add(evaluation);

        if (stateChanged && alertEvent != null)
        {
            _logger.LogInformation(
                "Alert rule {RuleId} ({RuleName}) status changed from {OldStatus} to {NewStatus}. Measured: {MeasuredValue:F2}",
                rule.Id, rule.Name, oldStatus, rule.Status, measuredValue);

            // Handle incident auto-creation for Critical/Error firing alerts
            if (rule.Status == AlertStatus.Firing && (rule.Severity == Severity.Critical || rule.Severity == Severity.Error))
            {
                await EnsureIncidentCreatedForAlertAsync(rule, alertEvent, cancellationToken);
            }

            // Enqueue notifications
            EnqueueNotifications(rule, alertEvent);
        }

        return new AlertEvaluationResult(
            rule.Id,
            true,
            measuredValue,
            rule.Threshold,
            conditionMet,
            oldStatus.ToString(),
            rule.Status.ToString(),
            stateChanged,
            alertEvent?.Message ?? $"Evaluated {rule.Name}: measured {measuredValue:F2}."
        );
    }

    private async Task<double> CalculateMeasuredValueAsync(
        AlertRule rule,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        return rule.ConditionType switch
        {
            AlertConditionType.ErrorRateAboveThreshold => await CalculateErrorRateAsync(rule, fromUtc, toUtc, cancellationToken),
            AlertConditionType.P95LatencyAboveThreshold => await CalculateLatencyPercentileAsync(rule, fromUtc, 0.95, cancellationToken),
            AlertConditionType.P99LatencyAboveThreshold => await CalculateLatencyPercentileAsync(rule, fromUtc, 0.99, cancellationToken),
            AlertConditionType.ThroughputBelowThreshold => await CalculateThroughputAsync(rule, fromUtc, toUtc, cancellationToken),
            AlertConditionType.ThroughputAboveThreshold => await CalculateThroughputAsync(rule, fromUtc, toUtc, cancellationToken),
            AlertConditionType.ExceptionCountAboveThreshold => await CalculateExceptionCountAsync(rule, fromUtc, cancellationToken),
            AlertConditionType.LogSeverityCountAboveThreshold => await CalculateLogSeverityCountAsync(rule, fromUtc, cancellationToken),
            AlertConditionType.NoTelemetryReceived => await CalculateMinutesSinceLastTelemetryAsync(rule, toUtc, cancellationToken),
            _ => 0.0
        };
    }

    private async Task<double> CalculateErrorRateAsync(
        AlertRule rule,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var query = _context.TraceRecords
            .Where(t => t.ApplicationId == rule.ApplicationId && t.StartedAtUtc >= fromUtc && t.StartedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(rule.EnvironmentName))
        {
            query = query.Where(t => t.EnvironmentName == rule.EnvironmentName);
        }
        if (!string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            query = query.Where(t => t.ServiceName == rule.ServiceName);
        }

        int total = await query.CountAsync(cancellationToken);
        if (total == 0) return 0.0;

        int errors = await query.CountAsync(t => t.HasErrors, cancellationToken);
        return Math.Round(((double)errors / total) * 100.0, 2);
    }

    private async Task<double> CalculateLatencyPercentileAsync(
        AlertRule rule,
        DateTime fromUtc,
        double percentile,
        CancellationToken cancellationToken)
    {
        var query = _context.TraceRecords
            .Where(t => t.ApplicationId == rule.ApplicationId && t.StartedAtUtc >= fromUtc);

        if (!string.IsNullOrWhiteSpace(rule.EnvironmentName))
        {
            query = query.Where(t => t.EnvironmentName == rule.EnvironmentName);
        }
        if (!string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            query = query.Where(t => t.ServiceName == rule.ServiceName);
        }

        var durations = await query
            .Select(t => t.Duration.TotalMilliseconds)
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        if (durations.Count == 0) return 0.0;

        int index = (int)Math.Ceiling(durations.Count * percentile) - 1;
        index = Math.Clamp(index, 0, durations.Count - 1);
        return Math.Round(durations[index], 2);
    }

    private async Task<double> CalculateThroughputAsync(
        AlertRule rule,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var query = _context.TraceRecords
            .Where(t => t.ApplicationId == rule.ApplicationId && t.StartedAtUtc >= fromUtc && t.StartedAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(rule.EnvironmentName))
        {
            query = query.Where(t => t.EnvironmentName == rule.EnvironmentName);
        }
        if (!string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            query = query.Where(t => t.ServiceName == rule.ServiceName);
        }

        int count = await query.CountAsync(cancellationToken);
        double totalSeconds = Math.Max(1.0, (toUtc - fromUtc).TotalSeconds);
        return Math.Round(count / totalSeconds, 2);
    }

    private async Task<double> CalculateExceptionCountAsync(
        AlertRule rule,
        DateTime fromUtc,
        CancellationToken cancellationToken)
    {
        var query = _context.LogEntries
            .Where(l => l.ApplicationId == rule.ApplicationId && l.Timestamp >= fromUtc && l.ExceptionDetail != null && l.ExceptionDetail != "");

        if (!string.IsNullOrWhiteSpace(rule.EnvironmentName))
        {
            query = query.Where(l => l.EnvironmentName == rule.EnvironmentName);
        }
        if (!string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            query = query.Where(l => l.ServiceName == rule.ServiceName);
        }

        return await query.CountAsync(cancellationToken);
    }

    private async Task<double> CalculateLogSeverityCountAsync(
        AlertRule rule,
        DateTime fromUtc,
        CancellationToken cancellationToken)
    {
        var minSeverity = rule.Severity == Severity.Critical ? Severity.Critical : Severity.Error;

        var query = _context.LogEntries
            .Where(l => l.ApplicationId == rule.ApplicationId && l.Timestamp >= fromUtc && l.Severity >= minSeverity);

        if (!string.IsNullOrWhiteSpace(rule.EnvironmentName))
        {
            query = query.Where(l => l.EnvironmentName == rule.EnvironmentName);
        }
        if (!string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            query = query.Where(l => l.ServiceName == rule.ServiceName);
        }

        return await query.CountAsync(cancellationToken);
    }

    private async Task<double> CalculateMinutesSinceLastTelemetryAsync(
        AlertRule rule,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        DateTime? latestLog = await _context.LogEntries
            .Where(l => l.ApplicationId == rule.ApplicationId)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => (DateTime?)l.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime? latestTrace = await _context.TraceRecords
            .Where(t => t.ApplicationId == rule.ApplicationId)
            .OrderByDescending(t => t.StartedAtUtc)
            .Select(t => (DateTime?)t.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime? latestMetric = await _context.MetricRecords
            .Where(m => m.ApplicationId == rule.ApplicationId)
            .OrderByDescending(m => m.Timestamp)
            .Select(m => (DateTime?)m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var timestamps = new[] { latestLog, latestTrace, latestMetric }
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();

        if (timestamps.Count == 0)
        {
            return 999999.0; // No telemetry ever observed
        }

        var mostRecent = timestamps.Max();
        return Math.Max(0.0, Math.Round((toUtc - mostRecent).TotalMinutes, 1));
    }

    private async Task EnsureIncidentCreatedForAlertAsync(
        AlertRule rule,
        AlertEvent alertEvent,
        CancellationToken cancellationToken)
    {
        // Check if an active incident is already linked to this alert rule
        bool hasActiveIncident = await _context.Incidents
            .AnyAsync(i => i.AlertRelations.Any(r => r.AlertRuleId == rule.Id) &&
                           i.Status != IncidentStatus.Closed &&
                           i.Status != IncidentStatus.Resolved,
                cancellationToken);

        if (!hasActiveIncident)
        {
            var incidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var incident = new Incident
            {
                IncidentNumber = incidentNumber,
                Title = $"{rule.Name} [{rule.Severity}]",
                Description = alertEvent.Message,
                Severity = rule.Severity,
                Status = IncidentStatus.Open,
                ApplicationId = rule.ApplicationId,
                EnvironmentName = rule.EnvironmentName,
                ServiceName = rule.ServiceName,
                CreatedAtUtc = DateTime.UtcNow
            };

            incident.TimelineEvents.Add(new IncidentTimelineEvent
            {
                IncidentId = incident.Id,
                EventType = IncidentTimelineEventType.Created,
                TimestampUtc = DateTime.UtcNow,
                ActorId = "system",
                ActorName = "SentinelOps Alert Engine",
                Title = "Incident Created from Alert Rule",
                Description = alertEvent.Message
            });

            incident.AlertRelations.Add(new IncidentAlertRelation
            {
                IncidentId = incident.Id,
                AlertRuleId = rule.Id,
                LinkedAtUtc = DateTime.UtcNow
            });

            _context.Incidents.Add(incident);
        }
    }

    private void EnqueueNotifications(AlertRule rule, AlertEvent alertEvent)
    {
        var eventType = alertEvent.NewStatus == AlertStatus.Firing
            ? NotificationEventType.AlertTriggered
            : NotificationEventType.AlertResolved;

        var subject = $"[{alertEvent.NewStatus.ToString().ToUpper()}] {rule.Name} ({rule.Severity})";
        var payloadJson = JsonSerializer.Serialize(new
        {
            alertRuleId = rule.Id,
            ruleName = rule.Name,
            status = rule.Status.ToString(),
            severity = rule.Severity.ToString(),
            conditionType = rule.ConditionType.ToString(),
            measuredValue = alertEvent.MeasuredValue,
            threshold = alertEvent.Threshold,
            triggeredAtUtc = alertEvent.TriggeredAtUtc,
            message = alertEvent.Message
        });

        // 1. In-App Notification
        _context.InAppNotifications.Add(new InAppNotification
        {
            ApplicationId = rule.ApplicationId,
            Role = "Operator",
            Title = subject,
            Message = alertEvent.Message,
            Severity = rule.Severity,
            LinkUrl = "/alerts",
            CreatedAtUtc = DateTime.UtcNow
        });

        // 2. Outbox Messages for assigned channels
        foreach (var mapping in rule.NotificationChannels)
        {
            if (mapping.NotificationChannel != null && mapping.NotificationChannel.IsEnabled)
            {
                _context.NotificationOutboxes.Add(new NotificationOutbox
                {
                    ChannelId = mapping.NotificationChannelId,
                    EventType = eventType,
                    Status = NotificationStatus.Pending,
                    Subject = subject,
                    Message = alertEvent.Message,
                    PayloadJson = payloadJson,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
    }
}
