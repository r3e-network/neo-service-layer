using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Infrastructure.Caching
{
    /// <summary>
    /// Interface for caching services with support for different storage backends.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached value or default if not found.</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Sets a value in the cache with optional expiration.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the key was found and removed, false otherwise.</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> ClearAsync();

        /// <summary>
        /// Gets multiple values from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached values.</typeparam>
        /// <param name="keys">The cache keys.</param>
        /// <returns>A dictionary of key-value pairs for found entries.</returns>
        Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;

        /// <summary>
        /// Sets multiple values in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="items">Dictionary of key-value pairs to cache.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <returns>True if all operations successful, false otherwise.</returns>
        Task<bool> SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Removes multiple values from the cache.
        /// </summary>
        /// <param name="keys">The cache keys to remove.</param>
        /// <returns>The number of keys that were removed.</returns>
        Task<int> RemoveManyAsync(IEnumerable<string> keys);

        /// <summary>
        /// Gets a value from cache or sets it using the provided factory function if not found.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="factory">Factory function to create the value if not found in cache.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <returns>The cached or newly created value.</returns>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Gets cache statistics and health information.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        Task<CacheStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Cache statistics information.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of entries in the cache.
        /// </summary>
        public long TotalEntries { get; set; }

        /// <summary>
        /// Gets or sets the cache hit count.
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// Gets or sets the cache miss count.
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// Gets or sets the cache hit ratio (0.0 to 1.0).
        /// </summary>
        public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;

        /// <summary>
        /// Gets or sets the approximate memory usage in bytes.
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Gets or sets the number of evicted entries.
        /// </summary>
        public long EvictionCount { get; set; }

        /// <summary>
        /// Gets or sets whether the cache is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets additional cache-specific metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
