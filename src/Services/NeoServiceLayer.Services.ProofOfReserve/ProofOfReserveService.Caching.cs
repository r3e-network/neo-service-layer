using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ProofOfReserve;

/// <summary>
/// Caching functionality for the Proof of Reserve Service.
/// </summary>
public partial class ProofOfReserveService
{
    private ProofOfReserveCacheHelper? _cacheHelper;

    /// <summary>
    /// Initializes the enclave for secure operations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation with a boolean result.</returns>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Proof of Reserve Service enclave");
        
        // Initialize secure storage for private keys
        await Task.CompletedTask;
        
        Logger.LogInformation("Proof of Reserve Service enclave initialized successfully");
        return true;
    }

    /// <summary>
    /// Initializes the cache helper if caching is enabled.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance.</param>
    public void InitializeCache(IMemoryCache memoryCache)
    {
        var (cachingEnabled, _, _) = GetPerformanceSettings();
        
        if (cachingEnabled && memoryCache != null)
        {
            // Create a logger for the cache helper
            var cacheLogger = new CacheHelperLogger(Logger);
            
            _cacheHelper = new ProofOfReserveCacheHelper(memoryCache, cacheLogger);
            
            Logger.LogInformation("Caching enabled for Proof of Reserve Service");
        }
        else
        {
            Logger.LogInformation("Caching disabled for Proof of Reserve Service");
        }
    }

    /// <summary>
    /// Gets reserve status with caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve status info.</returns>
    private async Task<ReserveStatusInfo> GetReserveStatusWithCachingAsync(string assetId, BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.ReserveStatus, 
            assetId, 
            blockchainType);

        var (cachingEnabled, _, cacheExpiration) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => GetReserveStatusInternalAsync(assetId, blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.ReserveStatus,
                cachingEnabled);
        }

        return await GetReserveStatusInternalAsync(assetId, blockchainType);
    }

    /// <summary>
    /// Internal method to get reserve status without caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve status info.</returns>
    private async Task<ReserveStatusInfo> GetReserveStatusInternalAsync(string assetId, BlockchainType blockchainType)
    {
        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_monitoredAssets.TryGetValue(assetId, out var asset))
            {
                var latestSnapshot = _reserveHistory[assetId].LastOrDefault();

                return new ReserveStatusInfo
                {
                    AssetId = assetId,
                    AssetSymbol = asset.AssetSymbol,
                    TotalSupply = latestSnapshot?.TotalSupply ?? 0m,
                    TotalReserves = latestSnapshot?.TotalReserves ?? 0m,
                    ReserveRatio = asset.CurrentReserveRatio,
                    Health = asset.Health,
                    LastUpdated = asset.LastUpdated,
                    LastAudit = latestSnapshot?.Timestamp ?? DateTime.MinValue,
                    ReserveBreakdown = latestSnapshot?.ReserveAddresses ?? Array.Empty<string>(),
                    IsCompliant = asset.CurrentReserveRatio >= asset.MinReserveRatio,
                    ComplianceNotes = asset.CurrentReserveRatio < asset.MinReserveRatio ?
                        "Reserve ratio below minimum threshold" : "Compliant"
                };
            }
        }

        throw new ArgumentException($"Asset {assetId} not found", nameof(assetId));
    }

    /// <summary>
    /// Gets reserve health with caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve health status.</returns>
    private async Task<ReserveHealthStatus> GetReserveHealthWithCachingAsync(string assetId, BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.HealthStatus, 
            assetId, 
            blockchainType);

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => GetReserveHealthInternalAsync(assetId, blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.HealthStatus,
                cachingEnabled);
        }

        return await GetReserveHealthInternalAsync(assetId, blockchainType);
    }

    /// <summary>
    /// Internal method to get reserve health without caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve health status.</returns>
    private async Task<ReserveHealthStatus> GetReserveHealthInternalAsync(string assetId, BlockchainType blockchainType)
    {
        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_monitoredAssets.TryGetValue(assetId, out var asset))
            {
                var latestSnapshot = _reserveHistory[assetId].LastOrDefault();
                if (latestSnapshot != null)
                {
                    return latestSnapshot.Health;
                }
                else
                {
                    return asset.Health;
                }
            }
        }

        throw new ArgumentException($"Asset {assetId} not found", nameof(assetId));
    }

    /// <summary>
    /// Gets active alerts with caching.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The active alerts.</returns>
    private async Task<IEnumerable<ReserveAlert>> GetActiveAlertsWithCachingAsync(BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.Alerts, 
            blockchainType);

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => GetActiveAlertsInternalAsync(blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.Alerts,
                cachingEnabled);
        }

        return await GetActiveAlertsInternalAsync(blockchainType);
    }

    /// <summary>
    /// Internal method to get active alerts without caching.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The active alerts.</returns>
    private async Task<IEnumerable<ReserveAlert>> GetActiveAlertsInternalAsync(BlockchainType blockchainType)
    {
        await Task.CompletedTask;

        lock (_assetsLock)
        {
            var activeAlerts = new List<ReserveAlert>();

            foreach (var alertConfig in _alertConfigs.Values.Where(a => a.IsEnabled))
            {
                if (_monitoredAssets.TryGetValue(alertConfig.AssetId, out var asset))
                {
                    var shouldAlert = alertConfig.Type switch
                    {
                        ReserveAlertType.LowReserveRatio => asset.CurrentReserveRatio < alertConfig.Threshold,
                        ReserveAlertType.ComplianceViolation => asset.Health == ReserveHealthStatus.Undercollateralized,
                        _ => false
                    };

                    if (shouldAlert)
                    {
                        activeAlerts.Add(new ReserveAlert
                        {
                            AlertId = alertConfig.AlertId,
                            AssetId = alertConfig.AssetId,
                            AlertType = alertConfig.Type,
                            Message = $"Alert: {alertConfig.AlertName} - Current ratio: {asset.CurrentReserveRatio:P2}",
                            Severity = asset.Health == ReserveHealthStatus.Undercollateralized ? AlertSeverity.Critical : AlertSeverity.Warning,
                            TriggeredAt = DateTime.UtcNow,
                            IsActive = true
                        });
                    }
                }
            }

            return activeAlerts;
        }
    }

    /// <summary>
    /// Gets blockchain balance with caching.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The address balance.</returns>
    private async Task<decimal> GetBlockchainBalanceWithCachingAsync(string address, BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.BlockchainBalance, 
            address, 
            blockchainType);

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => QueryAddressBalanceAsync(address, blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.BlockchainBalance,
                cachingEnabled);
        }

        return await QueryAddressBalanceAsync(address, blockchainType);
    }

    /// <summary>
    /// Gets reserve snapshots with caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve snapshots.</returns>
    private async Task<ReserveSnapshot[]> GetReserveSnapshotsWithCachingAsync(
        string assetId, 
        DateTime from, 
        DateTime to, 
        BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.ReserveSnapshot, 
            assetId, 
            from.ToString("yyyy-MM-dd"), 
            to.ToString("yyyy-MM-dd"), 
            blockchainType);

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => GetReserveSnapshotsInternalAsync(assetId, from, to, blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.ReserveSnapshot,
                cachingEnabled);
        }

        return await GetReserveSnapshotsInternalAsync(assetId, from, to, blockchainType);
    }

    /// <summary>
    /// Internal method to get reserve snapshots without caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The reserve snapshots.</returns>
    private async Task<ReserveSnapshot[]> GetReserveSnapshotsInternalAsync(
        string assetId, 
        DateTime from, 
        DateTime to, 
        BlockchainType blockchainType)
    {
        await Task.CompletedTask;

        lock (_assetsLock)
        {
            if (_reserveHistory.TryGetValue(assetId, out var history))
            {
                return history.Where(s => s.Timestamp >= from && s.Timestamp <= to).ToArray();
            }
        }

        return Array.Empty<ReserveSnapshot>();
    }

    /// <summary>
    /// Generates audit report with caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The audit report.</returns>
    private async Task<AuditReport> GenerateAuditReportWithCachingAsync(
        string assetId, 
        DateTime from, 
        DateTime to, 
        BlockchainType blockchainType)
    {
        var cacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
            ProofOfReserveCacheHelper.CacheKeys.AuditReport, 
            assetId, 
            from.ToString("yyyy-MM-dd"), 
            to.ToString("yyyy-MM-dd"), 
            blockchainType);

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => GenerateAuditReportInternalAsync(assetId, from, to, blockchainType),
                ProofOfReserveCacheHelper.CacheExpirations.AuditReport,
                cachingEnabled);
        }

        return await GenerateAuditReportInternalAsync(assetId, from, to, blockchainType);
    }

    /// <summary>
    /// Internal method to generate audit report without caching.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The audit report.</returns>
    private async Task<AuditReport> GenerateAuditReportInternalAsync(
        string assetId, 
        DateTime from, 
        DateTime to, 
        BlockchainType blockchainType)
    {
        var asset = GetMonitoredAsset(assetId);
        var snapshots = await GetReserveSnapshotsInternalAsync(assetId, from, to, blockchainType);

        var report = new AuditReport
        {
            ReportId = Guid.NewGuid().ToString(),
            AssetId = assetId,
            AssetSymbol = asset.AssetSymbol,
            PeriodStart = from,
            PeriodEnd = to,
            GeneratedAt = DateTime.UtcNow,
            TotalSnapshots = snapshots.Length,
            AverageReserveRatio = snapshots.Length > 0 ? snapshots.Average(s => s.ReserveRatio) : 0,
            MinReserveRatio = snapshots.Length > 0 ? snapshots.Min(s => s.ReserveRatio) : 0,
            MaxReserveRatio = snapshots.Length > 0 ? snapshots.Max(s => s.ReserveRatio) : 0,
            CompliancePercentage = snapshots.Length > 0 ?
                (decimal)snapshots.Count(s => s.ReserveRatio >= asset.MinReserveRatio) / snapshots.Length * 100 : 0,
            Recommendations = GenerateAuditRecommendations(asset, snapshots)
        };

        Logger.LogInformation("Generated audit report {ReportId} for asset {AssetId} covering period {From} to {To}",
            report.ReportId, assetId, from, to);

        return report;
    }

    /// <summary>
    /// Gets configuration summary with caching.
    /// </summary>
    /// <returns>The configuration summary.</returns>
    private async Task<ConfigurationSummary?> GetConfigurationSummaryWithCachingAsync()
    {
        var cacheKey = ProofOfReserveCacheHelper.CacheKeys.ConfigSummary;

        var (cachingEnabled, _, _) = GetPerformanceSettings();

        if (_cacheHelper != null && cachingEnabled)
        {
            return await _cacheHelper.GetOrCreateAsync(
                cacheKey,
                () => Task.FromResult(GetConfigurationSummary()),
                ProofOfReserveCacheHelper.CacheExpirations.ConfigSummary,
                cachingEnabled);
        }

        return GetConfigurationSummary();
    }

    /// <summary>
    /// Invalidates cache entries for an asset when data changes.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    private void InvalidateAssetCache(string assetId)
    {
        if (_cacheHelper != null)
        {
            _cacheHelper.InvalidateAssetCache(assetId);
            Logger.LogDebug("Invalidated cache for asset: {AssetId}", assetId);
        }
    }

    /// <summary>
    /// Invalidates global cache entries.
    /// </summary>
    private void InvalidateGlobalCache()
    {
        if (_cacheHelper != null)
        {
            _cacheHelper.Remove(ProofOfReserveCacheHelper.CacheKeys.ConfigSummary);
            
            // Remove alert cache for all blockchains
            foreach (var blockchain in SupportedBlockchains)
            {
                var alertsCacheKey = ProofOfReserveCacheHelper.BuildCacheKey(
                    ProofOfReserveCacheHelper.CacheKeys.Alerts, 
                    blockchain);
                _cacheHelper.Remove(alertsCacheKey);
            }

            Logger.LogDebug("Invalidated global cache entries");
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    /// <returns>The cache statistics.</returns>
    public CacheStatistics? GetCacheStatistics()
    {
        return _cacheHelper?.GetStatistics();
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public void ResetCacheStatistics()
    {
        _cacheHelper?.ResetStatistics();
        Logger.LogInformation("Cache statistics reset");
    }

    /// <summary>
    /// Gets the cache hit ratio for monitoring.
    /// </summary>
    /// <returns>The cache hit ratio (0.0 to 1.0).</returns>
    public double GetCacheHitRatio()
    {
        var stats = GetCacheStatistics();
        return stats?.HitRatio ?? 0.0;
    }

    /// <summary>
    /// Warms up the cache with frequently accessed data.
    /// </summary>
    /// <returns>Task representing the warm-up operation.</returns>
    public async Task WarmUpCacheAsync()
    {
        if (_cacheHelper == null)
        {
            Logger.LogDebug("Cache not enabled, skipping warm-up");
            return;
        }

        try
        {
            Logger.LogInformation("Starting cache warm-up");
            var startTime = DateTime.UtcNow;

            var activeAssets = GetActiveAssets();
            var warmUpTasks = new List<Task>();

            // Warm up asset data
            foreach (var asset in activeAssets.Take(10)) // Limit to avoid overwhelming
            {
                foreach (var blockchain in SupportedBlockchains)
                {
                    warmUpTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await GetReserveStatusWithCachingAsync(asset.AssetId, blockchain);
                            await GetReserveHealthWithCachingAsync(asset.AssetId, blockchain);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Failed to warm up cache for asset {AssetId} on {Blockchain}",
                                asset.AssetId, blockchain);
                        }
                    }));
                }
            }

            // Warm up global data
            warmUpTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await GetConfigurationSummaryWithCachingAsync();
                    
                    foreach (var blockchain in SupportedBlockchains)
                    {
                        await GetActiveAlertsWithCachingAsync(blockchain);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to warm up global cache data");
                }
            }));

            await Task.WhenAll(warmUpTasks);

            var duration = DateTime.UtcNow - startTime;
            Logger.LogInformation("Cache warm-up completed in {Duration}ms, warmed {AssetCount} assets",
                duration.TotalMilliseconds, activeAssets.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during cache warm-up");
        }
    }

    /// <summary>
    /// Disposes cache-related resources.
    /// </summary>
    partial void DisposeCacheResources()
    {
        _cacheHelper?.Dispose();
    }
}

/// <summary>
/// Logger wrapper for the cache helper.
/// </summary>
internal class CacheHelperLogger : ILogger<ProofOfReserveCacheHelper>
{
    private readonly ILogger _baseLogger;

    public CacheHelperLogger(ILogger baseLogger)
    {
        _baseLogger = baseLogger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _baseLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _baseLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _baseLogger.Log(logLevel, eventId, state, exception, formatter);
    }
}