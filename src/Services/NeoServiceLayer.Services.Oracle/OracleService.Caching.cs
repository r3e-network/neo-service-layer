using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Caching;
using NeoServiceLayer.Services.Oracle.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Caching functionality for the Oracle service to reduce external API calls.
/// </summary>
public partial class OracleService
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _priceCacheExpiration = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _staticDataCacheExpiration = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets cached price data or fetches from source if not cached.
    /// </summary>
    public async Task<decimal> GetCachedPriceAsync(
        string symbol,
        string source,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"price_{symbol}_{source}";
        
        // Try to get from cache first
        if (_memoryCache.TryGetValue<decimal>(cacheKey, out var cachedPrice))
        {
            _logger.LogDebug("Cache hit for price {Symbol} from {Source}", symbol, source);
            return cachedPrice;
        }

        // Cache miss - fetch from source
        _logger.LogDebug("Cache miss for price {Symbol} from {Source}. Fetching from source.", symbol, source);
        
        try
        {
            // Use circuit breaker for resilient external calls
            var priceData = await ExecuteInEnclaveAsync(async () =>
            {
                var jsonResult = await OracleGetPriceDataAsync(symbol, source, cancellationToken)
                    .ConfigureAwait(false);
                
                // Parse the JSON result to extract price
                var priceInfo = System.Text.Json.JsonSerializer.Deserialize<PriceData>(jsonResult);
                return priceInfo?.Price ?? 0m;
            }).ConfigureAwait(false);

            // Cache the result
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_priceCacheExpiration)
                .SetSlidingExpiration(TimeSpan.FromSeconds(30))
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogDebug("Price cache entry {Key} evicted. Reason: {Reason}", key, reason);
                });

            _memoryCache.Set(cacheKey, priceData, cacheEntryOptions);
            
            _logger.LogInformation("Cached price for {Symbol} from {Source}: {Price}", 
                symbol, source, priceData);
            
            return priceData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol} from {Source}", symbol, source);
            throw;
        }
    }

    /// <summary>
    /// Gets cached aggregated data or fetches from sources if not cached.
    /// </summary>
    public async Task<string> GetCachedAggregatedDataAsync(
        string[] sources,
        string aggregationMethod,
        CancellationToken cancellationToken = default)
    {
        var sourcesKey = string.Join("_", sources);
        var cacheKey = $"aggregated_{sourcesKey}_{aggregationMethod}";
        
        // Try to get from cache first
        if (_memoryCache.TryGetValue<string>(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache hit for aggregated data from {SourceCount} sources", sources.Length);
            return cachedData;
        }

        // Cache miss - fetch and aggregate from sources
        _logger.LogDebug("Cache miss for aggregated data. Fetching from {SourceCount} sources.", sources.Length);
        
        try
        {
            var aggregatedData = await OracleAggregateDataAsync(sources, aggregationMethod, cancellationToken)
                .ConfigureAwait(false);

            // Cache the result with appropriate expiration based on data type
            var cacheExpiration = DetermineCacheExpiration(aggregationMethod);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(cacheExpiration)
                .SetPriority(CacheItemPriority.Normal);

            _memoryCache.Set(cacheKey, aggregatedData, cacheEntryOptions);
            
            _logger.LogInformation("Cached aggregated data from {SourceCount} sources using {Method}", 
                sources.Length, aggregationMethod);
            
            return aggregatedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating data from {SourceCount} sources", sources.Length);
            throw;
        }
    }

    /// <summary>
    /// Invalidates cache entries for a specific symbol or pattern.
    /// </summary>
    public void InvalidateCache(string pattern)
    {
        // Note: IMemoryCache doesn't support pattern-based removal
        // This would need to be enhanced with a cache key tracking mechanism
        _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        if (_memoryCache is MemoryCache memCache)
        {
            var stats = memCache.GetCurrentStatistics();
            if (stats != null)
            {
                return new CacheStatistics
                {
                    TotalEntries = stats.CurrentEntryCount,
                    HitCount = stats.TotalHits,
                    MissCount = stats.TotalMisses,
                    MemoryUsage = stats.CurrentEstimatedSize ?? 0,
                    IsHealthy = true
                };
            }
        }

        return new CacheStatistics
        {
            IsHealthy = true,
            TotalEntries = -1,
            HitCount = -1,
            MissCount = -1,
            MemoryUsage = -1
        };
    }

    /// <summary>
    /// Determines the appropriate cache expiration based on the data type.
    /// </summary>
    private TimeSpan DetermineCacheExpiration(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "price" => _priceCacheExpiration,
            "static" => _staticDataCacheExpiration,
            "realtime" => TimeSpan.FromSeconds(10),
            "historical" => TimeSpan.FromHours(1),
            _ => _defaultCacheExpiration
        };
    }

    /// <summary>
    /// Warms up the cache with frequently accessed data.
    /// </summary>
    public async Task WarmUpCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cache warm-up for Oracle service");

        // Define frequently accessed symbols
        var frequentSymbols = new[] { "ETH/USD", "BTC/USD", "NEO/USD", "GAS/USD" };
        var sources = new[] { "binance", "coinbase", "kraken" };

        var warmUpTasks = new List<Task>();

        foreach (var symbol in frequentSymbols)
        {
            foreach (var source in sources)
            {
                warmUpTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await GetCachedPriceAsync(symbol, source, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to warm up cache for {Symbol} from {Source}", 
                            symbol, source);
                    }
                }, cancellationToken));
            }
        }

        await Task.WhenAll(warmUpTasks).ConfigureAwait(false);
        
        _logger.LogInformation("Cache warm-up completed. Loaded {Count} entries", warmUpTasks.Count);
    }
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public class CacheStatistics
{
    public long TotalEntries { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long EvictionCount { get; set; }
    public long MemoryUsage { get; set; }
    public bool IsHealthy { get; set; }
    public double HitRate => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;
}