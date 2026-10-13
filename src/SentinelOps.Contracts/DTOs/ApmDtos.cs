namespace SentinelOps.Contracts.DTOs;

public class ServiceMapDto
{
    public List<ServiceMapNodeDto> Nodes { get; set; } = [];
    public List<ServiceMapEdgeDto> Edges { get; set; } = [];
    public DateTime ComputedAtUtc { get; set; }
    public string TimeRange { get; set; } = "1h";
}

public class ServiceMapNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public double Throughput { get; set; }
    public double ErrorRate { get; set; }
    public double AvgLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public string HealthStatus { get; set; } = "Healthy";
    public long TotalCalls { get; set; }
    public long ErrorCount { get; set; }
}

public class ServiceMapEdgeDto
{
    public string SourceService { get; set; } = string.Empty;
    public string TargetService { get; set; } = string.Empty;
    public long CallCount { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double AvgLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
}

public class ServiceDependenciesDto
{
    public string ServiceName { get; set; } = string.Empty;
    public List<ServiceMapEdgeDto> Inbound { get; set; } = [];
    public List<ServiceMapEdgeDto> Outbound { get; set; } = [];
}

public class EndpointPerformanceDto
{
    public string EndpointKey { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string Route { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Production";
    public double ThroughputPerMin { get; set; }
    public long TotalCalls { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double AvgDurationMs { get; set; }
    public double P50DurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double P99DurationMs { get; set; }
}

public class EndpointBreakdownDto
{
    public string EndpointKey { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double ThroughputPerMin { get; set; }
    public double AvgDurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double ErrorRate { get; set; }
    public List<LatencyBucketDto> LatencyBuckets { get; set; } = [];
    public List<ErrorCategoryDto> ErrorBreakdown { get; set; } = [];
    public List<string> SampleTraceIds { get; set; } = [];
}

public class LatencyBucketDto
{
    public string RangeLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ErrorCategoryDto
{
    public string StatusCodeOrType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ExternalDependencyDto
{
    public string Type { get; set; } = "Database"; // Database or ExternalHttp
    public string TargetHostOrSystem { get; set; } = string.Empty;
    public string OperationOrDb { get; set; } = string.Empty;
    public long CallCount { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double AvgDurationMs { get; set; }
    public double P95DurationMs { get; set; }
}
