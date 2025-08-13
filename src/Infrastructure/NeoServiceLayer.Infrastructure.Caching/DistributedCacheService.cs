using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Infrastructure.Caching
{
    /// <summary>
    /// Distributed cache service implementation that wraps IDistributedCache.
    /// </summary>
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly DistributedCacheServiceOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        // Statistics tracking (approximate for distributed cache)
        private long _hitCount;
        private long _missCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedCacheService"/> class.
        /// </summary>
        /// <param name="distributedCache">The distributed cache instance.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">Cache service options.</param>
        public DistributedCacheService(
            IDistributedCache distributedCache,
            ILogger<DistributedCacheService> logger,
            IOptions<DistributedCacheServiceOptions>? options = null)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new DistributedCacheServiceOptions();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            _logger.LogInformation("DistributedCacheService initialized with options: {@Options}", _options);
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var fullKey = GetFullKey<T>(key);
                var cachedBytes = await _distributedCache.GetAsync(fullKey).ConfigureAwait(false);

                if (cachedBytes?.Length > 0)
                {
                    var json = System.Text.Encoding.UTF8.GetString(cachedBytes);
                    var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                    if (value != null)
                    {
                        Interlocked.Increment(ref _hitCount);
                        _logger.LogTrace("Distributed cache hit for key: {Key}", fullKey);
                        return value;
                    }
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogTrace("Distributed cache miss for key: {Key}", fullKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting distributed cache value for key: {Key}", key);
                Interlocked.Increment(ref _missCount);
                return null;
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
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);

                var options = new DistributedCacheEntryOptions();
                var expirationTime = expiration ?? _options.DefaultExpiration;

                if (expirationTime != TimeSpan.Zero)
                {
                    options.SetAbsoluteExpiration(expirationTime);
                }

                await _distributedCache.SetAsync(fullKey, bytes, options).ConfigureAwait(false);

                _logger.LogTrace("Set distributed cache value for key: {Key} with expiration: {Expiration}",
                    fullKey, expirationTime);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting distributed cache value for key: {Key}", key);
                return false;
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
                await _distributedCache.RemoveAsync(fullKey).ConfigureAwait(false);

                _logger.LogTrace("Removed distributed cache entry for key: {Key}", fullKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing distributed cache value for key: {Key}", key);
                return false;
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
                var value = await _distributedCache.GetAsync(fullKey).ConfigureAwait(false);
                var exists = value?.Length > 0;

                _logger.LogTrace("Distributed cache existence check for key: {Key} = {Exists}", fullKey, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking distributed cache key existence: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ClearAsync()
        {
            // Note: IDistributedCache doesn't have a clear method
            // This would need to be implemented based on the specific cache provider
            _logger.LogWarning("ClearAsync is not supported for distributed cache - operation skipped");

            // Reset local statistics
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);

            return await Task.FromResult(false).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
        {
            var result = new Dictionary<string, T?>();

            if (keys == null)
                return result;

            try
            {
                // Note: This implementation makes multiple individual calls
                // Some distributed cache providers support batch operations
                var tasks = keys.Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(async key =>
                    {
                        var value = await GetAsync<T>(key).ConfigureAwait(false);
                        return new KeyValuePair<string, T?>(key, value);
                    });

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var kvp in results)
                {
                    result[kvp.Key] = kvp.Value;
                }

                _logger.LogTrace("Retrieved {Count} distributed cache entries", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple distributed cache values");
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
                // Note: This implementation makes multiple individual calls
                // Some distributed cache providers support batch operations
                var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                var success = results.All(r => r);
                _logger.LogTrace("Set {Count} distributed cache entries, success: {Success}", items.Count, success);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple distributed cache values");
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
                _logger.LogTrace("Removed {Count} distributed cache entries", removedCount);

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing multiple distributed cache values");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                var hitCount = Interlocked.Read(ref _hitCount);
                var missCount = Interlocked.Read(ref _missCount);

                var statistics = new CacheStatistics
                {
                    TotalEntries = -1, // Not available for distributed cache
                    HitCount = hitCount,
                    MissCount = missCount,
                    EvictionCount = -1, // Not available for distributed cache
                    MemoryUsage = -1, // Not available for distributed cache
                    IsHealthy = await CheckHealthAsync().ConfigureAwait(false),
                    Metadata = new Dictionary<string, object>
                    {
                        ["CacheType"] = "Distributed",
                        ["DefaultExpiration"] = _options.DefaultExpiration,
                        ["KeyPrefix"] = _options.KeyPrefix,
                        ["SerializationFormat"] = "JSON"
                    }
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting distributed cache statistics");
                return new CacheStatistics { IsHealthy = false };
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
        /// Checks the health of the distributed cache.
        /// </summary>
        private async Task<bool> CheckHealthAsync()
        {
            try
            {
                var testKey = $"{_options.KeyPrefix}:healthcheck:{Guid.NewGuid()}";
                var testValue = System.Text.Encoding.UTF8.GetBytes("healthcheck");

                // Try to set and get a test value
                await _distributedCache.SetAsync(testKey, testValue).ConfigureAwait(false);
                var result = await _distributedCache.GetAsync(testKey).ConfigureAwait(false);
                await _distributedCache.RemoveAsync(testKey).ConfigureAwait(false);

                return result?.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Distributed cache health check failed");
                return false;
            }
        }
    }

    /// <summary>
    /// Configuration options for DistributedCacheService.
    /// </summary>
    public class DistributedCacheServiceOptions
    {
        /// <summary>
        /// Gets or sets the default expiration time for cache entries.
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the key prefix for all cache entries.
        /// </summary>
        public string KeyPrefix { get; set; } = "NSL";
    }
}
