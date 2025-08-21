using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Caching
{
    /// <summary>
    /// Comprehensive distributed caching service for Neo Service Layer
    /// Provides high-performance, scalable caching with security and consistency features
    /// </summary>
    public interface IDistributedCachingService
    {
        /// <summary>
        /// Gets a cached value by key
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cached value or null if not found</returns>
        Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple cached values by keys
        /// </summary>
        /// <typeparam name="T">Type of cached values</typeparam>
        /// <param name="keys">Cache keys</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of found cached values</returns>
        Task<Dictionary<string, CacheResult<T>>> GetManyAsync<T>(
            IEnumerable<string> keys,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in the cache
        /// </summary>
        /// <typeparam name="T">Type of value to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="options">Caching options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Set operation result</returns>
        Task<CacheSetResult> SetAsync<T>(
            string key,
            T value,
            CacheOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets multiple values in the cache
        /// </summary>
        /// <typeparam name="T">Type of values to cache</typeparam>
        /// <param name="items">Key-value pairs to cache</param>
        /// <param name="options">Caching options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Set operation results</returns>
        Task<Dictionary<string, CacheSetResult>> SetManyAsync<T>(
            Dictionary<string, T> items,
            CacheOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a cached value by key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Remove operation result</returns>
        Task<CacheRemoveResult> RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple cached values by keys
        /// </summary>
        /// <param name="keys">Cache keys</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Remove operation results</returns>
        Task<Dictionary<string, CacheRemoveResult>> RemoveManyAsync(
            IEnumerable<string> keys,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes cached values matching a pattern
        /// </summary>
        /// <param name="pattern">Key pattern (supports wildcards)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bulk remove operation result</returns>
        Task<CacheBulkRemoveResult> RemoveByPatternAsync(
            string pattern,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in the cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if key exists</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets expiration time for an existing cached value
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="expiration">Expiration time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Expiration set result</returns>
        Task<CacheExpirationResult> SetExpirationAsync(
            string key,
            TimeSpan expiration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets time-to-live for a cached value
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Time-to-live information</returns>
        Task<CacheTtlResult> GetTtlAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments a numeric cached value atomically
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="increment">Increment amount</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Increment operation result</returns>
        Task<CacheIncrementResult> IncrementAsync(
            string key,
            long increment = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrements a numeric cached value atomically
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="decrement">Decrement amount</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Decrement operation result</returns>
        Task<CacheIncrementResult> DecrementAsync(
            string key,
            long decrement = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs atomic compare-and-set operation
        /// </summary>
        /// <typeparam name="T">Type of cached value</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="expectedValue">Expected current value</param>
        /// <param name="newValue">New value to set</param>
        /// <param name="options">Caching options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Compare-and-set operation result</returns>
        Task<CacheCompareSetResult> CompareAndSetAsync<T>(
            string key,
            T expectedValue,
            T newValue,
            CacheOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a distributed lock
        /// </summary>
        /// <param name="lockKey">Lock key</param>
        /// <param name="lockOptions">Lock options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Distributed lock handle</returns>
        Task<IDistributedLock?> AcquireLockAsync(
            string lockKey,
            DistributedLockOptions? lockOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics and information about cache usage
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cache statistics</returns>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cache health information
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cache health information</returns>
        Task<CacheHealth> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries for a specific tag
        /// </summary>
        /// <param name="tag">Cache tag</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag invalidation result</returns>
        Task<CacheTagInvalidationResult> InvalidateByTagAsync(
            string tag,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Warms up the cache with precomputed values
        /// </summary>
        /// <param name="warmupItems">Items to preload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cache warmup result</returns>
        Task<CacheWarmupResult> WarmupAsync(
            Dictionary<string, CacheWarmupItem> warmupItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes all cached data (use with caution)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Flush operation result</returns>
        Task<CacheFlushResult> FlushAllAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a distributed lock
    /// </summary>
    public interface IDistributedLock : IDisposable
    {
        /// <summary>
        /// Lock identifier
        /// </summary>
        string LockId { get; }

        /// <summary>
        /// Lock key
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Whether the lock is currently held
        /// </summary>
        bool IsAcquired { get; }

        /// <summary>
        /// When the lock was acquired
        /// </summary>
        DateTime AcquiredAt { get; }

        /// <summary>
        /// When the lock expires
        /// </summary>
        DateTime ExpiresAt { get; }

        /// <summary>
        /// Extends the lock expiration time
        /// </summary>
        /// <param name="extension">Time to extend</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extension result</returns>
        Task<LockExtensionResult> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases the lock
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Release result</returns>
        Task<LockReleaseResult> ReleaseAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration options for caching
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// Time-to-live for the cached value
        /// </summary>
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Absolute expiration time
        /// </summary>
        public DateTime? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Sliding expiration (resets on access)
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Cache priority
        /// </summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;

        /// <summary>
        /// Whether to compress the cached value
        /// </summary>
        public bool Compress { get; set; } = false;

        /// <summary>
        /// Whether to encrypt the cached value
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// Cache tags for invalidation
        /// </summary>
        public HashSet<string> Tags { get; set; } = new();

        /// <summary>
        /// Cache region for organization
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Serialization format
        /// </summary>
        public SerializationFormat SerializationFormat { get; set; } = SerializationFormat.Json;

        /// <summary>
        /// Whether to use distributed consistency
        /// </summary>
        public bool UseStrongConsistency { get; set; } = false;

        /// <summary>
        /// Cache replication factor
        /// </summary>
        public int ReplicationFactor { get; set; } = 1;
    }

    /// <summary>
    /// Configuration options for distributed locks
    /// </summary>
    public class DistributedLockOptions
    {
        /// <summary>
        /// Lock expiration time
        /// </summary>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Maximum wait time to acquire the lock
        /// </summary>
        public TimeSpan? MaxWaitTime { get; set; }

        /// <summary>
        /// Whether to auto-extend the lock
        /// </summary>
        public bool AutoExtend { get; set; } = true;

        /// <summary>
        /// Auto-extension interval
        /// </summary>
        public TimeSpan AutoExtendInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Lock owner identifier
        /// </summary>
        public string? OwnerId { get; set; }

        /// <summary>
        /// Additional metadata for the lock
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Item for cache warmup
    /// </summary>
    public class CacheWarmupItem
    {
        /// <summary>
        /// Value to cache
        /// </summary>
        public object Value { get; set; } = null!;

        /// <summary>
        /// Cache options for the item
        /// </summary>
        public CacheOptions? Options { get; set; }

        /// <summary>
        /// Priority for warmup (higher first)
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Enumerations for caching
    /// </summary>
    public enum CachePriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    public enum SerializationFormat
    {
        Json,
        MessagePack,
        ProtocolBuffers,
        Binary
    }

    public enum CacheConsistencyLevel
    {
        Eventual,
        Strong,
        Sequential
    }

    /// <summary>
    /// Result classes for cache operations
    /// </summary>
    public class CacheResult<T>
    {
        public bool Success { get; set; }
        public bool Found { get; set; }
        public T? Value { get; set; }
        public DateTime? CachedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheSetResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public DateTime SetAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheRemoveResult
    {
        public bool Success { get; set; }
        public bool Found { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheBulkRemoveResult
    {
        public bool Success { get; set; }
        public int RemovedCount { get; set; }
        public List<string> RemovedKeys { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheExpirationResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public DateTime? NewExpirationTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CacheTtlResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public TimeSpan? TimeToLive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CacheIncrementResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public long NewValue { get; set; }
        public long Change { get; set; }
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheCompareSetResult
    {
        public bool Success { get; set; }
        public bool ValueChanged { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheTagInvalidationResult
    {
        public bool Success { get; set; }
        public string Tag { get; set; } = string.Empty;
        public int InvalidatedCount { get; set; }
        public List<string> InvalidatedKeys { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public CacheMetrics Metrics { get; set; } = new();
    }

    public class CacheWarmupResult
    {
        public bool Success { get; set; }
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<string> FailedKeys { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CacheFlushResult
    {
        public bool Success { get; set; }
        public int FlushedCount { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LockExtensionResult
    {
        public bool Success { get; set; }
        public string LockId { get; set; } = string.Empty;
        public DateTime NewExpirationTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LockReleaseResult
    {
        public bool Success { get; set; }
        public string LockId { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Cache statistics and metrics
    /// </summary>
    public class CacheStatistics
    {
        public long TotalRequests { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
        
        public long TotalSets { get; set; }
        public long TotalRemoves { get; set; }
        public long TotalEvictions { get; set; }
        
        public long CurrentItemCount { get; set; }
        public long TotalMemoryUsageBytes { get; set; }
        public long MaxMemoryUsageBytes { get; set; }
        
        public TimeSpan AverageGetLatency { get; set; }
        public TimeSpan AverageSetLatency { get; set; }
        
        public int ActiveConnectionCount { get; set; }
        public DateTime CollectedAt { get; set; }
        
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    public class CacheHealth
    {
        public HealthStatus Status { get; set; }
        public bool IsConnected { get; set; }
        public int ConnectedNodes { get; set; }
        public int TotalNodes { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double CpuUsagePercent { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class CacheMetrics
    {
        public TimeSpan ExecutionTime { get; set; }
        public long DataSizeBytes { get; set; }
        public bool WasCompressed { get; set; }
        public bool WasEncrypted { get; set; }
        public string? NodeId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }
}