using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Cache helper for the Proof of Reserve Service with memory and distributed caching support.
/// </summary>
public class ProofOfReserveCacheHelper : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ProofOfReserveCacheHelper> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _cacheLocks = new();
    private readonly Timer _cleanupTimer;
    private readonly object _statsLock = new();
    private CacheStatistics _statistics = new();

    /// <summary>
    /// Cache key prefixes for different data types.
    /// </summary>
    public static class CacheKeys
    {
        public const string ReserveSnapshot = "por:snapshot";
        public const string ReserveStatus = "por:status";
        public const string ProofData = "por:proof";
        public const string AssetInfo = "por:asset";
        public const string HealthStatus = "por:health";
        public const string AuditReport = "por:audit";
        public const string Alerts = "por:alerts";
        public const string BlockchainBalance = "por:balance";
        public const string ConfigSummary = "por:config";
    }

    /// <summary>
    /// Default cache expiration times for different data types.
    /// </summary>
    public static class CacheExpirations
    {
        public static readonly TimeSpan ReserveSnapshot = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ReserveStatus = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ProofData = TimeSpan.FromHours(1);
        public static readonly TimeSpan AssetInfo = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan HealthStatus = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan AuditReport = TimeSpan.FromHours(6);
        public static readonly TimeSpan Alerts = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan BlockchainBalance = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan ConfigSummary = TimeSpan.FromMinutes(60);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveCacheHelper"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public ProofOfReserveCacheHelper(
        IMemoryCache memoryCache,
        ILogger<ProofOfReserveCacheHelper> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;

        // Setup cleanup timer to run every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        _logger.LogDebug("Proof of Reserve cache helper initialized");
    }

    /// <summary>
    /// Gets cached data or executes factory method if not cached.
    /// </summary>
    /// <typeparam name="T">The type of data to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory method to create data if not cached.</param>
    /// <param name="expiration">The cache expiration time.</param>
    /// <param name="enableCaching">Whether caching is enabled.</param>
    /// <returns>The cached or newly created data.</returns>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        bool enableCaching = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (!enableCaching)
        {
            _logger.LogDebug("Caching disabled, executing factory for key: {Key}", key);
            return await factory();
        }

        // Check if data is already cached
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            RecordCacheHit(key);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        // Use semaphore to prevent multiple simultaneous executions for the same key
        var semaphore = _cacheLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_memoryCache.TryGetValue(key, out cachedValue) && cachedValue != null)
            {
                RecordCacheHit(key);
                _logger.LogDebug("Cache hit after lock for key: {Key}", key);
                return cachedValue;
            }

            // Execute factory method
            _logger.LogDebug("Cache miss, executing factory for key: {Key}", key);
            var startTime = DateTime.UtcNow;
            
            try
            {
                var value = await factory();
                var duration = DateTime.UtcNow - startTime;

                // Cache the result with expiration
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration,
                    SlidingExpiration = expiration < TimeSpan.FromMinutes(30) ? expiration / 2 : null,
                    Size = EstimateSize(value),
                    Priority = DetermineCachePriority(key)
                };

                cacheEntryOptions.RegisterPostEvictionCallback(OnCacheEntryEvicted);

                _memoryCache.Set(key, value, cacheEntryOptions);

                RecordCacheMiss(key, duration);
                _logger.LogDebug("Cached value for key: {Key}, expiration: {Expiration}, size: {Size}",
                    key, expiration, cacheEntryOptions.Size);

                return value;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                RecordCacheError(key, duration, ex);
                throw;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets cached data synchronously.
    /// </summary>
    /// <typeparam name="T">The type of data to get.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached data or default value.</returns>
    public T? Get<T>(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_memoryCache.TryGetValue(key, out T? value))
        {
            RecordCacheHit(key);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }

        RecordCacheMiss(key, TimeSpan.Zero);
        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of data to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The cache expiration time.</param>
    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            SlidingExpiration = expiration < TimeSpan.FromMinutes(30) ? expiration / 2 : null,
            Size = EstimateSize(value),
            Priority = DetermineCachePriority(key)
        };

        cacheEntryOptions.RegisterPostEvictionCallback(OnCacheEntryEvicted);

        _memoryCache.Set(key, value, cacheEntryOptions);

        _logger.LogDebug("Set cache value for key: {Key}, expiration: {Expiration}, size: {Size}",
            key, expiration, cacheEntryOptions.Size);
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _memoryCache.Remove(key);
        _logger.LogDebug("Removed cache entry for key: {Key}", key);
    }

    /// <summary>
    /// Removes all cache entries matching a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports wildcards).</param>
    public void RemoveByPattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        // Note: IMemoryCache doesn't provide a way to enumerate keys
        // In a production environment, you might want to use IDistributedCache
        // or maintain a separate index of cache keys
        
        _logger.LogWarning("RemoveByPattern not fully implemented for MemoryCache: {Pattern}", pattern);
    }

    /// <summary>
    /// Invalidates cache entries for a specific asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    public void InvalidateAssetCache(string assetId)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetId);

        var keysToRemove = new[]
        {
            BuildCacheKey(CacheKeys.ReserveSnapshot, assetId),
            BuildCacheKey(CacheKeys.ReserveStatus, assetId),
            BuildCacheKey(CacheKeys.AssetInfo, assetId),
            BuildCacheKey(CacheKeys.HealthStatus, assetId),
            BuildCacheKey(CacheKeys.Alerts, assetId)
        };

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogInformation("Invalidated cache for asset: {AssetId}", assetId);
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>The cache statistics.</returns>
    public CacheStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            return new CacheStatistics
            {
                TotalRequests = _statistics.TotalRequests,
                CacheHits = _statistics.CacheHits,
                CacheMisses = _statistics.CacheMisses,
                CacheErrors = _statistics.CacheErrors,
                HitRatio = _statistics.TotalRequests > 0 ? (double)_statistics.CacheHits / _statistics.TotalRequests : 0,
                AverageExecutionTime = _statistics.TotalExecutionTime.TotalMilliseconds / Math.Max(_statistics.CacheMisses, 1),
                LastResetTime = _statistics.LastResetTime
            };
        }
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _statistics = new CacheStatistics
            {
                LastResetTime = DateTime.UtcNow
            };
        }

        _logger.LogInformation("Cache statistics reset");
    }

    /// <summary>
    /// Builds a cache key with prefix and parameters.
    /// </summary>
    /// <param name="prefix">The cache key prefix.</param>
    /// <param name="parameters">The parameters to include in the key.</param>
    /// <returns>The built cache key.</returns>
    public static string BuildCacheKey(string prefix, params object[] parameters)
    {
        var keyBuilder = new StringBuilder(prefix);
        
        foreach (var param in parameters)
        {
            keyBuilder.Append(':');
            keyBuilder.Append(param?.ToString() ?? "null");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Builds a cache key with hash for complex objects.
    /// </summary>
    /// <param name="prefix">The cache key prefix.</param>
    /// <param name="obj">The object to hash.</param>
    /// <returns>The built cache key with hash.</returns>
    public static string BuildHashedCacheKey(string prefix, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var hash = ComputeHash(json);
        return $"{prefix}:{hash}";
    }

    /// <summary>
    /// Estimates the size of an object for cache sizing.
    /// </summary>
    /// <param name="obj">The object to estimate.</param>
    /// <returns>The estimated size in bytes.</returns>
    private static long EstimateSize(object? obj)
    {
        if (obj == null) return 0;

        try
        {
            var json = JsonSerializer.Serialize(obj);
            return Encoding.UTF8.GetByteCount(json);
        }
        catch
        {
            // Fallback estimation
            return obj.ToString()?.Length ?? 0;
        }
    }

    /// <summary>
    /// Determines cache priority based on key type.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cache priority.</returns>
    private static CacheItemPriority DetermineCachePriority(string key)
    {
        if (key.Contains(CacheKeys.HealthStatus) || key.Contains(CacheKeys.Alerts))
            return CacheItemPriority.High;
        
        if (key.Contains(CacheKeys.ReserveStatus) || key.Contains(CacheKeys.AssetInfo))
            return CacheItemPriority.Normal;
        
        if (key.Contains(CacheKeys.AuditReport) || key.Contains(CacheKeys.ConfigSummary))
            return CacheItemPriority.Low;

        return CacheItemPriority.Normal;
    }

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The hash as hex string.</returns>
    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Records a cache hit in statistics.
    /// </summary>
    /// <param name="key">The cache key.</param>
    private void RecordCacheHit(string key)
    {
        lock (_statsLock)
        {
            _statistics.TotalRequests++;
            _statistics.CacheHits++;
        }
    }

    /// <summary>
    /// Records a cache miss in statistics.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="executionTime">The execution time.</param>
    private void RecordCacheMiss(string key, TimeSpan executionTime)
    {
        lock (_statsLock)
        {
            _statistics.TotalRequests++;
            _statistics.CacheMisses++;
            _statistics.TotalExecutionTime += executionTime;
        }
    }

    /// <summary>
    /// Records a cache error in statistics.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="executionTime">The execution time.</param>
    /// <param name="exception">The exception that occurred.</param>
    private void RecordCacheError(string key, TimeSpan executionTime, Exception exception)
    {
        lock (_statsLock)
        {
            _statistics.TotalRequests++;
            _statistics.CacheErrors++;
            _statistics.TotalExecutionTime += executionTime;
        }

        _logger.LogError(exception, "Cache error for key: {Key}", key);
    }

    /// <summary>
    /// Handles cache entry eviction callback.
    /// </summary>
    /// <param name="key">The evicted key.</param>
    /// <param name="value">The evicted value.</param>
    /// <param name="reason">The eviction reason.</param>
    /// <param name="state">The callback state.</param>
    private void OnCacheEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        _logger.LogDebug("Cache entry evicted: {Key}, reason: {Reason}", key, reason);
    }

    /// <summary>
    /// Cleans up expired cache entries and locks.
    /// </summary>
    /// <param name="state">Timer state.</param>
    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            // Remove unused semaphores
            var keysToRemove = new List<string>();
            foreach (var kvp in _cacheLocks)
            {
                if (kvp.Value.CurrentCount == 1) // Not in use
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                if (_cacheLocks.TryRemove(key, out var semaphore))
                {
                    semaphore.Dispose();
                }
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} unused cache locks", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    /// <summary>
    /// Disposes the cache helper.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        foreach (var semaphore in _cacheLocks.Values)
        {
            semaphore.Dispose();
        }
        _cacheLocks.Clear();

        _logger.LogDebug("Proof of Reserve cache helper disposed");
    }
}

/// <summary>
/// Cache statistics for monitoring and diagnostics.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cache requests.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Gets or sets the number of cache errors.
    /// </summary>
    public long CacheErrors { get; set; }

    /// <summary>
    /// Gets or sets the cache hit ratio.
    /// </summary>
    public double HitRatio { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets when statistics were last reset.
    /// </summary>
    public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
}