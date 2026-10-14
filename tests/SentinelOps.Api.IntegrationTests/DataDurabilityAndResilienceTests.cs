using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelOps.Application.Common.Exceptions;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Infrastructure.Services;
using StackExchange.Redis;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class DataDurabilityAndResilienceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DataDurabilityAndResilienceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RedisTelemetryBuffer_WhenDurableStorageRequiredAndRedisDisconnected_FailsClosedWithException()
    {
        // Arrange: Require durable storage, but Redis is disconnected
        var logger = Substitute.For<ILogger<RedisTelemetryBuffer>>();
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(false);

        var buffer = new RedisTelemetryBuffer(logger, mockRedis, requireDurableStorage: true);

        var logMessage = new LogIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            Message = "Production log that must not be lost silently",
            Severity = "Error",
            Timestamp = DateTime.UtcNow
        };

        var traceMessage = new TraceIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            TraceId = "0123456789abcdef0123456789abcdef",
            RootSpanName = "GET /api/checkout",
            DurationMs = 125,
            StartedAtUtc = DateTime.UtcNow
        };

        var metricMessage = new MetricIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            MetricName = "orders_processed_total",
            MetricType = "Counter",
            Value = 1,
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        buffer.RequireDurableStorage.Should().BeTrue();
        buffer.IsDurableStorageActive.Should().BeFalse();

        await FluentActions.Invoking(() => buffer.PushLogAsync(logMessage))
            .Should().ThrowAsync<DurableBufferUnavailableException>();

        await FluentActions.Invoking(() => buffer.PushTraceAsync(traceMessage))
            .Should().ThrowAsync<DurableBufferUnavailableException>();

        await FluentActions.Invoking(() => buffer.PushMetricAsync(metricMessage))
            .Should().ThrowAsync<DurableBufferUnavailableException>();
    }

    [Fact]
    public async Task RedisTelemetryBuffer_WhenDurableStorageNotRequiredAndRedisDisconnected_AllowsInMemoryFallback()
    {
        // Arrange: Development/testing mode where in-memory fallback is acceptable
        var logger = Substitute.For<ILogger<RedisTelemetryBuffer>>();
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(false);

        var buffer = new RedisTelemetryBuffer(logger, mockRedis, requireDurableStorage: false);

        var logMessage = new LogIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            Message = "Dev log buffered in memory",
            Severity = "Information",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        buffer.RequireDurableStorage.Should().BeFalse();
        buffer.IsDurableStorageActive.Should().BeFalse();

        // Push should NOT throw
        await buffer.PushLogAsync(logMessage);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var readLogs = new List<LogIngestionMessage>();
        await foreach (var log in buffer.ReadLogsAsync(cts.Token))
        {
            readLogs.Add(log);
            if (readLogs.Count >= 1) break;
        }

        readLogs.Should().HaveCount(1);
        readLogs[0].Message.Should().Be("Dev log buffered in memory");
    }

    [Fact]
    public async Task TelemetryBufferHealthCheck_WhenDurableStorageRequiredAndRedisDisconnected_ReportsUnhealthy()
    {
        // Arrange
        var mockBuffer = Substitute.For<ITelemetryBuffer>();
        mockBuffer.RequireDurableStorage.Returns(true);
        mockBuffer.IsDurableStorageActive.Returns(false);

        var healthCheck = new TelemetryBufferHealthCheck(mockBuffer);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Durable Redis stream telemetry buffer is disconnected");
    }

    [Fact]
    public async Task TelemetryBufferHealthCheck_WhenDurableStorageRequiredAndRedisConnected_ReportsHealthy()
    {
        // Arrange
        var mockBuffer = Substitute.For<ITelemetryBuffer>();
        mockBuffer.RequireDurableStorage.Returns(true);
        mockBuffer.IsDurableStorageActive.Returns(true);

        var healthCheck = new TelemetryBufferHealthCheck(mockBuffer);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("operational");
    }

    [Fact]
    public async Task TelemetryBufferHealthCheck_WhenDurableStorageNotRequired_ReportsHealthyWithFallbackNotice()
    {
        // Arrange
        var mockBuffer = Substitute.For<ITelemetryBuffer>();
        mockBuffer.RequireDurableStorage.Returns(false);
        mockBuffer.IsDurableStorageActive.Returns(false);

        var healthCheck = new TelemetryBufferHealthCheck(mockBuffer);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("fallback in-memory mode enabled");
    }

    [Fact]
    public async Task IngestionEndpoint_WhenBufferThrowsDurableBufferUnavailableException_Returns503WithRetryAfter()
    {
        // Arrange: Custom client with ITelemetryBuffer configured to throw DurableBufferUnavailableException
        var throwingBuffer = Substitute.For<ITelemetryBuffer>();
        throwingBuffer.PushLogsAsync(Arg.Any<IEnumerable<LogIngestionMessage>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DurableBufferUnavailableException("Durable storage offline"));

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITelemetryBuffer));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(throwingBuffer);
            });
        });

        var client = customFactory.CreateClient();

        // Create an application in this factory's scope to get a valid API key
        var apiKey = await CreateAppAndApiKeyAsync(customFactory);

        var payload = new
        {
            resourceLogs = new[]
            {
                new
                {
                    scopeLogs = new[]
                    {
                        new
                        {
                            logRecords = new[]
                            {
                                new
                                {
                                    timeUnixNano = "1700000000000000000",
                                    severityText = "ERROR",
                                    body = new { stringValue = "Critical order failure" }
                                }
                            }
                        }
                    }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/logs")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-API-Key", apiKey);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.Contains("Retry-After").Should().BeTrue();
        response.Headers.GetValues("Retry-After").First().Should().Be("5");

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Telemetry Ingestion Unavailable");
    }

    private static async Task<string> CreateAppAndApiKeyAsync(WebApplicationFactory<Program> factory)
    {
        const string rawApiKey = "sent_live_durability_test_key_123456789";
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SentinelOps.Infrastructure.Persistence.ApplicationDbContext>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        await db.Database.EnsureCreatedAsync();

        var existingKey = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.ApiKeys, k => k.KeyPrefix == "sent_live_durability");

        if (existingKey == null)
        {
            var app = new SentinelOps.Domain.Entities.Application
            {
                Name = "durability-test-app",
                DisplayName = "Durability Test Application",
                Description = "Used for testing fail-closed durability policies"
            };
            db.Applications.Add(app);

            var key = new SentinelOps.Domain.Entities.ApiKey
            {
                ApplicationId = app.Id,
                Name = "Durability Test Key",
                KeyPrefix = "sent_live_durability",
                KeyHash = apiKeyService.HashApiKey(rawApiKey)
            };
            db.ApiKeys.Add(key);
            await db.SaveChangesAsync();
        }

        return rawApiKey;
    }
}
