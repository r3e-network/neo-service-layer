using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Infrastructure.Caching
{
    /// <summary>
    /// In-memory cache service implementation using Microsoft.Extensions.Caching.Memory.
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheServiceOptions _options;
        private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps;
        private readonly object _statisticsLock = new();

        // Statistics tracking
        private long _hitCount;
        private long _missCount;
        private long _evictionCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
        /// </summary>
        /// <param name="cache">The memory cache instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">Cache service options.</param>
        public MemoryCacheService(
            IMemoryCache cache,
            ILogger<MemoryCacheService> logger,
            IOptions<MemoryCacheServiceOptions> options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new MemoryCacheServiceOptions();
            _keyTimestamps = new ConcurrentDictionary<string, DateTime>();

            _logger.LogInformation("MemoryCacheService initialized with options: {@Options}", _options);
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var fullKey = GetFullKey<T>(key);

                if (_cache.TryGetValue(fullKey, out var value) && value is T typedValue)
                {
                    Interlocked.Increment(ref _hitCount);
                    _logger.LogTrace("Cache hit for key: {Key}", fullKey);
                    return typedValue;
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogTrace("Cache miss for key: {Key}", fullKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                Interlocked.Increment(ref _missCount);
                return null;
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var fullKey = GetFullKey<T>(key);
                var expirationTime = expiration ?? _options.DefaultExpiration;

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime,
                    Priority = CacheItemPriority.Normal,
                    Size = EstimateSize(value)
                };

                // Add eviction callback for statistics
                cacheEntryOptions.RegisterPostEvictionCallback(OnEviction);

                _cache.Set(fullKey, value, cacheEntryOptions);
                _keyTimestamps[fullKey] = DateTime.UtcNow;

                _logger.LogTrace("Cached value for key: {Key} with expiration: {Expiration}", fullKey, expirationTime);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
                return false;
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var fullKey = GetFullKey<object>(key);
                _cache.Remove(fullKey);
                _keyTimestamps.TryRemove(fullKey, out _);

                _logger.LogTrace("Removed cache entry for key: {Key}", fullKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
                return false;
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var fullKey = GetFullKey<object>(key);
                var exists = _cache.TryGetValue(fullKey, out _);

                _logger.LogTrace("Cache existence check for key: {Key} = {Exists}", fullKey, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
                return false;
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ClearAsync()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    // Use reflection to clear the cache (MemoryCache doesn't have a clear method)
                    var field = typeof(MemoryCache).GetField("_coherentState",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (field?.GetValue(memoryCache) is object coherentState)
                    {
                        var entriesCollection = coherentState.GetType()
                            .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (entriesCollection?.GetValue(coherentState) is System.Collections.IDictionary entries)
                        {
                            entries.Clear();
                        }
                    }
                }

                _keyTimestamps.Clear();
                Interlocked.Exchange(ref _hitCount, 0);
                Interlocked.Exchange(ref _missCount, 0);
                Interlocked.Exchange(ref _evictionCount, 0);

                _logger.LogInformation("Memory cache cleared successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing memory cache");
                return false;
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
        {
            var result = new Dictionary<string, T?>();

            if (keys == null)
                return result;

            try
            {
                foreach (var key in keys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        var value = await GetAsync<T>(key).ConfigureAwait(false);
                        result[key] = value;
                    }
                }

                _logger.LogTrace("Retrieved {Count} cache entries", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple cache values");
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null) where T : class
        {
            if (items == null || !items.Any())
                return true;

            try
            {
                var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                var success = results.All(r => r);
                _logger.LogTrace("Set {Count} cache entries, success: {Success}", items.Count, success);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple cache values");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> RemoveManyAsync(IEnumerable<string> keys)
        {
            if (keys == null)
                return 0;

            try
            {
                var tasks = keys.Select(RemoveAsync);
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                var removedCount = results.Count(r => r);
                _logger.LogTrace("Removed {Count} cache entries", removedCount);

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing multiple cache values");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                var totalEntries = _keyTimestamps.Count;
                var hitCount = Interlocked.Read(ref _hitCount);
                var missCount = Interlocked.Read(ref _missCount);
                var evictionCount = Interlocked.Read(ref _evictionCount);

                var statistics = new CacheStatistics
                {
                    TotalEntries = totalEntries,
                    HitCount = hitCount,
                    MissCount = missCount,
                    EvictionCount = evictionCount,
                    MemoryUsage = EstimateMemoryUsage(),
                    IsHealthy = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["CacheType"] = "Memory",
                        ["DefaultExpiration"] = _options.DefaultExpiration,
                        ["MaxSize"] = _options.MaxSize,
                        ["CompactionPercentage"] = _options.CompactionPercentage
                    }
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new CacheStatistics { IsHealthy = false };
            }
            finally
            {
                await Task.CompletedTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the full cache key with type prefix.
        /// </summary>
        private string GetFullKey<T>(string key)
        {
            var typePrefix = typeof(T).Name;
            return $"{_options.KeyPrefix}:{typePrefix}:{key}";
        }

        /// <summary>
        /// Estimates the size of a cached object.
        /// </summary>
        private long EstimateSize(object value)
        {
            // Simple size estimation - could be enhanced with more sophisticated logic
            return value switch
            {
                string str => str.Length * 2, // Unicode characters are 2 bytes
                byte[] bytes => bytes.Length,
                _ => _options.DefaultItemSize
            };
        }

        /// <summary>
        /// Estimates total memory usage of the cache.
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // This is a rough estimation - actual memory usage may vary
            return _keyTimestamps.Count * _options.DefaultItemSize;
        }

        /// <summary>
        /// Callback for cache entry eviction.
        /// </summary>
        private void OnEviction(object key, object? value, EvictionReason reason, object? state)
        {
            Interlocked.Increment(ref _evictionCount);

            if (key is string stringKey)
            {
                _keyTimestamps.TryRemove(stringKey, out _);
            }

            _logger.LogTrace("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cache?.Dispose();
            _keyTimestamps?.Clear();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Configuration options for MemoryCacheService.
    /// </summary>
    public class MemoryCacheServiceOptions
    {
        /// <summary>
        /// Gets or sets the default expiration time for cache entries.
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the key prefix for all cache entries.
        /// </summary>
        public string KeyPrefix { get; set; } = "NSL";

        /// <summary>
        /// Gets or sets the maximum cache size in bytes.
        /// </summary>
        public long MaxSize { get; set; } = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Gets or sets the default item size estimation in bytes.
        /// </summary>
        public long DefaultItemSize { get; set; } = 1024; // 1 KB

        /// <summary>
        /// Gets or sets the compaction percentage when cache is full.
        /// </summary>
        public double CompactionPercentage { get; set; } = 0.25; // Remove 25% of entries
    }
}
