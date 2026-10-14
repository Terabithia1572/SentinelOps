using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelOps.Contracts.Ingestion;
using SentinelOps.Infrastructure.Services;
using StackExchange.Redis;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class HorizontalScalingAndHighAvailabilityTests
{
    [Fact]
    public void SignalR_RedisBackplaneConfiguration_CanBeEnabledViaConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["SignalR:UseRedisBackplane"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        var signalRBuilder = services.AddSignalR();

        var useRedisBackplane = configuration.GetValue<bool>("SignalR:UseRedisBackplane", false);
        var redisConn = configuration.GetConnectionString("Redis");

        // Act
        if (useRedisBackplane && !string.IsNullOrWhiteSpace(redisConn))
        {
            signalRBuilder.AddStackExchangeRedis(redisConn, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("sentinelops:signalr");
                options.Configuration.AbortOnConnectFail = false;
            });
        }

        var provider = services.BuildServiceProvider();

        // Assert
        useRedisBackplane.Should().BeTrue();
        redisConn.Should().NotBeNullOrWhiteSpace();
        var hubLifetimeManager = provider.GetService<Microsoft.AspNetCore.SignalR.HubLifetimeManager<SentinelOps.Api.Hubs.AlertsHub>>();
        hubLifetimeManager.Should().NotBeNull();
        hubLifetimeManager!.GetType().Name.Should().Contain("RedisHubLifetimeManager");
    }

    [Fact]
    public async Task RedisTelemetryBuffer_PelRecovery_ClaimsAbandonedPendingEntries()
    {
        // Arrange
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(true);

        var mockDb = Substitute.For<IDatabase>();
        mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);

        var logger = Substitute.For<ILogger<RedisTelemetryBuffer>>();

        var pendingId = new RedisValue("1700000000000-0");
        var ctor = typeof(StreamPendingMessageInfo).GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .First(c => c.GetParameters().Length == 4);

        var pendingInfo = (StreamPendingMessageInfo)ctor.Invoke(new object[]
        {
            pendingId,
            (RedisValue)"crashed-consumer-pod-1",
            45000L, // idleTimeInMs > 30s threshold
            2
        })!;

        pendingInfo.IdleTimeInMilliseconds.Should().Be(45000);
        pendingInfo.MessageId.Should().Be(pendingId);

        mockDb.StreamPendingMessagesAsync(
            "sentinelops:stream:logs",
            "sentinelops-worker-group",
            100,
            RedisValue.Null,
            default,
            default,
            CommandFlags.None)
            .Returns(Task.FromResult(new[] { pendingInfo }));

        var testLog = new LogIngestionMessage
        {
            ApplicationId = Guid.NewGuid(),
            Message = "Recovered after worker crash",
            Severity = "Information",
            Timestamp = DateTime.UtcNow
        };
        var serializedLog = JsonSerializer.Serialize(testLog);

        var claimedEntry = new StreamEntry(
            pendingId,
            new NameValueEntry[] { new("data", serializedLog) });

        mockDb.StreamClaimAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<RedisValue>(),
            Arg.Any<long>(),
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(new[] { claimedEntry }));

        var buffer = new RedisTelemetryBuffer(logger, mockRedis);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var recoveredLogs = new List<LogIngestionMessage>();
        await foreach (var log in buffer.ReadLogsAsync(cts.Token))
        {
            recoveredLogs.Add(log);
            if (recoveredLogs.Count >= 1)
            {
                break;
            }
        }

        // Assert
        recoveredLogs.Should().HaveCount(1);
        recoveredLogs[0].Message.Should().Be("Recovered after worker crash");

        // Verify StreamClaimAsync was invoked with the abandoned message ID
        await mockDb.Received(1).StreamClaimAsync(
            Arg.Is<RedisKey>(k => k == "sentinelops:stream:logs"),
            Arg.Is<RedisValue>(v => v == "sentinelops-worker-group"),
            Arg.Any<RedisValue>(),
            Arg.Is<long>(t => t == 30_000),
            Arg.Is<RedisValue[]>(ids => ids.Contains(pendingId)),
            Arg.Any<CommandFlags>());

        // Verify acknowledgment was sent after recovery
        await mockDb.Received(1).StreamAcknowledgeAsync(
            Arg.Is<RedisKey>(k => k == "sentinelops:stream:logs"),
            Arg.Is<RedisValue>(v => v == "sentinelops-worker-group"),
            Arg.Is<RedisValue>(v => v == pendingId),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RedisTelemetryBuffer_MalformedPayload_IsRoutedToDeadLetterStreamAndAcknowledged()
    {
        // Arrange
        var mockRedis = Substitute.For<IConnectionMultiplexer>();
        mockRedis.IsConnected.Returns(true);

        var mockDb = Substitute.For<IDatabase>();
        mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);

        var logger = Substitute.For<ILogger<RedisTelemetryBuffer>>();

        // No pending messages
        mockDb.StreamPendingMessagesAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<int>(),
            Arg.Any<RedisValue>(),
            Arg.Any<RedisValue>(),
            Arg.Any<RedisValue>(),
            Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<StreamPendingMessageInfo>()));

        var badId = new RedisValue("1700000000001-0");
        var badEntry = new StreamEntry(badId, new NameValueEntry[] { new("data", "INVALID_NOT_JSON{") });

        // First call returns bad entry, subsequent calls return empty to stop the loop
        var callCount = 0;
        mockDb.StreamReadGroupAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<RedisValue>(),
            Arg.Any<RedisValue>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount == 1 ? new[] { badEntry } : Array.Empty<StreamEntry>());
            });

        var buffer = new RedisTelemetryBuffer(logger, mockRedis);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        var readMessages = new List<LogIngestionMessage>();
        try
        {
            await foreach (var log in buffer.ReadLogsAsync(cts.Token))
            {
                readMessages.Add(log);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when loop times out
        }

        // Assert - malformed log was skipped, so zero valid logs yielded
        readMessages.Should().BeEmpty();

        // Verify bad payload was written to dead-letter stream
        await mockDb.Received(1).StreamAddAsync(
            Arg.Is<RedisKey>(k => k == "sentinelops:stream:dead-letter"),
            Arg.Any<NameValueEntry[]>(),
            Arg.Any<RedisValue?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CommandFlags>());

        // Verify corrupted message was acknowledged so consumer loop does not poison
        await mockDb.Received(1).StreamAcknowledgeAsync(
            Arg.Is<RedisKey>(k => k == "sentinelops:stream:logs"),
            Arg.Is<RedisValue>(v => v == "sentinelops-worker-group"),
            Arg.Is<RedisValue>(v => v == badId),
            Arg.Any<CommandFlags>());
    }
}
