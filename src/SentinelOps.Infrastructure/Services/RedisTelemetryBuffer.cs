using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Ingestion;
using StackExchange.Redis;

namespace SentinelOps.Infrastructure.Services;

public class RedisTelemetryBuffer : ITelemetryBuffer
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisTelemetryBuffer> _logger;

    private const string LogsStreamKey = "sentinelops:stream:logs";
    private const string TracesStreamKey = "sentinelops:stream:traces";
    private const string MetricsStreamKey = "sentinelops:stream:metrics";
    private const string DeadLetterStreamKey = "sentinelops:stream:dead-letter";
    private const string ConsumerGroup = "sentinelops-worker-group";
    private const int MaxStreamLength = 100_000;

    // Resilient in-memory fallback channels for standalone runs & test environments
    private readonly Channel<LogIngestionMessage> _fallbackLogChannel = Channel.CreateBounded<LogIngestionMessage>(
        new BoundedChannelOptions(MaxStreamLength) { FullMode = BoundedChannelFullMode.Wait });
    private readonly Channel<TraceIngestionMessage> _fallbackTraceChannel = Channel.CreateBounded<TraceIngestionMessage>(
        new BoundedChannelOptions(MaxStreamLength) { FullMode = BoundedChannelFullMode.Wait });
    private readonly Channel<MetricIngestionMessage> _fallbackMetricChannel = Channel.CreateBounded<MetricIngestionMessage>(
        new BoundedChannelOptions(MaxStreamLength) { FullMode = BoundedChannelFullMode.Wait });

    private readonly bool _requireDurableStorage;

    public bool IsDurableStorageActive => _redis != null && _redis.IsConnected;
    public bool RequireDurableStorage => _requireDurableStorage;

    public RedisTelemetryBuffer(
        ILogger<RedisTelemetryBuffer> logger,
        IConnectionMultiplexer? redis = null,
        Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : this(logger, redis, string.Equals(configuration?["TelemetryBuffer:RequireDurableStorage"], "true", StringComparison.OrdinalIgnoreCase))
    {
    }

    public RedisTelemetryBuffer(
        ILogger<RedisTelemetryBuffer> logger,
        IConnectionMultiplexer? redis,
        bool requireDurableStorage)
    {
        _logger = logger;
        _redis = redis;
        _requireDurableStorage = requireDurableStorage;

        EnsureConsumerGroups();
    }

    private void EnsureConsumerGroups()
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var db = _redis.GetDatabase();
            CreateGroupIfNotExists(db, LogsStreamKey);
            CreateGroupIfNotExists(db, TracesStreamKey);
            CreateGroupIfNotExists(db, MetricsStreamKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Redis Stream consumer groups; fallback channels active.");
        }
    }

    private static void CreateGroupIfNotExists(IDatabase db, string streamKey)
    {
        try
        {
            db.StreamCreateConsumerGroup(streamKey, ConsumerGroup, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Consumer group already exists - expected
        }
    }

    public async Task PushLogAsync(LogIngestionMessage message, CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(message);
                var entry = new NameValueEntry[] { new("data", json), new("appId", message.ApplicationId.ToString()) };
                await db.StreamAddAsync(LogsStreamKey, entry, maxLength: MaxStreamLength, useApproximateMaxLength: true);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis stream write failed for log.");
                if (_requireDurableStorage)
                {
                    throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering failed; Redis stream write error in production fail-closed mode.", ex);
                }
            }
        }
        else if (_requireDurableStorage)
        {
            _logger.LogError("Durable storage is required in production fail-closed mode, but Redis is disconnected. Rejecting telemetry log.");
            throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering is unavailable; Redis is not connected.");
        }

        await _fallbackLogChannel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task PushLogsAsync(IEnumerable<LogIngestionMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            await PushLogAsync(msg, cancellationToken);
        }
    }

    public async Task PushTraceAsync(TraceIngestionMessage message, CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(message);
                var entry = new NameValueEntry[] { new("data", json), new("traceId", message.TraceId) };
                await db.StreamAddAsync(TracesStreamKey, entry, maxLength: MaxStreamLength, useApproximateMaxLength: true);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis stream write failed for trace.");
                if (_requireDurableStorage)
                {
                    throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering failed; Redis stream write error in production fail-closed mode.", ex);
                }
            }
        }
        else if (_requireDurableStorage)
        {
            _logger.LogError("Durable storage is required in production fail-closed mode, but Redis is disconnected. Rejecting telemetry trace.");
            throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering is unavailable; Redis is not connected.");
        }

        await _fallbackTraceChannel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task PushTracesAsync(IEnumerable<TraceIngestionMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            await PushTraceAsync(msg, cancellationToken);
        }
    }

    public async Task PushMetricAsync(MetricIngestionMessage message, CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(message);
                var entry = new NameValueEntry[] { new("data", json), new("metric", message.MetricName) };
                await db.StreamAddAsync(MetricsStreamKey, entry, maxLength: MaxStreamLength, useApproximateMaxLength: true);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis stream write failed for metric.");
                if (_requireDurableStorage)
                {
                    throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering failed; Redis stream write error in production fail-closed mode.", ex);
                }
            }
        }
        else if (_requireDurableStorage)
        {
            _logger.LogError("Durable storage is required in production fail-closed mode, but Redis is disconnected. Rejecting telemetry metric.");
            throw new SentinelOps.Application.Common.Exceptions.DurableBufferUnavailableException("Durable telemetry buffering is unavailable; Redis is not connected.");
        }

        await _fallbackMetricChannel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task PushMetricsAsync(IEnumerable<MetricIngestionMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            await PushMetricAsync(msg, cancellationToken);
        }
    }

    private async Task<List<StreamEntry>> ReadEntriesWithPelRecoveryAsync(
        IDatabase db,
        string streamKey,
        string consumerName,
        int count = 100,
        long minIdleTimeMs = 30_000)
    {
        var entries = new List<StreamEntry>();
        try
        {
            var pendingInfos = await db.StreamPendingMessagesAsync(
                streamKey,
                ConsumerGroup,
                count,
                consumerName: RedisValue.Null);

            if (pendingInfos != null && pendingInfos.Length > 0)
            {
                var staleIds = pendingInfos
                    .Where(p => p.IdleTimeInMilliseconds >= minIdleTimeMs)
                    .Select(p => p.MessageId)
                    .ToArray();

                if (staleIds.Length > 0)
                {
                    var claimedEntries = await db.StreamClaimAsync(
                        streamKey,
                        ConsumerGroup,
                        consumerName,
                        minIdleTimeMs,
                        staleIds);

                    if (claimedEntries != null && claimedEntries.Length > 0)
                    {
                        entries.AddRange(claimedEntries);
                        _logger.LogInformation("Claimed {Count} unacknowledged entries from PEL for {StreamKey}", claimedEntries.Length, streamKey);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "PEL auto-claim check skipped for stream {StreamKey}", streamKey);
        }

        if (entries.Count == 0)
        {
            try
            {
                var newEntries = await db.StreamReadGroupAsync(
                    streamKey,
                    ConsumerGroup,
                    consumerName,
                    StreamPosition.NewMessages,
                    count: count);

                if (newEntries != null && newEntries.Length > 0)
                {
                    entries.AddRange(newEntries);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading from Redis stream {StreamKey}", streamKey);
                throw;
            }
        }

        return entries;
    }

    public async IAsyncEnumerable<LogIngestionMessage> ReadLogsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            IDatabase? db = null;
            try
            {
                db = _redis.GetDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Redis database for logs stream; falling back to in-memory channel.");
            }

            if (db != null)
            {
                var consumerName = $"consumer-{Guid.NewGuid():N}"[..16];

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (_fallbackLogChannel.Reader.TryRead(out var fallbackMsg))
                    {
                        yield return fallbackMsg;
                    }

                    List<StreamEntry> entries;
                    try
                    {
                        entries = await ReadEntriesWithPelRecoveryAsync(db, LogsStreamKey, consumerName, count: 100);
                    }
                    catch
                    {
                        break;
                    }

                    if (entries.Count > 0)
                    {
                        foreach (var entry in entries)
                        {
                            var json = entry["data"].ToString();
                            LogIngestionMessage? msg = null;
                            try
                            {
                                msg = JsonSerializer.Deserialize<LogIngestionMessage>(json);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogError(parseEx, "Malformed log payload sent to dead letter stream");
                                await db.StreamAddAsync(DeadLetterStreamKey, new NameValueEntry[] { new("type", "log"), new("data", json) });
                                await db.StreamAcknowledgeAsync(LogsStreamKey, ConsumerGroup, entry.Id);
                                continue;
                            }

                            if (msg != null)
                            {
                                await db.StreamAcknowledgeAsync(LogsStreamKey, ConsumerGroup, entry.Id);
                                yield return msg;
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(200, cancellationToken);
                    }
                }
            }
        }

        // Read from in-memory fallback channel
        while (await _fallbackLogChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_fallbackLogChannel.Reader.TryRead(out var log))
            {
                yield return log;
            }
        }
    }

    public async IAsyncEnumerable<TraceIngestionMessage> ReadTracesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            IDatabase? db = null;
            try
            {
                db = _redis.GetDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Redis database for traces stream; falling back to in-memory channel.");
            }

            if (db != null)
            {
                var consumerName = $"consumer-{Guid.NewGuid():N}"[..16];

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (_fallbackTraceChannel.Reader.TryRead(out var fallbackMsg))
                    {
                        yield return fallbackMsg;
                    }

                    List<StreamEntry> entries;
                    try
                    {
                        entries = await ReadEntriesWithPelRecoveryAsync(db, TracesStreamKey, consumerName, count: 100);
                    }
                    catch
                    {
                        break;
                    }

                    if (entries.Count > 0)
                    {
                        foreach (var entry in entries)
                        {
                            var json = entry["data"].ToString();
                            TraceIngestionMessage? msg = null;
                            try
                            {
                                msg = JsonSerializer.Deserialize<TraceIngestionMessage>(json);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogError(parseEx, "Malformed trace payload sent to dead letter stream");
                                await db.StreamAddAsync(DeadLetterStreamKey, new NameValueEntry[] { new("type", "trace"), new("data", json) });
                                await db.StreamAcknowledgeAsync(TracesStreamKey, ConsumerGroup, entry.Id);
                                continue;
                            }

                            if (msg != null)
                            {
                                await db.StreamAcknowledgeAsync(TracesStreamKey, ConsumerGroup, entry.Id);
                                yield return msg;
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(200, cancellationToken);
                    }
                }
            }
        }

        while (await _fallbackTraceChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_fallbackTraceChannel.Reader.TryRead(out var trace))
            {
                yield return trace;
            }
        }
    }

    public async IAsyncEnumerable<MetricIngestionMessage> ReadMetricsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            IDatabase? db = null;
            try
            {
                db = _redis.GetDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Redis database for metrics stream; falling back to in-memory channel.");
            }

            if (db != null)
            {
                var consumerName = $"consumer-{Guid.NewGuid():N}"[..16];

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (_fallbackMetricChannel.Reader.TryRead(out var fallbackMsg))
                    {
                        yield return fallbackMsg;
                    }

                    List<StreamEntry> entries;
                    try
                    {
                        entries = await ReadEntriesWithPelRecoveryAsync(db, MetricsStreamKey, consumerName, count: 100);
                    }
                    catch
                    {
                        break;
                    }

                    if (entries.Count > 0)
                    {
                        foreach (var entry in entries)
                        {
                            var json = entry["data"].ToString();
                            MetricIngestionMessage? msg = null;
                            try
                            {
                                msg = JsonSerializer.Deserialize<MetricIngestionMessage>(json);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogError(parseEx, "Malformed metric payload sent to dead letter stream");
                                await db.StreamAddAsync(DeadLetterStreamKey, new NameValueEntry[] { new("type", "metric"), new("data", json) });
                                await db.StreamAcknowledgeAsync(MetricsStreamKey, ConsumerGroup, entry.Id);
                                continue;
                            }

                            if (msg != null)
                            {
                                await db.StreamAcknowledgeAsync(MetricsStreamKey, ConsumerGroup, entry.Id);
                                yield return msg;
                            }
                        }
                    }
                    else
                    {
                        await Task.Delay(200, cancellationToken);
                    }
                }
            }
        }

        while (await _fallbackMetricChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_fallbackMetricChannel.Reader.TryRead(out var metric))
            {
                yield return metric;
            }
        }
    }
}
