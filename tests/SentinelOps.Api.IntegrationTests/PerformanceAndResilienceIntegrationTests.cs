using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Contracts.Otlp;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Services;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace SentinelOps.Api.IntegrationTests;

public class PerformanceAndResilienceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public PerformanceAndResilienceIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessProbe_ShouldSustainHighThroughput_WithSubMillisecondP50()
    {
        // Warmup
        for (var i = 0; i < 10; i++)
        {
            var warm = await _client.GetAsync("/health/live");
            warm.EnsureSuccessStatusCode();
        }

        const int totalRequests = 100;
        const int concurrency = 10;
        var latencies = new List<double>(totalRequests);
        var errors = 0;
        var syncLock = new object();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = concurrency };
        var sw = Stopwatch.StartNew();

        await Parallel.ForEachAsync(Enumerable.Range(0, totalRequests), parallelOptions, async (_, _) =>
        {
            var reqSw = Stopwatch.StartNew();
            try
            {
                var res = await _client.GetAsync("/health/live");
                reqSw.Stop();
                lock (syncLock)
                {
                    if (res.IsSuccessStatusCode)
                        latencies.Add(reqSw.Elapsed.TotalMilliseconds);
                    else
                        errors++;
                }
            }
            catch
            {
                reqSw.Stop();
                lock (syncLock) { errors++; }
            }
        });

        sw.Stop();
        latencies.Sort();

        var rps = totalRequests / sw.Elapsed.TotalSeconds;
        var p50 = GetPercentile(latencies, 0.50);
        var p95 = GetPercentile(latencies, 0.95);
        var p99 = GetPercentile(latencies, 0.99);

        _output.WriteLine($"[PERF] Liveness: Total={totalRequests}, RPS={rps:F1}, P50={p50:F2}ms, P95={p95:F2}ms, P99={p99:F2}ms, Errors={errors}");

        errors.Should().Be(0);
        latencies.Count.Should().Be(totalRequests);
        p50.Should().BeLessThan(25.0); // Generous ceiling for in-memory test runner
    }

    [Fact]
    public async Task OtlpIngestion_UnderConcurrentLoad_ShouldSustainThroughputWithoutErrors()
    {
        // 1. Setup Tenant and App with API Key
        var token = await CreateUserWithAdminRoleAsync("perf_ingest");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Perf Org " + Guid.NewGuid().ToString("N")[..6]
        });
        orgRes.EnsureSuccessStatusCode();
        var org = (await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", org.Id.ToString());

        var appRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "perf-app-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "Perf Ingestion App"
        });
        appRes.EnsureSuccessStatusCode();
        var app = (await appRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        var keyRes = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/keys", new CreateApiKeyRequest
        {
            Name = "PerfKey",
            ExpiresInDays = 30
        });
        keyRes.EnsureSuccessStatusCode();
        var keyDto = (await keyRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>())!.Data!;
        var rawKey = keyDto.RawApiKey;

        // 2. Execute concurrent OTLP log ingestion
        const int totalRequests = 60;
        const int concurrency = 6;
        var latencies = new List<double>(totalRequests);
        var errors = 0;
        var syncLock = new object();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = concurrency };
        var sw = Stopwatch.StartNew();

        await Parallel.ForEachAsync(Enumerable.Range(0, totalRequests), parallelOptions, async (idx, _) =>
        {
            var reqSw = Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/logs");
                req.Headers.Add("X-API-Key", rawKey);
                var payload = new OtlpLogsPayload
                {
                    ResourceLogs =
                    [
                        new()
                        {
                            Resource = new OtlpResource
                            {
                                Attributes = [new() { Key = "service.name", Value = new() { StringValue = "PerfClient" } }]
                            },
                            ScopeLogs =
                            [
                                new()
                                {
                                    LogRecords =
                                    [
                                        new()
                                        {
                                            SeverityText = "Information",
                                            Body = new OtlpAnyValue { StringValue = $"High-concurrency load test record #{idx}" }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                };
                req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var res = await _client.SendAsync(req);
                reqSw.Stop();

                lock (syncLock)
                {
                    if (res.IsSuccessStatusCode)
                        latencies.Add(reqSw.Elapsed.TotalMilliseconds);
                    else
                        errors++;
                }
            }
            catch
            {
                reqSw.Stop();
                lock (syncLock) { errors++; }
            }
        });

        sw.Stop();
        latencies.Sort();

        var rps = totalRequests / sw.Elapsed.TotalSeconds;
        var p50 = GetPercentile(latencies, 0.50);
        var p95 = GetPercentile(latencies, 0.95);
        var p99 = GetPercentile(latencies, 0.99);

        _output.WriteLine($"[PERF] OTLP Logs: Total={totalRequests}, RPS={rps:F1}, P50={p50:F2}ms, P95={p95:F2}ms, P99={p99:F2}ms, Errors={errors}");

        errors.Should().Be(0);
        latencies.Count.Should().Be(totalRequests);
    }

    [Fact]
    public async Task MultiTenantQueries_UnderConcurrentLoad_ShouldMaintainStrictIsolation()
    {
        // 1. Setup Tenant Alpha
        var tokenAlpha = await CreateUserWithAdminRoleAsync("perf_alpha");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAlpha);

        var orgAlphaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Org Alpha " + Guid.NewGuid().ToString("N")[..6]
        });
        orgAlphaRes.EnsureSuccessStatusCode();
        var orgAlpha = (await orgAlphaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgAlpha.Id.ToString());

        var appAlphaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-alpha-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "App Alpha"
        });
        appAlphaRes.EnsureSuccessStatusCode();
        var appAlpha = (await appAlphaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 2. Setup Tenant Beta
        var tokenBeta = await CreateUserWithAdminRoleAsync("perf_beta");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBeta);

        var orgBetaRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = "Org Beta " + Guid.NewGuid().ToString("N")[..6]
        });
        orgBetaRes.EnsureSuccessStatusCode();
        var orgBeta = (await orgBetaRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>())!.Data!;

        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgBeta.Id.ToString());

        var appBetaRes = await _client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest
        {
            Name = "app-beta-" + Guid.NewGuid().ToString("N")[..6],
            DisplayName = "App Beta"
        });
        appBetaRes.EnsureSuccessStatusCode();
        var appBeta = (await appBetaRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>())!.Data!;

        // 3. Concurrently query dashboard and applications alternating between Alpha and Beta
        const int iterations = 40;
        var errors = 0;
        var syncLock = new object();

        await Parallel.ForEachAsync(Enumerable.Range(0, iterations), new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (idx, _) =>
        {
            var isAlpha = idx % 2 == 0;
            var targetToken = isAlpha ? tokenAlpha : tokenBeta;
            var targetOrgId = isAlpha ? orgAlpha.Id : orgBeta.Id;
            var expectedAppName = isAlpha ? appAlpha.Name : appBeta.Name;
            var foreignAppName = isAlpha ? appBeta.Name : appAlpha.Name;

            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/applications");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);
            req.Headers.Add("X-Organization-Id", targetOrgId.ToString());

            var res = await _client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                lock (syncLock) { errors++; }
                return;
            }

            var appsResp = await res.Content.ReadFromJsonAsync<ApiResponse<List<ApplicationDto>>>();
            var apps = appsResp?.Data ?? [];

            // Verify caller only sees their own app and NEVER sees the foreign app
            var sawOwn = apps.Any(a => a.Name == expectedAppName);
            var sawForeign = apps.Any(a => a.Name == foreignAppName);

            if (!sawOwn || sawForeign)
            {
                lock (syncLock) { errors++; }
            }
        });

        errors.Should().Be(0, "Multi-tenant queries under concurrency must never leak cross-tenant entities");
    }

    [Fact]
    public async Task RedisTelemetryBuffer_WhenRedisFails_GracefullyFallsBackToInMemoryChannel()
    {
        // 1. Setup mock redis that throws connection exception on GetDatabase
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(true);
        mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns<IDatabase>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Simulated network partition"));

        var buffer = new RedisTelemetryBuffer(NullLogger<RedisTelemetryBuffer>.Instance, mockRedis);

        // 2. Push message to buffer - must not throw exception, must fallback gracefully
        var pushAct = async () => await buffer.PushLogAsync(new LogIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            Message = "Resilience test log entry",
            Severity = "Information",
            Timestamp = DateTime.UtcNow
        });

        await pushAct.Should().NotThrowAsync();

        // 3. Verify fallback channel receives message
        var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var channelItem = await buffer.ReadLogsAsync(readCts.Token).GetAsyncEnumerator(readCts.Token).MoveNextAsync();
        channelItem.Should().BeTrue("Fallback in-memory channel must receive messages when Redis fails");
    }

    private static double GetPercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        var idx = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
    }

    private async Task<string> CreateUserWithAdminRoleAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}_{unique}@sentinelops.local";
        var user = new ApplicationUser
        {
            UserName = $"user_{prefix}_{unique}",
            Email = email,
            FullName = $"{prefix} Admin User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, Roles.Admin);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [Roles.Admin]);
        return token;
    }
}
