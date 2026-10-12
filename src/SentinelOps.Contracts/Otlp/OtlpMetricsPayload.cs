using System.Text.Json.Serialization;

namespace SentinelOps.Contracts.Otlp;

public class OtlpMetricsPayload
{
    [JsonPropertyName("resourceMetrics")]
    public List<ResourceMetrics> ResourceMetrics { get; set; } = [];
}

public class ResourceMetrics
{
    [JsonPropertyName("resource")]
    public OtlpResource? Resource { get; set; }

    [JsonPropertyName("scopeMetrics")]
    public List<ScopeMetrics> ScopeMetrics { get; set; } = [];
}

public class ScopeMetrics
{
    [JsonPropertyName("scope")]
    public OtlpScope? Scope { get; set; }

    [JsonPropertyName("metrics")]
    public List<OtlpMetric> Metrics { get; set; } = [];
}

public class OtlpMetric
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("gauge")]
    public OtlpGauge? Gauge { get; set; }

    [JsonPropertyName("sum")]
    public OtlpSum? Sum { get; set; }

    [JsonPropertyName("histogram")]
    public OtlpHistogram? Histogram { get; set; }
}

public class OtlpGauge
{
    [JsonPropertyName("dataPoints")]
    public List<OtlpNumberDataPoint> DataPoints { get; set; } = [];
}

public class OtlpSum
{
    [JsonPropertyName("dataPoints")]
    public List<OtlpNumberDataPoint> DataPoints { get; set; } = [];

    [JsonPropertyName("aggregationTemporality")]
    public int? AggregationTemporality { get; set; }

    [JsonPropertyName("isMonotonic")]
    public bool? IsMonotonic { get; set; }
}

public class OtlpHistogram
{
    [JsonPropertyName("dataPoints")]
    public List<OtlpHistogramDataPoint> DataPoints { get; set; } = [];
}

public class OtlpNumberDataPoint
{
    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];

    [JsonPropertyName("startTimeUnixNano")]
    public string? StartTimeUnixNano { get; set; }

    [JsonPropertyName("timeUnixNano")]
    public string? TimeUnixNano { get; set; }

    [JsonPropertyName("asDouble")]
    public double? AsDouble { get; set; }

    [JsonPropertyName("asInt")]
    public object? AsInt { get; set; }

    public double GetNumericValue()
    {
        if (AsDouble.HasValue) return AsDouble.Value;
        if (AsInt != null && double.TryParse(AsInt.ToString(), out var d)) return d;
        return 0;
    }
}

public class OtlpHistogramDataPoint
{
    [JsonPropertyName("attributes")]
    public List<OtlpKeyValue> Attributes { get; set; } = [];

    [JsonPropertyName("startTimeUnixNano")]
    public string? StartTimeUnixNano { get; set; }

    [JsonPropertyName("timeUnixNano")]
    public string? TimeUnixNano { get; set; }

    [JsonPropertyName("count")]
    public object? Count { get; set; }

    [JsonPropertyName("sum")]
    public double? Sum { get; set; }
}
