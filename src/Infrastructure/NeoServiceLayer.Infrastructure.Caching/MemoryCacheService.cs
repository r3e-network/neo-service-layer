using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.Caching
{
    /// <summary>
    /// In-memory cache service implementation using Microsoft.Extensions.Caching.Memory.
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        // LoggerMessage delegates for performance optimization
        private static readonly Action<ILogger, MemoryCacheServiceOptions, Exception?> _serviceInitialized =
            LoggerMessage.Define<MemoryCacheServiceOptions>(LogLevel.Information, new EventId(3001, "ServiceInitialized"),
                "MemoryCacheService initialized with options: {@Options}");

        private static readonly Action<ILogger, string, Exception?> _cacheHit =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3002, "CacheHit"),
                "Cache hit for key: {Key}");

        private static readonly Action<ILogger, string, Exception?> _cacheMiss =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3003, "CacheMiss"),
                "Cache miss for key: {Key}");

        private static readonly Action<ILogger, string, Exception?> _getCacheError =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3004, "GetCacheError"),
                "Error getting cache value for key: {Key}");

        private static readonly Action<ILogger, string, TimeSpan, Exception?> _valuesCached =
            LoggerMessage.Define<string, TimeSpan>(LogLevel.Trace, new EventId(3005, "ValueCached"),
                "Cached value for key: {Key} with expiration: {Expiration}");

        private static readonly Action<ILogger, string, Exception?> _setCacheError =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3006, "SetCacheError"),
                "Error setting cache value for key: {Key}");

        private static readonly Action<ILogger, string, Exception?> _cacheEntryRemoved =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3007, "CacheEntryRemoved"),
                "Removed cache entry for key: {Key}");

        private static readonly Action<ILogger, string, Exception?> _removeCacheError =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3008, "RemoveCacheError"),
                "Error removing cache value for key: {Key}");

        private static readonly Action<ILogger, string, bool, Exception?> _cacheExistenceCheck =
            LoggerMessage.Define<string, bool>(LogLevel.Trace, new EventId(3009, "CacheExistenceCheck"),
                "Cache existence check for key: {Key} = {Exists}");

        private static readonly Action<ILogger, string, Exception?> _existenceCheckError =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3010, "ExistenceCheckError"),
                "Error checking cache key existence: {Key}");

        private static readonly Action<ILogger, Exception?> _cacheCleared =
            LoggerMessage.Define(LogLevel.Information, new EventId(3011, "CacheCleared"),
                "Memory cache cleared successfully");

        private static readonly Action<ILogger, Exception?> _clearCacheError =
            LoggerMessage.Define(LogLevel.Error, new EventId(3012, "ClearCacheError"),
                "Error clearing memory cache");

        private static readonly Action<ILogger, int, Exception?> _multipleEntriesRetrieved =
            LoggerMessage.Define<int>(LogLevel.Trace, new EventId(3013, "MultipleEntriesRetrieved"),
                "Retrieved {Count} cache entries");

        private static readonly Action<ILogger, Exception?> _getMultipleError =
            LoggerMessage.Define(LogLevel.Error, new EventId(3014, "GetMultipleError"),
                "Error getting multiple cache values");

        private static readonly Action<ILogger, int, bool, Exception?> _multipleEntriesSet =
            LoggerMessage.Define<int, bool>(LogLevel.Trace, new EventId(3015, "MultipleEntriesSet"),
                "Set {Count} cache entries, success: {Success}");

        private static readonly Action<ILogger, Exception?> _setMultipleError =
            LoggerMessage.Define(LogLevel.Error, new EventId(3016, "SetMultipleError"),
                "Error setting multiple cache values");

        private static readonly Action<ILogger, int, Exception?> _multipleEntriesRemoved =
            LoggerMessage.Define<int>(LogLevel.Trace, new EventId(3017, "MultipleEntriesRemoved"),
                "Removed {Count} cache entries");

        private static readonly Action<ILogger, Exception?> _removeMultipleError =
            LoggerMessage.Define(LogLevel.Error, new EventId(3018, "RemoveMultipleError"),
                "Error removing multiple cache values");

        private static readonly Action<ILogger, Exception?> _statisticsError =
            LoggerMessage.Define(LogLevel.Error, new EventId(3019, "StatisticsError"),
                "Error getting cache statistics");

        private static readonly Action<ILogger, object, EvictionReason, Exception?> _entryEvicted =
            LoggerMessage.Define<object, EvictionReason>(LogLevel.Trace, new EventId(3020, "EntryEvicted"),
                "Cache entry evicted: {Key}, Reason: {Reason}");
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

            _serviceInitialized(_logger, _options, null);
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
                    _cacheHit(_logger, fullKey, null);
                    return typedValue;
                }

                Interlocked.Increment(ref _missCount);
                _cacheMiss(_logger, fullKey, null);
                return null;
            }
            catch (Exception ex)
            {
                _getCacheError(_logger, key, ex);
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

                _valuesCached(_logger, fullKey, expirationTime, null);
                return true;
            }
            catch (Exception ex)
            {
                _setCacheError(_logger, key, ex);
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

                _cacheEntryRemoved(_logger, fullKey, null);
                return true;
            }
            catch (Exception ex)
            {
                _removeCacheError(_logger, key, ex);
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

                _cacheExistenceCheck(_logger, fullKey, exists, null);
                return exists;
            }
            catch (Exception ex)
            {
                _existenceCheckError(_logger, key, ex);
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

                _cacheCleared(_logger, null);
                return true;
            }
            catch (Exception ex)
            {
                _clearCacheError(_logger, ex);
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

                _multipleEntriesRetrieved(_logger, result.Count, null);
                return result;
            }
            catch (Exception ex)
            {
                _getMultipleError(_logger, ex);
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
                _multipleEntriesSet(_logger, items.Count, success, null);

                return success;
            }
            catch (Exception ex)
            {
                _setMultipleError(_logger, ex);
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
                _multipleEntriesRemoved(_logger, removedCount, null);

                return removedCount;
            }
            catch (Exception ex)
            {
                _removeMultipleError(_logger, ex);
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            try
            {
                // First try to get the value from cache
                var cached = await GetAsync<T>(key).ConfigureAwait(false);
                if (cached != null)
                {
                    return cached;
                }

                // Value not in cache, use factory to create it
                var value = await factory().ConfigureAwait(false);
                if (value != null)
                {
                    await SetAsync(key, value, expiration).ConfigureAwait(false);
                }

                return value;
            }
            catch (Exception ex)
            {
                _getCacheError(_logger, key, ex);
                // If cache operation fails, still try to get the value from factory
                try
                {
                    return await factory().ConfigureAwait(false);
                }
                catch
                {
                    return default;
                }
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
                _statisticsError(_logger, ex);
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

            _entryEvicted(_logger, key, reason, null);
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
