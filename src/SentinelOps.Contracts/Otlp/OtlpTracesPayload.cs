using System.Text.Json.Serialization;

namespace SentinelOps.Contracts.Otlp;

public class OtlpTracesPayload
{
    [JsonPropertyName("resourceSpans")]
    public List<ResourceSpans> ResourceSpans { get; set; } = [];
}

public class ResourceSpans
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeSpans")]
    public List<ScopeSpans> ScopeSpans { get; set; } = [];
}

public class ScopeSpans
{
    [JsonPropertyName("scope")]
    public OtlpScope? Scope { get; set; }

    [JsonPropertyName("spans")]
    public List<OtlpSpan> Spans { get; set; } = [];
}

public class OtlpSpan
{
    [JsonPropertyName("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("spanId")]
    public string SpanId { get; set; } = string.Empty;

    [JsonPropertyName("traceState")]
    public string? TraceState { get; set; }

    [JsonPropertyName("parentSpanId")]
    public string? ParentSpanId { get; set; }

    [JsonPropertyName("flags")]
    public object? Flags { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public object? Kind { get; set; }

    [JsonPropertyName("startTimeUnixNano")]
    public string? StartTimeUnixNano { get; set; }

    [JsonPropertyName("endTimeUnixNano")]
    public string? EndTimeUnixNano { get; set; }

    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];

    [JsonPropertyName("status")]
    public OtlpStatus? Status { get; set; }
}

public class OtlpStatus
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("code")]
    public object? Code { get; set; }
}
