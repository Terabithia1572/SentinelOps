using System.Text.Json;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Contracts.Otlp;

namespace SentinelOps.Application.Services;

public static class OtlpPayloadMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static List<LogIngestionMessage> MapLogs(OtlpLogsPayload payload, Guid applicationId)
    {
        var result = new List<LogIngestionMessage>();
        if (payload.ResourceLogs == null) return result;

        foreach (var resLogs in payload.ResourceLogs)
        {
            var (serviceName, envName) = ExtractResourceAttributes(resLogs.Resource);

            if (resLogs.ScopeLogs == null) continue;

            foreach (var scopeLogs in resLogs.ScopeLogs)
            {
                if (scopeLogs.LogRecords == null) continue;

                foreach (var record in scopeLogs.LogRecords)
                {
                    var attributesDict = ExtractAttributes(record.Attributes);
                    var propertiesJson = JsonSerializer.Serialize(attributesDict, JsonOptions);

                    var severity = ResolveSeverity(record.SeverityText, record.SeverityNumber);
                    var message = record.Body?.AsString() ?? string.Empty;
                    var timestamp = ParseUnixNano(record.TimeUnixNano ?? record.ObservedTimeUnixNano);

                    var exceptionDetail = ExtractException(attributesDict);
                    var correlationId = attributesDict.GetValueOrDefault("correlation.id")
                        ?? attributesDict.GetValueOrDefault("correlation_id")
                        ?? attributesDict.GetValueOrDefault("trace_id");

                    result.Add(new LogIngestionMessage
                    {
                        ApplicationId = applicationId,
                        ServiceName = serviceName,
                        EnvironmentName = envName,
                        Severity = severity,
                        Message = message,
                        MessageTemplate = message,
                        Properties = propertiesJson,
                        ExceptionDetail = exceptionDetail,
                        TraceId = record.TraceId,
                        SpanId = record.SpanId,
                        CorrelationId = correlationId,
                        Timestamp = timestamp
                    });
                }
            }
        }

        return result;
    }

    public static List<TraceIngestionMessage> MapTraces(OtlpTracesPayload payload, Guid applicationId)
    {
        var result = new List<TraceIngestionMessage>();
        if (payload.ResourceSpans == null) return result;

        foreach (var resSpans in payload.ResourceSpans)
        {
            var (serviceName, envName) = ExtractResourceAttributes(resSpans.Resource);

            if (resSpans.ScopeSpans == null) continue;

            var spansByTrace = new Dictionary<string, List<SpanIngestionMessage>>(StringComparer.OrdinalIgnoreCase);

            foreach (var scopeSpans in resSpans.ScopeSpans)
            {
                if (scopeSpans.Spans == null) continue;

                foreach (var span in scopeSpans.Spans)
                {
                    if (string.IsNullOrWhiteSpace(span.TraceId)) continue;

                    var startTime = ParseUnixNano(span.StartTimeUnixNano);
                    var endTime = ParseUnixNano(span.EndTimeUnixNano);
                    var durationMs = Math.Max(0, (endTime - startTime).TotalMilliseconds);
                    var attributesDict = ExtractAttributes(span.Attributes);
                    var attributesJson = JsonSerializer.Serialize(attributesDict, JsonOptions);

                    var status = "Ok";
                    if (span.Status?.Code != null)
                    {
                        var codeStr = span.Status.Code.ToString() ?? "";
                        if (codeStr is "2" or "STATUS_CODE_ERROR" || codeStr.Contains("Error", StringComparison.OrdinalIgnoreCase))
                        {
                            status = "Error";
                        }
                    }

                    var spanMsg = new SpanIngestionMessage
                    {
                        SpanId = span.SpanId,
                        TraceId = span.TraceId,
                        ParentSpanId = string.IsNullOrWhiteSpace(span.ParentSpanId) ? null : span.ParentSpanId,
                        OperationName = span.Name,
                        ServiceName = serviceName,
                        DurationMs = durationMs,
                        Status = status,
                        StartedAtUtc = startTime,
                        Attributes = attributesJson
                    };

                    if (!spansByTrace.TryGetValue(span.TraceId, out var spanList))
                    {
                        spanList = [];
                        spansByTrace[span.TraceId] = spanList;
                    }
                    spanList.Add(spanMsg);
                }
            }

            foreach (var (traceId, spans) in spansByTrace)
            {
                var rootSpan = spans.FirstOrDefault(s => string.IsNullOrEmpty(s.ParentSpanId))
                    ?? spans.OrderBy(s => s.StartedAtUtc).First();

                var startedAt = spans.Min(s => s.StartedAtUtc);
                var totalDurationMs = spans.Max(s => s.DurationMs);
                var hasErrors = spans.Any(s => s.Status.Equals("Error", StringComparison.OrdinalIgnoreCase));

                result.Add(new TraceIngestionMessage
                {
                    TraceId = traceId,
                    ApplicationId = applicationId,
                    ServiceName = serviceName,
                    EnvironmentName = envName,
                    RootSpanName = rootSpan.OperationName,
                    DurationMs = totalDurationMs,
                    SpanCount = spans.Count,
                    HasErrors = hasErrors,
                    StartedAtUtc = startedAt,
                    Spans = spans
                });
            }
        }

        return result;
    }

    public static List<MetricIngestionMessage> MapMetrics(OtlpMetricsPayload payload, Guid applicationId)
    {
        var result = new List<MetricIngestionMessage>();
        if (payload.ResourceMetrics == null) return result;

        foreach (var resMetrics in payload.ResourceMetrics)
        {
            var (serviceName, envName) = ExtractResourceAttributes(resMetrics.Resource);

            if (resMetrics.ScopeMetrics == null) continue;

            foreach (var scopeMetrics in resMetrics.ScopeMetrics)
            {
                if (scopeMetrics.Metrics == null) continue;

                foreach (var metric in scopeMetrics.Metrics)
                {
                    var metricName = metric.Name;

                    // Handle Gauge
                    if (metric.Gauge?.DataPoints != null)
                    {
                        foreach (var dp in metric.Gauge.DataPoints)
                        {
                            var tagsDict = ExtractAttributes(dp.Attributes);
                            result.Add(new MetricIngestionMessage
                            {
                                ApplicationId = applicationId,
                                ServiceName = serviceName,
                                EnvironmentName = envName,
                                MetricName = metricName,
                                MetricType = "Gauge",
                                Value = dp.GetNumericValue(),
                                Tags = JsonSerializer.Serialize(tagsDict, JsonOptions),
                                Timestamp = ParseUnixNano(dp.TimeUnixNano ?? dp.StartTimeUnixNano)
                            });
                        }
                    }

                    // Handle Sum (Counter)
                    if (metric.Sum?.DataPoints != null)
                    {
                        foreach (var dp in metric.Sum.DataPoints)
                        {
                            var tagsDict = ExtractAttributes(dp.Attributes);
                            result.Add(new MetricIngestionMessage
                            {
                                ApplicationId = applicationId,
                                ServiceName = serviceName,
                                EnvironmentName = envName,
                                MetricName = metricName,
                                MetricType = "Counter",
                                Value = dp.GetNumericValue(),
                                Tags = JsonSerializer.Serialize(tagsDict, JsonOptions),
                                Timestamp = ParseUnixNano(dp.TimeUnixNano ?? dp.StartTimeUnixNano)
                            });
                        }
                    }

                    // Handle Histogram
                    if (metric.Histogram?.DataPoints != null)
                    {
                        foreach (var dp in metric.Histogram.DataPoints)
                        {
                            var tagsDict = ExtractAttributes(dp.Attributes);
                            var val = dp.Sum ?? (dp.Count != null && double.TryParse(dp.Count.ToString(), out var c) ? c : 0.0);
                            result.Add(new MetricIngestionMessage
                            {
                                ApplicationId = applicationId,
                                ServiceName = serviceName,
                                EnvironmentName = envName,
                                MetricName = metricName,
                                MetricType = "Histogram",
                                Value = val,
                                Tags = JsonSerializer.Serialize(tagsDict, JsonOptions),
                                Timestamp = ParseUnixNano(dp.TimeUnixNano ?? dp.StartTimeUnixNano)
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    private static (string ServiceName, string Environment) ExtractResourceAttributes(OtlpResource? resource)
    {
        var serviceName = "unknown-service";
        var envName = "Production";

        if (resource?.Attributes != null)
        {
            var dict = ExtractAttributes(resource.Attributes);
            if (dict.TryGetValue("service.name", out var sn) && !string.IsNullOrWhiteSpace(sn))
            {
                serviceName = sn;
            }
            if (dict.TryGetValue("deployment.environment", out var env) && !string.IsNullOrWhiteSpace(env))
            {
                envName = env;
            }
            else if (dict.TryGetValue("environment", out var envAlt) && !string.IsNullOrWhiteSpace(envAlt))
            {
                envName = envAlt;
            }
        }

        return (serviceName, envName);
    }

    private static Dictionary<string, string> ExtractAttributes(List<OtlpKeyValue>? attributes)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (attributes == null) return dict;

        foreach (var attr in attributes)
        {
            if (string.IsNullOrWhiteSpace(attr.Key) || attr.Value == null) continue;
            dict[attr.Key] = attr.Value.AsString();
        }

        return dict;
    }

    private static string? ExtractException(Dictionary<string, string> attributes)
    {
        attributes.TryGetValue("exception.message", out var msg);
        attributes.TryGetValue("exception.stacktrace", out var stack);

        if (!string.IsNullOrEmpty(msg) || !string.IsNullOrEmpty(stack))
        {
            var exType = attributes.GetValueOrDefault("exception.type", "Exception");
            return $"{exType}: {msg}\n{stack}".Trim();
        }

        return null;
    }

    private static string ResolveSeverity(string? severityText, object? severityNumber)
    {
        if (!string.IsNullOrWhiteSpace(severityText))
        {
            var lower = severityText.ToLowerInvariant();
            if (lower.Contains("crit") || lower.Contains("fatal")) return "Critical";
            if (lower.Contains("err")) return "Error";
            if (lower.Contains("warn")) return "Warning";
            if (lower.Contains("info")) return "Information";
            if (lower.Contains("debug")) return "Debug";
            if (lower.Contains("trace")) return "Trace";
        }

        if (severityNumber != null)
        {
            var str = severityNumber.ToString() ?? "";
            var lower = str.ToLowerInvariant();
            if (lower.Contains("crit") || lower.Contains("fatal")) return "Critical";
            if (lower.Contains("err")) return "Error";
            if (lower.Contains("warn")) return "Warning";
            if (lower.Contains("info")) return "Information";
            if (lower.Contains("debug")) return "Debug";
            if (lower.Contains("trace")) return "Trace";

            if (int.TryParse(str, out var num) && num > 0)
            {
                return num switch
                {
                    >= 21 => "Critical",
                    >= 17 => "Error",
                    >= 13 => "Warning",
                    >= 9 => "Information",
                    >= 5 => "Debug",
                    _ => "Trace"
                };
            }
        }

        return "Information";
    }

    public static DateTime ParseUnixNano(string? nanoStr)
    {
        if (!string.IsNullOrWhiteSpace(nanoStr) && long.TryParse(nanoStr, out var nanos) && nanos > 0)
        {
            try
            {
                var millis = nanos / 1_000_000;
                return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        return DateTime.UtcNow;
    }
}
