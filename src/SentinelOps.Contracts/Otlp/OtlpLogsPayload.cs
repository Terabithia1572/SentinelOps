using System.Text.Json.Serialization;

namespace SentinelOps.Contracts.Otlp;

public class OtlpLogsPayload
{
    [JsonPropertyName("resourceLogs")]
    public List<ResourceLogs> ResourceLogs { get; set; } = [];
}

public class ResourceLogs
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeLogs")]
    public List<ScopeLogs> ScopeLogs { get; set; } = [];
}

public class ScopeLogs
{
    [JsonPropertyName("scope")]
    public OtlpScope? Scope { get; set; }

    [JsonPropertyName("logRecords")]
    public List<OtlpLogRecord> LogRecords { get; set; } = [];
}

public class OtlpLogRecord
{
    [JsonPropertyName("timeUnixNano")]
    public string? TimeUnixNano { get; set; }

    [JsonPropertyName("observedTimeUnixNano")]
    public string? ObservedTimeUnixNano { get; set; }

    [JsonPropertyName("severityNumber")]
    public object? SeverityNumber { get; set; }

    [JsonPropertyName("severityText")]
    public string? SeverityText { get; set; }

    [JsonPropertyName("body")]
    public OtlpAnyValue? Body { get; set; }

    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];

    [JsonPropertyName("flags")]
    public object? Flags { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }
}

public class OtlpResource
{
    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];
}

public class OtlpScope
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];
}

public class OtlpKeyValue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public OtlpAnyValue? Value { get; set; }
}

public class OtlpAnyValue
{
    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }

    [JsonPropertyName("boolValue")]
    public bool? BoolValue { get; set; }

    [JsonPropertyName("intValue")]
    public object? IntValue { get; set; }

    [JsonPropertyName("doubleValue")]
    public double? DoubleValue { get; set; }

    [JsonPropertyName("arrayValue")]
    public OtlpArrayValue? ArrayValue { get; set; }

    [JsonPropertyName("kvlistValue")]
    public OtlpKeyValueList? KvlistValue { get; set; }

    public string AsString()
    {
        if (StringValue != null) return StringValue;
        if (IntValue != null) return IntValue.ToString() ?? "";
        if (DoubleValue != null) return DoubleValue.Value.ToString();
        if (BoolValue != null) return BoolValue.Value ? "true" : "false";
        return "";
    }
}

public class OtlpArrayValue
{
    [JsonPropertyName("values")]
    public List<OtlpAnyValue> Values { get; set; } = [];
}

public class OtlpKeyValueList
{
    [JsonPropertyName("values")]
    public List<OtlpKeyValue> Values { get; set; } = [];
}
