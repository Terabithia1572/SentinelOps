using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SentinelOps.Contracts.Otlp;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("===============================================================================");
Console.WriteLine("        SENTINELOPS — ENTERPRISE PERFORMANCE BENCHMARK ENGINE                 ");
Console.WriteLine("===============================================================================");
Console.ResetColor();

var endpoint = GetArg(args, "--endpoint", "http://localhost:5100");
var email = GetArg(args, "--email", "qa.admin@sentinelops.local");
var password = GetArg(args, "--password", "P@ssword2026!Secure");
var apiKey = GetArg(args, "--api-key", "sent_live_default_key");
var profile = GetArg(args, "--profile", "baseline").ToLowerInvariant();

var (totalRequests, concurrency) = profile switch
{
    "smoke" => (20, 2),
    "stress" => (300, 15),
    "soak" => (500, 10),
    _ => (100, 5) // baseline
};

if (int.TryParse(GetArg(args, "--requests", ""), out var rOverride)) totalRequests = rOverride;
if (int.TryParse(GetArg(args, "--concurrency", ""), out var cOverride)) concurrency = cOverride;

Console.WriteLine($"Target Endpoint:    {endpoint}");
Console.WriteLine($"Profile:            {profile.ToUpperInvariant()} (Requests: {totalRequests}, Concurrency: {concurrency})");
Console.WriteLine($"Audit Timestamp:    {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine();

using var httpClient = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromSeconds(30) };

// 1. Authenticate to acquire Bearer JWT
Console.Write("Authenticating administrator session... ");
string? jwtToken = null;
try
{
    var loginPayload = JsonSerializer.Serialize(new { email, password });
    var loginContent = new StringContent(loginPayload, Encoding.UTF8, "application/json");
    var authRes = await httpClient.PostAsync("/api/auth/login", loginContent);
    if (authRes.IsSuccessStatusCode)
    {
        var authDoc = JsonDocument.Parse(await authRes.Content.ReadAsStringAsync());
        jwtToken = authDoc.RootElement.GetProperty("data").GetProperty("token").GetString();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("SUCCESS (JWT acquired)");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"FAILED (Status {authRes.StatusCode}). Testing unauthenticated endpoints only.");
        Console.ResetColor();
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Console.WriteLine("Running performance scenarios...");
Console.WriteLine("-------------------------------------------------------------------------------");

var results = new List<BenchmarkResult>();

// SCENARIO 1: Healthcheck Endpoint (Baseline Web/API throughput)
results.Add(await RunScenarioAsync(
    name: "System Liveness Probe (/health/live)",
    totalRequests: totalRequests,
    concurrency: concurrency,
    requestFactory: _ => new HttpRequestMessage(HttpMethod.Get, "/health/live")));

// SCENARIO 2: OTLP Log Ingestion Throughput (Write-heavy ingestion path)
results.Add(await RunScenarioAsync(
    name: "OTLP Log Ingestion (/v1/logs)",
    totalRequests: totalRequests,
    concurrency: concurrency,
    requestFactory: i =>
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/logs");
        req.Headers.Add("X-API-Key", apiKey);
        var payload = CreateSampleLogPayload(i);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return req;
    }));

// SCENARIO 3: OTLP Traces Ingestion Throughput
results.Add(await RunScenarioAsync(
    name: "OTLP Trace Ingestion (/v1/traces)",
    totalRequests: totalRequests,
    concurrency: concurrency,
    requestFactory: i =>
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/traces");
        req.Headers.Add("X-API-Key", apiKey);
        var payload = CreateSampleTracePayload(i);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return req;
    }));

if (!string.IsNullOrEmpty(jwtToken))
{
    // SCENARIO 4: Authenticated Dashboard Overview Query (Read-heavy aggregation)
    results.Add(await RunScenarioAsync(
        name: "Dashboard KPI Summary (/api/dashboard/summary)",
        totalRequests: totalRequests,
        concurrency: concurrency,
        requestFactory: _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/summary");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            return req;
        }));

    // SCENARIO 5: Log Explorer Filtered Query (Pagination & PostgreSQL Indexing)
    results.Add(await RunScenarioAsync(
        name: "Log Explorer Filtered (/api/logs?pageSize=50)",
        totalRequests: totalRequests,
        concurrency: concurrency,
        requestFactory: _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/logs?page=1&pageSize=50&severity=Error");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            return req;
        }));

    // SCENARIO 6: Trace Explorer Query
    results.Add(await RunScenarioAsync(
        name: "Trace Explorer Query (/api/traces?pageSize=50)",
        totalRequests: totalRequests,
        concurrency: concurrency,
        requestFactory: _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/traces?page=1&pageSize=50");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            return req;
        }));

    // SCENARIO 7: System Component Health
    results.Add(await RunScenarioAsync(
        name: "Component Health Diagnostic (/api/system/health)",
        totalRequests: totalRequests,
        concurrency: concurrency,
        requestFactory: _ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/system/health");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            return req;
        }));
}

// PRINT RESULTS TABLE
Console.WriteLine();
Console.WriteLine("====================================================================================================================");
Console.WriteLine("                                           BENCHMARK RESULTS SUMMARY                                                ");
Console.WriteLine("====================================================================================================================");
Console.WriteLine(string.Format("{0,-38} | {1,6} | {2,8} | {3,8} | {4,8} | {5,8} | {6,8} | {7,8} | {8,6}",
    "Scenario", "Count", "Req/sec", "Avg (ms)", "P50 (ms)", "P95 (ms)", "P99 (ms)", "Max (ms)", "Errors"));
Console.WriteLine(new string('-', 116));

foreach (var r in results)
{
    Console.WriteLine(string.Format("{0,-38} | {1,6} | {2,8:F1} | {3,8:F2} | {4,8:F2} | {5,8:F2} | {6,8:F2} | {7,8:F2} | {8,6}",
        r.ScenarioName, r.TotalRequests, r.RequestsPerSecond, r.AverageMs, r.P50Ms, r.P95Ms, r.P99Ms, r.MaxMs, r.Errors));
}
Console.WriteLine("====================================================================================================================");

async Task<BenchmarkResult> RunScenarioAsync(
    string name,
    int totalRequests,
    int concurrency,
    Func<int, HttpRequestMessage> requestFactory)
{
    Console.Write($"Executing: {name}... ");
    var latencies = new List<double>(totalRequests);
    var errors = 0;
    var syncLock = new object();

    var stopwatch = Stopwatch.StartNew();
    var queue = Enumerable.Range(1, totalRequests).ToList();
    var tasks = new List<Task>();

    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = concurrency };
    await Parallel.ForEachAsync(queue, parallelOptions, async (idx, ct) =>
    {
        var req = requestFactory(idx);
        var reqSw = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient.SendAsync(req, ct);
            reqSw.Stop();
            var ms = reqSw.Elapsed.TotalMilliseconds;
            lock (syncLock)
            {
                if (response.IsSuccessStatusCode)
                {
                    latencies.Add(ms);
                }
                else
                {
                    errors++;
                }
            }
        }
        catch
        {
            reqSw.Stop();
            lock (syncLock)
            {
                errors++;
            }
        }
    });

    stopwatch.Stop();
    var totalSeconds = stopwatch.Elapsed.TotalSeconds;
    var rps = totalSeconds > 0 ? (latencies.Count + errors) / totalSeconds : 0;

    latencies.Sort();
    var avg = latencies.Count > 0 ? latencies.Average() : 0;
    var p50 = GetPercentile(latencies, 0.50);
    var p95 = GetPercentile(latencies, 0.95);
    var p99 = GetPercentile(latencies, 0.99);
    var max = latencies.Count > 0 ? latencies.Last() : 0;

    Console.ForegroundColor = errors == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine($"DONE ({rps:F1} req/s, avg {avg:F1} ms)");
    Console.ResetColor();

    return new BenchmarkResult(name, totalRequests, rps, avg, p50, p95, p99, max, errors);
}

static double GetPercentile(List<double> sorted, double percentile)
{
    if (sorted.Count == 0) return 0;
    var idx = (int)Math.Ceiling(percentile * sorted.Count) - 1;
    return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
}

static OtlpLogsPayload CreateSampleLogPayload(int index) => new()
{
    ResourceLogs =
    [
        new()
        {
            Resource = new OtlpResource
            {
                Attributes = [new() { Key = "service.name", Value = new() { StringValue = "BenchmarkClient" } }]
            },
            ScopeLogs =
            [
                new()
                {
                    LogRecords =
                    [
                        new()
                        {
                            SeverityText = index % 5 == 0 ? "Error" : "Information",
                            Body = new OtlpAnyValue { StringValue = $"Benchmark payload iteration #{index}" }
                        }
                    ]
                }
            ]
        }
    ]
};

static OtlpTracesPayload CreateSampleTracePayload(int index) => new()
{
    ResourceSpans =
    [
        new()
        {
            Resource = new OtlpResource
            {
                Attributes = [new() { Key = "service.name", Value = new() { StringValue = "BenchmarkClient" } }]
            },
            ScopeSpans =
            [
                new()
                {
                    Spans =
                    [
                        new()
                        {
                            TraceId = Guid.NewGuid().ToString("N"),
                            SpanId = Guid.NewGuid().ToString("N")[..16],
                            Name = "BenchmarkOperation",
                            StartTimeUnixNano = "1700000000000000000",
                            EndTimeUnixNano = "1700000000025000000"
                        }
                    ]
                }
            ]
        }
    ]
};

static string GetArg(string[] args, string flag, string fallback)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return fallback;
}

record BenchmarkResult(
    string ScenarioName,
    int TotalRequests,
    double RequestsPerSecond,
    double AverageMs,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    int Errors);
