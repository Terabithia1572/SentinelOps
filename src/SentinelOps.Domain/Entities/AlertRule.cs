using SentinelOps.Domain.Common;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Domain.Entities;

public class AlertRule : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ServiceName { get; set; }
    public AlertConditionType ConditionType { get; set; } = AlertConditionType.ErrorRateAboveThreshold;
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThan;
    public string MetricName { get; set; } = "error_rate";
    public double Threshold { get; set; }
    public int EvaluationWindowMinutes { get; set; } = 5;
    public int EvaluationFrequencyMinutes { get; set; } = 1;
    public int ConsecutiveCountRequired { get; set; } = 1;
    public int CurrentConsecutiveFailures { get; set; } = 0;
    public int CooldownMinutes { get; set; } = 15;
    public AlertStatus Status { get; set; } = AlertStatus.Normal;
    public Severity Severity { get; set; } = Severity.Warning;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastEvaluatedAtUtc { get; set; }
    public DateTime? LastFiredAtUtc { get; set; }
    public DateTime? LastResolvedAtUtc { get; set; }
    public double? LastMeasuredValue { get; set; }
    public DateTime? SuppressedUntilUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<AlertIncident> Incidents { get; set; } = new List<AlertIncident>();
    public ICollection<AlertEvaluation> Evaluations { get; set; } = new List<AlertEvaluation>();
    public ICollection<AlertEvent> Events { get; set; } = new List<AlertEvent>();
    public ICollection<AlertRuleNotificationChannel> NotificationChannels { get; set; } = new List<AlertRuleNotificationChannel>();

    public bool Evaluate(double measuredValue, DateTime utcNow, out AlertEvent? alertEvent)
    {
        alertEvent = null;
        LastEvaluatedAtUtc = utcNow;
        LastMeasuredValue = measuredValue;

        if (SuppressedUntilUtc.HasValue && utcNow < SuppressedUntilUtc.Value)
        {
            Status = AlertStatus.Suppressed;
            return false;
        }

        bool conditionTriggered = Operator switch
        {
            ComparisonOperator.GreaterThan => measuredValue > Threshold,
            ComparisonOperator.GreaterThanOrEqual => measuredValue >= Threshold,
            ComparisonOperator.LessThan => measuredValue < Threshold,
            ComparisonOperator.LessThanOrEqual => measuredValue <= Threshold,
            ComparisonOperator.Equal => Math.Abs(measuredValue - Threshold) < 0.0001,
            _ => measuredValue > Threshold
        };

        var oldStatus = Status;

        if (conditionTriggered)
        {
            CurrentConsecutiveFailures++;
            if (CurrentConsecutiveFailures >= ConsecutiveCountRequired)
            {
                // Check cooldown if already firing
                if (Status == AlertStatus.Firing && LastFiredAtUtc.HasValue)
                {
                    if (utcNow - LastFiredAtUtc.Value < TimeSpan.FromMinutes(CooldownMinutes))
                    {
                        // Within cooldown: still firing, but suppress duplicate notification firing
                        return false;
                    }
                }

                Status = AlertStatus.Firing;
                LastFiredAtUtc = utcNow;

                alertEvent = new AlertEvent
                {
                    AlertRuleId = Id,
                    OldStatus = oldStatus,
                    NewStatus = AlertStatus.Firing,
                    MeasuredValue = measuredValue,
                    Threshold = Threshold,
                    TriggeredAtUtc = utcNow,
                    Message = $"Alert rule '{Name}' triggered: measured {measuredValue:F2} (threshold {Threshold:F2})."
                };
                Events.Add(alertEvent);
                return true;
            }
            else
            {
                Status = AlertStatus.Pending;
                return false;
            }
        }
        else
        {
            CurrentConsecutiveFailures = 0;
            if (Status == AlertStatus.Firing || Status == AlertStatus.Pending || Status == AlertStatus.Acknowledged)
            {
                Status = AlertStatus.Resolved;
                LastResolvedAtUtc = utcNow;

                alertEvent = new AlertEvent
                {
                    AlertRuleId = Id,
                    OldStatus = oldStatus,
                    NewStatus = AlertStatus.Resolved,
                    MeasuredValue = measuredValue,
                    Threshold = Threshold,
                    TriggeredAtUtc = utcNow,
                    Message = $"Alert rule '{Name}' recovered: measured {measuredValue:F2} is within threshold {Threshold:F2}."
                };
                Events.Add(alertEvent);
                return true;
            }

            Status = AlertStatus.Normal;
            return false;
        }
    }

    public void Acknowledge(string? acknowledgedBy, DateTime utcNow)
    {
        if (Status == AlertStatus.Firing)
        {
            var oldStatus = Status;
            Status = AlertStatus.Acknowledged;
            Events.Add(new AlertEvent
            {
                AlertRuleId = Id,
                OldStatus = oldStatus,
                NewStatus = AlertStatus.Acknowledged,
                MeasuredValue = LastMeasuredValue ?? Threshold,
                Threshold = Threshold,
                TriggeredAtUtc = utcNow,
                Message = $"Alert rule '{Name}' acknowledged by {acknowledgedBy ?? "system"}."
            });
        }
    }

    public void Resolve(string? resolvedBy, DateTime utcNow, string reason)
    {
        var oldStatus = Status;
        Status = AlertStatus.Resolved;
        LastResolvedAtUtc = utcNow;
        CurrentConsecutiveFailures = 0;
        Events.Add(new AlertEvent
        {
            AlertRuleId = Id,
            OldStatus = oldStatus,
            NewStatus = AlertStatus.Resolved,
            MeasuredValue = LastMeasuredValue ?? Threshold,
            Threshold = Threshold,
            TriggeredAtUtc = utcNow,
            Message = $"Alert rule '{Name}' manually resolved by {resolvedBy ?? "user"}: {reason}."
        });
    }

    public void Suppress(DateTime untilUtc, string? suppressedBy)
    {
        SuppressedUntilUtc = untilUtc;
        Status = AlertStatus.Suppressed;
        Events.Add(new AlertEvent
        {
            AlertRuleId = Id,
            OldStatus = Status,
            NewStatus = AlertStatus.Suppressed,
            MeasuredValue = LastMeasuredValue ?? Threshold,
            Threshold = Threshold,
            TriggeredAtUtc = DateTime.UtcNow,
            Message = $"Alert rule '{Name}' suppressed until {untilUtc:u} by {suppressedBy ?? "user"}."
        });
    }

    public void Enable()
    {
        IsEnabled = true;
        Status = AlertStatus.Normal;
        CurrentConsecutiveFailures = 0;
    }

    public void Disable()
    {
        IsEnabled = false;
        CurrentConsecutiveFailures = 0;
    }
}
