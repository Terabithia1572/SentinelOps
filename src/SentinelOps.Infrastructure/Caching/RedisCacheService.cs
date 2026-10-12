using System.Text.Json;
using SentinelOps.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SentinelOps.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected) return default;
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected) return;
        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await _database.StringSetAsync(key, serialized, expiry);
        }
        catch
        {
            // Silently degrade if cache server is unavailable
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected) return;
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch
        {
            // Silently degrade if cache server is unavailable
        }
    }
}
