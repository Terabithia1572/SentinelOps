using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using EnvironmentEntity = SentinelOps.Domain.Entities.Environment;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Application.Common.Mappings;

public static class MappingExtensions
{
    public static ApplicationDto ToDto(this ApplicationEntity entity)
    {
        return new ApplicationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Environments = entity.Environments?.Select(e => e.ToDto()).ToList() ?? [],
            Services = entity.Services?.Select(s => s.ToDto()).ToList() ?? []
        };
    }

    public static EnvironmentDto ToDto(this EnvironmentEntity entity)
    {
        return new EnvironmentDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            Name = entity.Name,
            EnvironmentType = entity.EnvironmentType.ToString(),
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static ServiceDto ToDto(this Service entity)
    {
        return new ServiceDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static ApiKeyDto ToDto(this ApiKey entity)
    {
        return new ApiKeyDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            Name = entity.Name,
            KeyPrefix = entity.KeyPrefix,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            RevokedAtUtc = entity.RevokedAtUtc,
            LastUsedAtUtc = entity.LastUsedAtUtc,
            CreatedBy = entity.CreatedBy,
            IsActive = entity.IsActive
        };
    }

    public static LogEntryDto ToDto(this LogEntry entity)
    {
        return new LogEntryDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            ServiceName = entity.ServiceName,
            EnvironmentName = entity.EnvironmentName,
            Severity = entity.Severity.ToString(),
            Message = entity.Message,
            MessageTemplate = entity.MessageTemplate,
            Properties = entity.Properties,
            ExceptionDetail = entity.ExceptionDetail,
            TraceId = entity.TraceId,
            SpanId = entity.SpanId,
            CorrelationId = entity.CorrelationId,
            Timestamp = entity.Timestamp
        };
    }

    public static TraceDto ToDto(this TraceRecord entity)
    {
        return new TraceDto
        {
            Id = entity.Id,
            TraceId = entity.TraceId,
            ApplicationId = entity.ApplicationId,
            ServiceName = entity.ServiceName,
            EnvironmentName = entity.EnvironmentName,
            RootSpanName = entity.RootSpanName,
            DurationMs = entity.Duration.TotalMilliseconds,
            SpanCount = entity.SpanCount,
            HasErrors = entity.HasErrors,
            StartedAtUtc = entity.StartedAtUtc
        };
    }

    public static MetricDto ToDto(this MetricRecord entity)
    {
        return new MetricDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            ServiceName = entity.ServiceName,
            EnvironmentName = entity.EnvironmentName,
            MetricName = entity.MetricName,
            MetricType = entity.MetricType.ToString(),
            Value = entity.Value,
            Tags = entity.Tags,
            Timestamp = entity.Timestamp
        };
    }

    public static AlertRuleDto ToDto(this AlertRule entity)
    {
        var opString = entity.Operator switch
        {
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.LessThanOrEqual => "<=",
            ComparisonOperator.Equal => "==",
            _ => ">"
        };

        return new AlertRuleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ApplicationId = entity.ApplicationId,
            EnvironmentName = entity.EnvironmentName,
            ServiceName = entity.ServiceName,
            ConditionType = entity.ConditionType.ToString(),
            Operator = entity.Operator.ToString(),
            Condition = opString,
            MetricName = entity.MetricName,
            Threshold = entity.Threshold,
            EvaluationWindowMinutes = entity.EvaluationWindowMinutes,
            EvaluationFrequencyMinutes = entity.EvaluationFrequencyMinutes,
            ConsecutiveCountRequired = entity.ConsecutiveCountRequired,
            CurrentConsecutiveFailures = entity.CurrentConsecutiveFailures,
            CooldownMinutes = entity.CooldownMinutes,
            Status = entity.Status.ToString(),
            Severity = entity.Severity.ToString(),
            IsEnabled = entity.IsEnabled,
            LastEvaluatedAtUtc = entity.LastEvaluatedAtUtc,
            LastFiredAtUtc = entity.LastFiredAtUtc,
            LastResolvedAtUtc = entity.LastResolvedAtUtc,
            LastMeasuredValue = entity.LastMeasuredValue
        };
    }

    public static HealthCheckDto ToDto(this HealthCheckResult entity)
    {
        return new HealthCheckDto
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            ServiceName = entity.ServiceName,
            EnvironmentName = entity.EnvironmentName,
            Status = entity.Status.ToString(),
            Description = entity.Description,
            ResponseTimeMs = entity.ResponseTimeMs,
            CheckedAtUtc = entity.CheckedAtUtc
        };
    }

    public static AlertEventDto ToDto(this AlertEvent entity)
    {
        return new AlertEventDto
        {
            Id = entity.Id,
            AlertRuleId = entity.AlertRuleId,
            AlertRuleName = entity.AlertRule?.Name ?? "Alert Rule",
            Severity = entity.AlertRule?.Severity.ToString() ?? "Warning",
            OldStatus = entity.OldStatus.ToString(),
            NewStatus = entity.NewStatus.ToString(),
            MeasuredValue = entity.MeasuredValue,
            Threshold = entity.Threshold,
            TriggeredAtUtc = entity.TriggeredAtUtc,
            Message = entity.Message
        };
    }

    public static IncidentDto ToDto(this Incident entity)
    {
        return new IncidentDto
        {
            Id = entity.Id,
            IncidentNumber = entity.IncidentNumber,
            Title = entity.Title,
            Description = entity.Description,
            Severity = entity.Severity.ToString(),
            Status = entity.Status.ToString(),
            ApplicationId = entity.ApplicationId,
            EnvironmentName = entity.EnvironmentName,
            ServiceName = entity.ServiceName,
            AssignedToUserId = entity.AssignedToUserId,
            AssignedToUserName = entity.AssignedToUserName,
            CreatedAtUtc = entity.CreatedAtUtc,
            AcknowledgedAtUtc = entity.AcknowledgedAtUtc,
            AcknowledgedBy = entity.AcknowledgedBy,
            MitigatedAtUtc = entity.MitigatedAtUtc,
            MitigatedBy = entity.MitigatedBy,
            ResolvedAtUtc = entity.ResolvedAtUtc,
            ResolvedBy = entity.ResolvedBy,
            ClosedAtUtc = entity.ClosedAtUtc,
            ClosedBy = entity.ClosedBy,
            RootCauseSummary = entity.RootCauseSummary,
            ResolutionNotes = entity.ResolutionNotes,
            RelatedTraceId = entity.RelatedTraceId,
            TimelineEventsCount = entity.TimelineEvents?.Count ?? 0,
            CommentsCount = entity.Comments?.Count ?? 0
        };
    }

    public static IncidentDetailDto ToDetailDto(this Incident entity)
    {
        var baseDto = entity.ToDto();
        return new IncidentDetailDto
        {
            Id = baseDto.Id,
            IncidentNumber = baseDto.IncidentNumber,
            Title = baseDto.Title,
            Description = baseDto.Description,
            Severity = baseDto.Severity,
            Status = baseDto.Status,
            ApplicationId = baseDto.ApplicationId,
            EnvironmentName = baseDto.EnvironmentName,
            ServiceName = baseDto.ServiceName,
            AssignedToUserId = baseDto.AssignedToUserId,
            AssignedToUserName = baseDto.AssignedToUserName,
            CreatedAtUtc = baseDto.CreatedAtUtc,
            AcknowledgedAtUtc = baseDto.AcknowledgedAtUtc,
            AcknowledgedBy = baseDto.AcknowledgedBy,
            MitigatedAtUtc = baseDto.MitigatedAtUtc,
            MitigatedBy = baseDto.MitigatedBy,
            ResolvedAtUtc = baseDto.ResolvedAtUtc,
            ResolvedBy = baseDto.ResolvedBy,
            ClosedAtUtc = baseDto.ClosedAtUtc,
            ClosedBy = baseDto.ClosedBy,
            RootCauseSummary = baseDto.RootCauseSummary,
            ResolutionNotes = baseDto.ResolutionNotes,
            RelatedTraceId = baseDto.RelatedTraceId,
            TimelineEventsCount = baseDto.TimelineEventsCount,
            CommentsCount = baseDto.CommentsCount,
            TimelineEvents = entity.TimelineEvents?
                .OrderBy(e => e.TimestampUtc)
                .Select(e => new IncidentTimelineEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType.ToString(),
                    TimestampUtc = e.TimestampUtc,
                    ActorId = e.ActorId,
                    ActorName = e.ActorName,
                    Title = e.Title,
                    Description = e.Description
                }).ToList() ?? new List<IncidentTimelineEventDto>(),
            Comments = entity.Comments?
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new IncidentCommentDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.UserName,
                    CommentText = c.CommentText,
                    CreatedAtUtc = c.CreatedAtUtc
                }).ToList() ?? new List<IncidentCommentDto>(),
            LinkedAlerts = entity.AlertRelations?
                .Where(r => r.AlertRule != null)
                .Select(r => r.AlertRule!.ToDto())
                .ToList() ?? new List<AlertRuleDto>()
        };
    }

    public static NotificationChannelDto ToDto(this NotificationChannel entity)
    {
        return new NotificationChannelDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.Type.ToString(),
            Destination = entity.Destination,
            IsEnabled = entity.IsEnabled,
            ApplicationId = entity.ApplicationId,
            CreatedBy = entity.CreatedBy,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static InAppNotificationDto ToDto(this InAppNotification entity)
    {
        return new InAppNotificationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Message = entity.Message,
            Severity = entity.Severity.ToString(),
            LinkUrl = entity.LinkUrl,
            IsRead = entity.IsRead,
            ReadAtUtc = entity.ReadAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    public static SloDto ToDto(this ServiceLevelObjective entity)
    {
        return new SloDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ApplicationId = entity.ApplicationId,
            ServiceName = entity.ServiceName,
            EnvironmentName = entity.EnvironmentName,
            IndicatorType = entity.IndicatorType.ToString(),
            TargetPercentage = entity.TargetPercentage,
            EvaluationWindow = entity.EvaluationWindow.ToString(),
            LatencyThresholdMs = entity.LatencyThresholdMs,
            IsEnabled = entity.IsEnabled,
            CurrentCompliancePercentage = entity.CurrentCompliancePercentage,
            CurrentErrorBudgetRemaining = entity.CurrentErrorBudgetRemaining,
            BurnRate = entity.BurnRate,
            IsBreached = entity.IsBreached,
            LastEvaluatedAtUtc = entity.LastEvaluatedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }
}
