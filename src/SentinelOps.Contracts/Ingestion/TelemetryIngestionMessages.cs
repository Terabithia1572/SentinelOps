namespace SentinelOps.Contracts.Ingestion;

public class LogIngestionMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = "unknown-service";
    public string EnvironmentName { get; set; } = "Production";
    public string Severity { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string Properties { get; set; } = "{}";
    public string? ExceptionDetail { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TraceIngestionMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TraceId { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = "unknown-service";
    public string EnvironmentName { get; set; } = "Production";
    public string RootSpanName { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public int SpanCount { get; set; }
    public bool HasErrors { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SpanIngestionMessage> Spans { get; set; } = [];
}

public class SpanIngestionMessage
{
    public string SpanId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "unknown-service";
    public double DurationMs { get; set; }
    public string Status { get; set; } = "Ok";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public string Attributes { get; set; } = "{}";
}

public class MetricIngestionMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string ServiceName { get; set; } = "unknown-service";
    public string EnvironmentName { get; set; } = "Production";
    public string MetricName { get; set; } = string.Empty;
    public string MetricType { get; set; } = "Counter";
    public double Value { get; set; }
    public string Tags { get; set; } = "{}";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
