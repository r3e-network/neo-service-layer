using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace NeoServiceLayer.Infrastructure.Caching;

/// <summary>
/// Extensions for Redis distributed cache operations.
/// </summary>
public static class RedisDistributedCacheExtensions
{
    /// <summary>
    /// Remove cache entries by pattern using Redis SCAN.
    /// </summary>
    public static async Task RemoveByPatternAsync(
        this IDistributedCache cache,
        string pattern,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        if (cache is not IRedisDistributedCache redisCache)
        {
            logger?.LogWarning("Pattern-based removal is only supported for Redis distributed cache");
            return;
        }

        try
        {
            await redisCache.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
            logger?.LogDebug("Removed cache entries matching pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to remove cache entries by pattern: {Pattern}", pattern);
            throw;
        }
    }

    /// <summary>
    /// Get cache size information.
    /// </summary>
    public static async Task<RedisCacheInfo> GetCacheInfoAsync(
        this IDistributedCache cache,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        if (cache is not IRedisDistributedCache redisCache)
        {
            logger?.LogWarning("Cache info is only available for Redis distributed cache");
            return new RedisCacheInfo();
        }

        try
        {
            return await redisCache.GetCacheInfoAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to get Redis cache info");
            return new RedisCacheInfo { IsHealthy = false };
        }
    }
}

/// <summary>
/// Enhanced Redis distributed cache interface with pattern operations.
/// </summary>
public interface IRedisDistributedCache : IDistributedCache
{
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<RedisCacheInfo> GetCacheInfoAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<long> DecrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enhanced Redis distributed cache implementation.
/// </summary>
public class EnhancedRedisDistributedCache : IRedisDistributedCache
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<EnhancedRedisDistributedCache> _logger;
    private readonly string _keyPrefix;

    public EnhancedRedisDistributedCache(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<EnhancedRedisDistributedCache> logger,
        string keyPrefix = "NSL:")
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix;
        _database = _connectionMultiplexer.GetDatabase();
    }

    public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            return await _database.StringGetAsync(redisKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key: {Key}", redisKey);
            return null;
        }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            var expiry = GetExpiry(options);
            await _database.StringSetAsync(redisKey, value, expiry).ConfigureAwait(false);
            
            _logger.LogDebug("Redis SET completed for key: {Key}, expiry: {Expiry}", redisKey, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key: {Key}", redisKey);
            throw;
        }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            await _database.KeyTouchAsync(redisKey).ConfigureAwait(false);
            _logger.LogDebug("Redis REFRESH completed for key: {Key}", redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REFRESH failed for key: {Key}", redisKey);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            await _database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
            _logger.LogDebug("Redis DELETE completed for key: {Key}", redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DELETE failed for key: {Key}", redisKey);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var redisPattern = GetRedisKey(pattern);
            
            var keys = server.KeysAsync(pattern: redisPattern);
            var keysToDelete = new List<RedisKey>();
            
            await foreach (var key in keys)
            {
                keysToDelete.Add(key);
                
                // Delete in batches to avoid memory issues
                if (keysToDelete.Count >= 1000)
                {
                    await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                    keysToDelete.Clear();
                }
            }
            
            // Delete remaining keys
            if (keysToDelete.Count > 0)
            {
                await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
            }
            
            _logger.LogInformation("Removed Redis keys matching pattern: {Pattern}", redisPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Redis keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<RedisCacheInfo> GetCacheInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var info = await server.InfoAsync("memory").ConfigureAwait(false);
            
            var usedMemory = GetInfoValue(info, "used_memory");
            var maxMemory = GetInfoValue(info, "maxmemory");
            var keyCount = await _database.ExecuteAsync("DBSIZE").ConfigureAwait(false);
            
            return new RedisCacheInfo
            {
                IsHealthy = _connectionMultiplexer.IsConnected,
                UsedMemoryBytes = usedMemory,
                MaxMemoryBytes = maxMemory,
                KeyCount = keyCount,
                ConnectedClients = GetInfoValue(info, "connected_clients"),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis cache info");
            return new RedisCacheInfo { IsHealthy = false };
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            return await _database.KeyExistsAsync(redisKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis EXISTS failed for key: {Key}", redisKey);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            var result = await _database.StringIncrementAsync(redisKey, value).ConfigureAwait(false);
            
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(redisKey, expiry.Value).ConfigureAwait(false);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis INCREMENT failed for key: {Key}", redisKey);
            throw;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var redisKey = GetRedisKey(key);
        try
        {
            var result = await _database.StringDecrementAsync(redisKey, value).ConfigureAwait(false);
            
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(redisKey, expiry.Value).ConfigureAwait(false);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DECREMENT failed for key: {Key}", redisKey);
            throw;
        }
    }

    private string GetRedisKey(string key) => $"{_keyPrefix}{key}";

    private static TimeSpan? GetExpiry(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow.Value;
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
        }

        return null;
    }

    private static long GetInfoValue(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        var value = info.SelectMany(g => g)
            .FirstOrDefault(kvp => kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            .Value;
            
        return long.TryParse(value, out var result) ? result : 0;
    }
}

/// <summary>
/// Redis cache information model.
/// </summary>
public class RedisCacheInfo
{
    public bool IsHealthy { get; set; }
    public long UsedMemoryBytes { get; set; }
    public long MaxMemoryBytes { get; set; }
    public long KeyCount { get; set; }
    public long ConnectedClients { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public double MemoryUsagePercentage => MaxMemoryBytes > 0 ? (double)UsedMemoryBytes / MaxMemoryBytes * 100 : 0;
    public string UsedMemoryFormatted => FormatBytes(UsedMemoryBytes);
    public string MaxMemoryFormatted => FormatBytes(MaxMemoryBytes);

    private static string FormatBytes(long bytes)
    {
        const int unit = 1024;
        if (bytes < unit) return $"{bytes} B";
        
        var exp = (int)(Math.Log(bytes) / Math.Log(unit));
        var pre = "KMGTPE"[exp - 1];
        return $"{bytes / Math.Pow(unit, exp):F1} {pre}B";
    }
}