using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Otlp;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Persistence;
using StackExchange.Redis;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class OtlpIngestionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string ValidApiKey = "sent_live_test_valid_key_123456789";
    private const string RevokedApiKey = "sent_live_test_revoked_key_987654321";

    public OtlpIngestionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DB and Redis registrations for clean isolated integration tests
                var redisDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                    var mockRedis = Substitute.For<IConnectionMultiplexer>();
                    var mockDb = Substitute.For<IDatabase>();
                    mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);
                    services.AddSingleton(mockRedis);
                }

                var dbDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
                }

                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("SentinelOps_OtlpIntegrationTestDb")
                           .UseInternalServiceProvider(inMemoryProvider));
            });
        });

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

        db.Database.EnsureCreated();

        if (!db.Applications.Any())
        {
            var app = new SentinelOps.Domain.Entities.Application
            {
                Name = "otlp-test-app",
                DisplayName = "OTLP Ingestion Test Application",
                Description = "Used for automated integration testing of telemetry ingestion"
            };
            db.Applications.Add(app);

            var activeKey = new ApiKey
            {
                ApplicationId = app.Id,
                Name = "Live Production Key",
                KeyPrefix = "sent_live_test_valid",
                KeyHash = apiKeyService.HashApiKey(ValidApiKey)
            };

            var revokedKey = new ApiKey
            {
                ApplicationId = app.Id,
                Name = "Revoked Key",
                KeyPrefix = "sent_live_test_revoked",
                KeyHash = apiKeyService.HashApiKey(RevokedApiKey),
                RevokedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            db.ApiKeys.AddRange(activeKey, revokedKey);
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task IngestLogs_WithoutApiKey_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new OtlpLogsPayload();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IngestLogs_WithInvalidApiKey_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "sent_invalid_nonexistent_key");
        var payload = new OtlpLogsPayload();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IngestLogs_WithRevokedApiKey_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", RevokedApiKey);
        var payload = new OtlpLogsPayload();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IngestLogs_WithValidApiKey_Returns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ValidApiKey);

        var payload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "BillingService" } }]
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
                                    Body = new OtlpAnyValue { StringValue = "Invoice #1042 generated" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IngestTraces_WithValidApiKey_Returns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ValidApiKey);

        var payload = new OtlpTracesPayload
        {
            ResourceSpans =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "PaymentGateway" } }]
                    },
                    ScopeSpans =
                    [
                        new()
                        {
                            Spans =
                            [
                                new()
                                {
                                    TraceId = "0af7651916cd43dd8448eb211c80319c",
                                    SpanId = "b7ad6b7169203331",
                                    Name = "AuthorizePayment",
                                    StartTimeUnixNano = "1700000000000000000",
                                    EndTimeUnixNano = "1700000000050000000"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/traces", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IngestMetrics_WithValidApiKey_Returns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ValidApiKey);

        var payload = new OtlpMetricsPayload
        {
            ResourceMetrics =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "TelemetryProducer" } }]
                    },
                    ScopeMetrics =
                    [
                        new()
                        {
                            Metrics =
                            [
                                new()
                                {
                                    Name = "system.cpu.utilization",
                                    Gauge = new OtlpGauge
                                    {
                                        DataPoints = [new() { AsDouble = 0.42 }]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/metrics", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IngestLogs_WithBearerAuthorizationHeader_Returns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidApiKey);

        var payload = new OtlpLogsPayload();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IngestLogs_WithSpoofedApplicationAttributes_StrictlyAttributedToKeyOwner()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ValidApiKey);

        var spoofedAppId = Guid.NewGuid().ToString();
        var payload = new OtlpLogsPayload
        {
            ResourceLogs =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes =
                        [
                            new() { Key = "application.id", Value = new() { StringValue = spoofedAppId } },
                            new() { Key = "service.name", Value = new() { StringValue = "UntrustedAttackerService" } }
                        ]
                    },
                    ScopeLogs =
                    [
                        new()
                        {
                            LogRecords =
                            [
                                new()
                                {
                                    SeverityText = "Warning",
                                    Body = new OtlpAnyValue { StringValue = "Trying to forge application ownership" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/logs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify that background TelemetryConsumerService persisted the message with authenticated app ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var authenticatedApp = await db.Applications.FirstAsync(a => a.Name == "otlp-test-app");

        LogEntry? persistedLog = null;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(100);
            using var checkScope = _factory.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            persistedLog = await checkDb.LogEntries
                .FirstOrDefaultAsync(l => l.Message == "Trying to forge application ownership");
            if (persistedLog != null) break;
        }

        persistedLog.Should().NotBeNull();
        persistedLog!.ApplicationId.Should().Be(authenticatedApp.Id);
        persistedLog.ApplicationId.ToString().Should().NotBe(spoofedAppId);
    }

    [Fact]
    public async Task TelemetryBuffer_WhenRedisIsOffline_OperatesReliablyViaFallbackChannels()
    {
        // Arrange
        var loggerMock = Substitute.For<Microsoft.Extensions.Logging.ILogger<SentinelOps.Infrastructure.Services.RedisTelemetryBuffer>>();
        var buffer = new SentinelOps.Infrastructure.Services.RedisTelemetryBuffer(loggerMock, null);

        var testAppId = Guid.NewGuid();
        var logMsg = new SentinelOps.Contracts.Ingestion.LogIngestionMessage
        {
            ApplicationId = testAppId,
            ServiceName = "FallbackTestService",
            Severity = "Information",
            Message = "Testing in-memory fallback buffer"
        };

        // Act - push into buffer
        await buffer.PushLogAsync(logMsg);

        // Assert - read from fallback channel
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = await buffer.ReadLogsAsync(cts.Token).FirstOrDefaultAsync(cts.Token);

        received.Should().NotBeNull();
        received!.ApplicationId.Should().Be(testAppId);
        received.Message.Should().Be("Testing in-memory fallback buffer");
    }

    [Fact]
    public async Task IngestTraces_WithDuplicateSpans_PersistsWithoutFailureAndDeduplicates()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ValidApiKey);

        var traceId = Guid.NewGuid().ToString("N");
        var spanId = Guid.NewGuid().ToString("N")[..16];

        OtlpTracesPayload CreatePayload() => new()
        {
            ResourceSpans =
            [
                new()
                {
                    Resource = new OtlpResource
                    {
                        Attributes = [new() { Key = "service.name", Value = new() { StringValue = "DeduplicationService" } }]
                    },
                    ScopeSpans =
                    [
                        new()
                        {
                            Spans =
                            [
                                new()
                                {
                                    TraceId = traceId,
                                    SpanId = spanId,
                                    Name = "DeduplicatedOperation",
                                    StartTimeUnixNano = "1700000000000000000",
                                    EndTimeUnixNano = "1700000000050000000"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act - send first time
        var content1 = new StringContent(JsonSerializer.Serialize(CreatePayload()), Encoding.UTF8, "application/json");
        var response1 = await client.PostAsync("/v1/traces", content1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - send duplicate
        var content2 = new StringContent(JsonSerializer.Serialize(CreatePayload()), Encoding.UTF8, "application/json");
        var response2 = await client.PostAsync("/v1/traces", content2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Wait for background persistence
        using var scope = _factory.Services.CreateScope();
        SpanRecord? persistedSpan = null;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(100);
            using var checkScope = _factory.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            persistedSpan = await checkDb.SpanRecords
                .FirstOrDefaultAsync(s => s.TraceId == traceId && s.SpanId == spanId);
            if (persistedSpan != null) break;
        }

        persistedSpan.Should().NotBeNull();

        // Verify span was not duplicated
        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var spanCount = await finalDb.SpanRecords.CountAsync(s => s.TraceId == traceId && s.SpanId == spanId);
        spanCount.Should().Be(1);

        var traceCount = await finalDb.TraceRecords.CountAsync(t => t.TraceId == traceId);
        traceCount.Should().Be(1);
    }
}
