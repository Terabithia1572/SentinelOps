using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using EnvironmentEntity = SentinelOps.Domain.Entities.Environment;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;

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
        return new AlertRuleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ApplicationId = entity.ApplicationId,
            MetricName = entity.MetricName,
            Condition = entity.Condition,
            Threshold = entity.Threshold,
            EvaluationWindowMinutes = entity.EvaluationWindowMinutes,
            CooldownMinutes = entity.CooldownMinutes,
            Status = entity.Status.ToString(),
            IsEnabled = entity.IsEnabled,
            LastEvaluatedAtUtc = entity.LastEvaluatedAtUtc
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
}
