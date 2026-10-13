using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class ObservabilityExplorerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ObservabilityExplorerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync(string role = "Viewer")
    {
        var email = $"user_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"user_{Guid.NewGuid():N}"[..12];
        const string password = "P@ssword2026!Secure";

        using (var scope = _factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identityService.CreateUserAsync(userName, email, "Explorer Test User", password, role);
        }

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var auth = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Data!.Token);
    }

    [Fact]
    public async Task UnauthenticatedRequests_MustReturnUnauthorized()
    {
        var unauthedClient = _factory.CreateClient();

        var logsRes = await unauthedClient.GetAsync("/api/logs");
        logsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var tracesRes = await unauthedClient.GetAsync("/api/traces");
        tracesRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var metricsRes = await unauthedClient.GetAsync("/api/metrics/timeseries");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var exceptionsRes = await unauthedClient.GetAsync("/api/exceptions");
        exceptionsRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var servicesRes = await unauthedClient.GetAsync("/api/services/health");
        servicesRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogsExplorer_ShouldFilterAndPaginateLogsCorrectly()
    {
        await AuthenticateAsync();

        var appId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString("N");
        Guid logId;

        // Seed test logs
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var entry = new LogEntry
            {
                ApplicationId = appId,
                ServiceName = "order-service",
                EnvironmentName = "Staging",
                Severity = Severity.Error,
                Message = "Payment checkout timeout encountered",
                ExceptionDetail = "System.TimeoutException: The gateway timed out\n   at PaymentGateway.Process()",
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            };
            await db.LogEntries.AddAsync(entry);
            await db.SaveChangesAsync(CancellationToken.None);
            logId = entry.Id;
        }

        // 1. Query logs by search keyword
        var searchRes = await _client.GetAsync($"/api/logs?applicationId={appId}&search=checkout");
        searchRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchBody = await searchRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>();
        searchBody.Should().NotBeNull();
        searchBody!.Data.Should().NotBeNull();
        searchBody.Data!.Items.Should().Contain(l => l.Id == logId);

        // 2. Query log by ID
        var detailRes = await _client.GetAsync($"/api/logs/{logId}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailBody = await detailRes.Content.ReadFromJsonAsync<ApiResponse<LogEntryDto>>();
        detailBody.Should().NotBeNull();
        detailBody!.Data.Should().NotBeNull();
        detailBody.Data!.Id.Should().Be(logId);
        detailBody.Data.TraceId.Should().Be(traceId);

        // 3. Query metadata
        var metaRes = await _client.GetAsync($"/api/logs/metadata?applicationId={appId}");
        metaRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var metaBody = await metaRes.Content.ReadFromJsonAsync<ApiResponse<Application.Logs.Queries.GetLogMetadata.LogMetadataDto>>();
        metaBody.Should().NotBeNull();
        metaBody!.Data.Should().NotBeNull();
        metaBody.Data!.Services.Should().Contain("order-service");
        metaBody.Data.Environments.Should().Contain("Staging");
    }

    [Fact]
    public async Task TracesExplorer_ShouldReturnTraceAndWaterfallHierarchy()
    {
        await AuthenticateAsync();

        var appId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString("N");
        var rootSpanId = "span-root-1";
        var childSpanId = "span-child-2";
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            await db.TraceRecords.AddAsync(new TraceRecord
            {
                TraceId = traceId,
                ApplicationId = appId,
                ServiceName = "gateway-service",
                EnvironmentName = "Production",
                RootSpanName = "GET /api/checkout",
                Duration = TimeSpan.FromMilliseconds(450),
                SpanCount = 2,
                HasErrors = true,
                StartedAtUtc = now
            });

            await db.SpanRecords.AddRangeAsync(
                new SpanRecord
                {
                    SpanId = rootSpanId,
                    TraceId = traceId,
                    ParentSpanId = null,
                    OperationName = "GET /api/checkout",
                    ServiceName = "gateway-service",
                    Duration = TimeSpan.FromMilliseconds(450),
                    Status = "Error",
                    StartedAtUtc = now
                },
                new SpanRecord
                {
                    SpanId = childSpanId,
                    TraceId = traceId,
                    ParentSpanId = rootSpanId,
                    OperationName = "POST /charge",
                    ServiceName = "billing-service",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Status = "Error",
                    StartedAtUtc = now.AddMilliseconds(50)
                }
            );

            await db.LogEntries.AddAsync(new LogEntry
            {
                ApplicationId = appId,
                ServiceName = "billing-service",
                EnvironmentName = "Production",
                Severity = Severity.Error,
                Message = "Charge declined",
                TraceId = traceId,
                SpanId = childSpanId,
                Timestamp = now.AddMilliseconds(210)
            });

            await db.SaveChangesAsync(CancellationToken.None);
        }

        // Query trace list
        var tracesRes = await _client.GetAsync($"/api/traces?applicationId={appId}&hasErrors=true");
        tracesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracesBody = await tracesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<TraceDto>>>();
        tracesBody.Should().NotBeNull();
        tracesBody!.Data.Should().NotBeNull();
        tracesBody.Data!.Items.Should().Contain(t => t.TraceId == traceId);

        // Query waterfall detail
        var waterfallRes = await _client.GetAsync($"/api/traces/{traceId}");
        waterfallRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var waterfallBody = await waterfallRes.Content.ReadFromJsonAsync<ApiResponse<TraceDetailDto>>();
        waterfallBody.Should().NotBeNull();
        waterfallBody!.Data.Should().NotBeNull();
        waterfallBody.Data!.Trace.TraceId.Should().Be(traceId);
        waterfallBody.Data.Spans.Should().HaveCount(2);

        var childSpan = waterfallBody.Data.Spans.First(s => s.SpanId == childSpanId);
        childSpan.Depth.Should().Be(1);
        childSpan.ParentSpanId.Should().Be(rootSpanId);
        childSpan.OffsetMs.Should().BeApproximately(50, 10);

        waterfallBody.Data.CorrelatedLogs.Should().HaveCount(1);
        waterfallBody.Data.CorrelatedLogs[0].SpanId.Should().Be(childSpanId);
        waterfallBody.Data.Services.Should().Contain("gateway-service");
        waterfallBody.Data.Services.Should().Contain("billing-service");
    }

    [Fact]
    public async Task MetricsTimeSeries_ShouldAggregatePointsAndPercentiles()
    {
        await AuthenticateAsync();

        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            for (int i = 0; i < 10; i++)
            {
                await db.TraceRecords.AddAsync(new TraceRecord
                {
                    TraceId = Guid.NewGuid().ToString("N"),
                    ApplicationId = appId,
                    ServiceName = "metrics-test-svc",
                    EnvironmentName = "Production",
                    RootSpanName = "GET /test",
                    Duration = TimeSpan.FromMilliseconds(50 + i * 20),
                    SpanCount = 1,
                    HasErrors = i % 2 == 0,
                    StartedAtUtc = now.AddMinutes(-5 + (i * 0.5))
                });
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }

        var metricsRes = await _client.GetAsync($"/api/metrics/timeseries?applicationId={appId}&serviceName=metrics-test-svc&bucketMinutes=5");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var metricsBody = await metricsRes.Content.ReadFromJsonAsync<ApiResponse<MetricSeriesResponse>>();

        metricsBody.Should().NotBeNull();
        metricsBody!.Data.Should().NotBeNull();
        metricsBody.Data!.TotalRequests.Should().Be(10);
        metricsBody.Data.OverallErrorRate.Should().Be(50.0);
        metricsBody.Data.AvgLatencyMs.Should().BeGreaterThan(0);
        metricsBody.Data.P95LatencyMs.Should().BeGreaterThan(0);
        metricsBody.Data.Points.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExceptionsExplorer_ShouldGroupErrorsByFingerprint()
    {
        await AuthenticateAsync();

        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            for (int i = 0; i < 3; i++)
            {
                await db.LogEntries.AddAsync(new LogEntry
                {
                    ApplicationId = appId,
                    ServiceName = "catalog-service",
                    EnvironmentName = "Production",
                    Severity = Severity.Error,
                    Message = "ItemNotFound: Product 123 does not exist",
                    ExceptionDetail = "ItemNotFoundException: Product 123 does not exist\n   at Catalog.Get()",
                    Timestamp = now.AddMinutes(-i)
                });
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }

        var exceptionsRes = await _client.GetAsync($"/api/exceptions?applicationId={appId}&serviceName=catalog-service");
        exceptionsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var exceptionsBody = await exceptionsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ExceptionGroupDto>>>();

        exceptionsBody.Should().NotBeNull();
        exceptionsBody!.Data.Should().NotBeNull();
        exceptionsBody.Data!.Should().NotBeEmpty();
        var group = exceptionsBody.Data.First(g => g.ServiceName == "catalog-service");
        group.Count.Should().Be(3);
        group.Fingerprint.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ServiceHealth_ShouldComputeCorrectHealthStatus()
    {
        await AuthenticateAsync();

        var appId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Healthy service (<2% error rate)
            for (int i = 0; i < 20; i++)
            {
                await db.TraceRecords.AddAsync(new TraceRecord
                {
                    TraceId = Guid.NewGuid().ToString("N"),
                    ApplicationId = appId,
                    ServiceName = "healthy-svc",
                    EnvironmentName = "Production",
                    RootSpanName = "GET /ok",
                    Duration = TimeSpan.FromMilliseconds(50),
                    SpanCount = 1,
                    HasErrors = false,
                    StartedAtUtc = now.AddMinutes(-1)
                });
            }

            // Unhealthy service (>10% error rate)
            for (int i = 0; i < 10; i++)
            {
                await db.TraceRecords.AddAsync(new TraceRecord
                {
                    TraceId = Guid.NewGuid().ToString("N"),
                    ApplicationId = appId,
                    ServiceName = "unhealthy-svc",
                    EnvironmentName = "Production",
                    RootSpanName = "GET /fail",
                    Duration = TimeSpan.FromMilliseconds(4000),
                    SpanCount = 1,
                    HasErrors = true,
                    StartedAtUtc = now.AddMinutes(-1)
                });
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }

        var healthRes = await _client.GetAsync($"/api/services/health?applicationId={appId}");
        healthRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var healthBody = await healthRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ServiceHealthSummaryDto>>>();

        healthBody.Should().NotBeNull();
        healthBody!.Data.Should().NotBeNull();
        var healthy = healthBody.Data!.FirstOrDefault(s => s.ServiceName == "healthy-svc");
        healthy.Should().NotBeNull();
        healthy!.Status.Should().Be("Healthy");

        var unhealthy = healthBody.Data.FirstOrDefault(s => s.ServiceName == "unhealthy-svc");
        unhealthy.Should().NotBeNull();
        unhealthy!.Status.Should().Be("Unhealthy");
    }
}
