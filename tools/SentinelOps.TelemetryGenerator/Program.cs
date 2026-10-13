using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SentinelOps.Contracts.Otlp;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=========================================================");
Console.WriteLine("      SENTINELOPS — SYNTHETIC TELEMETRY GENERATOR        ");
Console.WriteLine("=========================================================");
Console.ResetColor();

var endpoint = GetArg(args, "--endpoint", "http://localhost:5100");
var apiKey = GetArg(args, "--api-key", Environment.GetEnvironmentVariable("SENTINELOPS_API_KEY") ?? "");
var serviceName = GetArg(args, "--service", "PaymentProcessingService");
var environment = GetArg(args, "--environment", "Production");
var count = int.TryParse(GetArg(args, "--count", "25"), out var c) ? c : 25;
var intervalMs = int.TryParse(GetArg(args, "--interval-ms", "150"), out var im) ? im : 150;
var scenario = GetArg(args, "--scenario", "all");

Console.WriteLine($"Target Endpoint: {endpoint}");
Console.WriteLine($"Service Name:    {serviceName}");
Console.WriteLine($"Environment:     {environment}");
Console.WriteLine($"Burst Count:     {count}");
Console.WriteLine($"Interval:        {intervalMs} ms");
Console.WriteLine($"Scenario:        {scenario}");
Console.WriteLine();

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-API-Key", apiKey);

var rng = new Random();
var successCount = 0;
var errorCount = 0;

for (var i = 1; i <= count; i++)
{
    Console.Write($"[{i}/{count}] Generating telemetry burst... ");

    try
    {
        var isErrorIteration = (i % 5 == 0) || scenario == "errors";
        var isSlowIteration = (i % 4 == 0) || scenario == "slow";

        // 1. Logs
        if (scenario is "all" or "logs" or "errors")
        {
            var logPayload = CreateLogsPayload(serviceName, environment, rng, isErrorIteration);
            var logRes = await SendOtlpAsync(http, $"{endpoint}/v1/logs", logPayload);
            if (!logRes) errorCount++;
        }

        // 2. Traces
        if (scenario is "all" or "traces" or "slow" or "errors")
        {
            var tracePayload = CreateTracesPayload(serviceName, environment, rng, isErrorIteration, isSlowIteration);
            var traceRes = await SendOtlpAsync(http, $"{endpoint}/v1/traces", tracePayload);
            if (!traceRes) errorCount++;
        }

        // 3. Metrics
        if (scenario is "all" or "metrics")
        {
            var metricPayload = CreateMetricsPayload(serviceName, environment, rng);
            var metricRes = await SendOtlpAsync(http, $"{endpoint}/v1/metrics", metricPayload);
            if (!metricRes) errorCount++;
        }

        successCount++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("SUCCESS");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        errorCount++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FAILED: {ex.Message}");
        Console.ResetColor();
    }

    if (i < count && intervalMs > 0)
    {
        await Task.Delay(intervalMs);
    }
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"Completed {count} telemetry cycles: {successCount} successful batches, {errorCount} errors.");
Console.ResetColor();

static string GetArg(string[] args, string name, string fallback)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return fallback;
}

static async Task<bool> SendOtlpAsync<T>(HttpClient http, string url, T payload)
{
    var json = JsonSerializer.Serialize(payload);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    using var response = await http.PostAsync(url, content);
    return response.IsSuccessStatusCode;
}

static OtlpLogsPayload CreateLogsPayload(string serviceName, string environment, Random rng, bool hasError)
{
    var traceId = RandomHex(16);
    var spanId = RandomHex(8);
    var nowNano = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L).ToString();

    var records = new List<OtlpLogRecord>
    {
        new()
        {
            TimeUnixNano = nowNano,
            SeverityNumber = 9,
            SeverityText = "Information",
            Body = new OtlpAnyValue { StringValue = $"HTTP request started: GET /api/v1/payments/status" },
            TraceId = traceId,
            SpanId = spanId,
            Attributes =
            [
                new() { Key = "http.method", Value = new() { StringValue = "GET" } },
                new() { Key = "http.status_code", Value = new() { IntValue = 200 } }
            ]
        }
    };

    if (hasError)
    {
        records.Add(new OtlpLogRecord
        {
            TimeUnixNano = nowNano,
            SeverityNumber = 17,
            SeverityText = "Error",
            Body = new OtlpAnyValue { StringValue = "Payment settlement gateway returned 504 Gateway Timeout" },
            TraceId = traceId,
            SpanId = spanId,
            Attributes =
            [
                new() { Key = "exception.type", Value = new() { StringValue = "System.TimeoutException" } },
                new() { Key = "exception.message", Value = new() { StringValue = "Operation timed out after 3000ms calling https://bank.api/settle" } },
                new() { Key = "exception.stacktrace", Value = new() { StringValue = "   at PaymentGateway.SettleAsync() in Gateway.cs:line 89\n   at PaymentProcessor.ExecuteAsync() in Processor.cs:line 42" } }
            ]
        });
    }

    return new OtlpLogsPayload
    {
        ResourceLogs =
        [
            new()
            {
                Resource = new OtlpResource
                {
                    Attributes =
                    [
                        new() { Key = "service.name", Value = new() { StringValue = serviceName } },
                        new() { Key = "deployment.environment", Value = new() { StringValue = environment } }
                    ]
                },
                ScopeLogs =
                [
                    new()
                    {
                        Scope = new OtlpScope { Name = "SentinelOps.TelemetryGenerator", Version = "1.0.0" },
                        LogRecords = records
                    }
                ]
            }
        ]
    };
}

static OtlpTracesPayload CreateTracesPayload(string serviceName, string environment, Random rng, bool hasError, bool isSlow)
{
    var traceId = RandomHex(16);
    var rootSpanId = RandomHex(8);
    var childSpanId = RandomHex(8);

    var startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var durationMs = isSlow ? rng.Next(800, 2200) : rng.Next(25, 180);
    var startNano = (startMs * 1_000_000L).ToString();
    var endNano = ((startMs + durationMs) * 1_000_000L).ToString();

    var childStartNano = ((startMs + 10) * 1_000_000L).ToString();
    var childEndNano = ((startMs + (durationMs / 2)) * 1_000_000L).ToString();

    return new OtlpTracesPayload
    {
        ResourceSpans =
        [
            new()
            {
                Resource = new OtlpResource
                {
                    Attributes =
                    [
                        new() { Key = "service.name", Value = new() { StringValue = serviceName } },
                        new() { Key = "deployment.environment", Value = new() { StringValue = environment } }
                    ]
                },
                ScopeSpans =
                [
                    new()
                    {
                        Scope = new OtlpScope { Name = "TelemetryGenerator", Version = "1.0.0" },
                        Spans =
                        [
                            new()
                            {
                                TraceId = traceId,
                                SpanId = rootSpanId,
                                Name = "POST /api/v1/checkout",
                                Kind = 1, // Server
                                StartTimeUnixNano = startNano,
                                EndTimeUnixNano = endNano,
                                Status = new OtlpStatus { Code = hasError ? 2 : 1, Description = hasError ? "Transaction failed" : "OK" },
                                Attributes =
                                [
                                    new() { Key = "http.route", Value = new() { StringValue = "/api/v1/checkout" } },
                                    new() { Key = "http.status_code", Value = new() { IntValue = hasError ? 500 : 200 } }
                                ]
                            },
                            new()
                            {
                                TraceId = traceId,
                                SpanId = childSpanId,
                                ParentSpanId = rootSpanId,
                                Name = "db_query:SELECT accounts",
                                Kind = 3, // Client
                                StartTimeUnixNano = childStartNano,
                                EndTimeUnixNano = childEndNano,
                                Status = new OtlpStatus { Code = 1 },
                                Attributes =
                                [
                                    new() { Key = "db.system", Value = new() { StringValue = "postgresql" } },
                                    new() { Key = "db.statement", Value = new() { StringValue = "SELECT balance FROM accounts WHERE user_id = $1" } }
                                ]
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

static OtlpMetricsPayload CreateMetricsPayload(string serviceName, string environment, Random rng)
{
    var nowNano = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L).ToString();

    return new OtlpMetricsPayload
    {
        ResourceMetrics =
        [
            new()
            {
                Resource = new OtlpResource
                {
                    Attributes =
                    [
                        new() { Key = "service.name", Value = new() { StringValue = serviceName } },
                        new() { Key = "deployment.environment", Value = new() { StringValue = environment } }
                    ]
                },
                ScopeMetrics =
                [
                    new()
                    {
                        Scope = new OtlpScope { Name = "TelemetryGenerator", Version = "1.0.0" },
                        Metrics =
                        [
                            new()
                            {
                                Name = "http_requests_total",
                                Description = "Total HTTP requests handled",
                                Unit = "1",
                                Sum = new OtlpSum
                                {
                                    DataPoints =
                                    [
                                        new()
                                        {
                                            TimeUnixNano = nowNano,
                                            AsDouble = rng.Next(100, 5000),
                                            Attributes =
                                            [
                                                new() { Key = "status", Value = new() { StringValue = "200" } }
                                            ]
                                        }
                                    ]
                                }
                            },
                            new()
                            {
                                Name = "process_cpu_usage_ratio",
                                Description = "CPU utilization ratio (0.0 to 1.0)",
                                Unit = "1",
                                Gauge = new OtlpGauge
                                {
                                    DataPoints =
                                    [
                                        new()
                                        {
                                            TimeUnixNano = nowNano,
                                            AsDouble = Math.Round(rng.NextDouble() * 0.75 + 0.15, 3)
                                        }
                                    ]
                                }
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

static string RandomHex(int byteCount)
{
    var bytes = new byte[byteCount];
    RandomNumberGenerator.Fill(bytes);
    return Convert.ToHexStringLower(bytes);
}
