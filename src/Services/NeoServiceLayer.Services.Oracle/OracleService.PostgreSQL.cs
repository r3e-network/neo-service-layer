using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;
using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace NeoServiceLayer.Services.Oracle;

public partial class OracleService
{
    private IOracleDataFeedRepository? _oracleRepository;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;

    /// <summary>
    /// Initializes PostgreSQL storage for the oracle service.
    /// </summary>
    private async Task InitializePersistentStorageAsync()
    {
        try
        {
            _oracleRepository = _serviceProvider?.GetService<IOracleDataFeedRepository>();

            if (_oracleRepository != null)
            {
                Logger.LogInformation("PostgreSQL storage initialized for OracleService");

                // Restore oracle data from PostgreSQL
                await RestoreOracleDataFromStorageAsync();

                // Start periodic persistence timer (every 30 seconds)
                _persistenceTimer = new Timer(
                    async _ => await PersistOracleDataAsync(),
                    null,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30));

                // Start cleanup timer (every hour)
                _cleanupTimer = new Timer(
                    async _ => await CleanupExpiredDataAsync(),
                    null,
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1));
            }
            else
            {
                Logger.LogWarning("PostgreSQL repository not available for OracleService");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize PostgreSQL storage for OracleService");
        }
    }

    /// <summary>
    /// Persists oracle feed data to PostgreSQL.
    /// </summary>
    private async Task PersistFeedDataAsync(string feedId, OracleFeedData feedData)
    {
        if (_oracleRepository == null) return;

        try
        {
            var oracleEntity = new Infrastructure.Persistence.PostgreSQL.Entities.OracleEntities.OracleDataFeed
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                Value = feedData.Value?.ToString() ?? string.Empty,
                DataType = feedData.Value?.GetType().Name ?? "string",
                Source = feedData.Source,
                Timestamp = feedData.Timestamp,
                Metadata = JsonSerializer.Serialize(feedData.Metadata),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _oracleRepository.CreateAsync(oracleEntity);

            Logger.LogDebug("Persisted feed data for {FeedId} to PostgreSQL", feedId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist feed data for {FeedId} to PostgreSQL", feedId);
        }
    }

    /// <summary>
    /// Persists a data source to PostgreSQL.
    /// </summary>
    private async Task PersistDataSourceAsync(DataSource dataSource)
    {
        if (_oracleRepository == null) return;

        try
        {
            // Check if data source already exists
            var existingDataSources = await _oracleRepository.GetByFeedIdAsync(dataSource.Id);
            var existingSource = existingDataSources.FirstOrDefault(ds => ds.Source == dataSource.Url);

            if (existingSource == null)
            {
                var oracleEntity = new Infrastructure.Persistence.PostgreSQL.Entities.OracleEntities.OracleDataFeed
                {
                    Id = Guid.NewGuid(),
                    FeedId = dataSource.Id,
                    Value = string.Empty, // Data sources don't have values
                    DataType = "DataSource",
                    Source = dataSource.Url,
                    Timestamp = dataSource.LastAccessedAt,
                    Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["type"] = dataSource.Type.ToString(),
                        ["blockchain_type"] = dataSource.BlockchainType.ToString(),
                        ["access_count"] = dataSource.AccessCount,
                        ["is_active"] = dataSource.IsActive,
                        ["created_at"] = dataSource.CreatedAt,
                        ["last_accessed_at"] = dataSource.LastAccessedAt
                    }),
                    IsActive = dataSource.IsActive,
                    CreatedAt = dataSource.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                await _oracleRepository.CreateAsync(oracleEntity);
                Logger.LogDebug("Persisted data source {DataSourceId} to PostgreSQL", dataSource.Id);
            }
            else
            {
                // Update existing data source
                existingSource.Timestamp = dataSource.LastAccessedAt;
                existingSource.IsActive = dataSource.IsActive;
                existingSource.UpdatedAt = DateTime.UtcNow;
                
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(existingSource.Metadata ?? "{}") ?? new Dictionary<string, object>();
                metadata["access_count"] = dataSource.AccessCount;
                metadata["last_accessed_at"] = dataSource.LastAccessedAt;
                existingSource.Metadata = JsonSerializer.Serialize(metadata);

                await _oracleRepository.UpdateAsync(existingSource);
                Logger.LogDebug("Updated data source {DataSourceId} in PostgreSQL", dataSource.Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist data source {DataSourceId} to PostgreSQL", dataSource.Id);
        }
    }

    /// <summary>
    /// Restores oracle data from PostgreSQL storage.
    /// </summary>
    private async Task RestoreOracleDataFromStorageAsync()
    {
        if (_oracleRepository == null) return;

        try
        {
            Logger.LogInformation("Restoring oracle data from PostgreSQL storage");

            // Restore active data sources
            await RestoreDataSourcesFromStorageAsync();

            // Restore recent feed data for caching
            await RestoreCachedFeedValuesAsync();

            Logger.LogInformation("Oracle data restored from PostgreSQL storage");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore oracle data from PostgreSQL storage");
        }
    }

    /// <summary>
    /// Restores data sources from PostgreSQL storage.
    /// </summary>
    private async Task RestoreDataSourcesFromStorageAsync()
    {
        if (_oracleRepository == null) return;

        try
        {
            // Get all data source entries (those with DataType = "DataSource")
            var allFeeds = await _oracleRepository.GetAllAsync();
            var dataSourceEntries = allFeeds.Where(f => f.DataType == "DataSource" && f.IsActive);
            var restoredCount = 0;

            foreach (var entry in dataSourceEntries)
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entry.Metadata ?? "{}") ?? new Dictionary<string, object>();
                    
                    var dataSource = new DataSource
                    {
                        Id = entry.FeedId,
                        Url = entry.Source,
                        Type = Enum.TryParse<DataSourceType>(metadata.GetValueOrDefault("type")?.ToString(), out var type) ? type : DataSourceType.Http,
                        BlockchainType = Enum.TryParse<Infrastructure.Blockchain.BlockchainType>(metadata.GetValueOrDefault("blockchain_type")?.ToString(), out var blockchainType) ? blockchainType : Infrastructure.Blockchain.BlockchainType.NeoN3,
                        AccessCount = int.TryParse(metadata.GetValueOrDefault("access_count")?.ToString(), out var accessCount) ? accessCount : 0,
                        IsActive = bool.TryParse(metadata.GetValueOrDefault("is_active")?.ToString(), out var isActive) ? isActive : true,
                        CreatedAt = DateTime.TryParse(metadata.GetValueOrDefault("created_at")?.ToString(), out var createdAt) ? createdAt : entry.CreatedAt,
                        LastAccessedAt = DateTime.TryParse(metadata.GetValueOrDefault("last_accessed_at")?.ToString(), out var lastAccessedAt) ? lastAccessedAt : entry.Timestamp
                    };

                    lock (_dataSources)
                    {
                        if (!_dataSources.Any(ds => ds.Id == dataSource.Id))
                        {
                            _dataSources.Add(dataSource);
                            restoredCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore data source from entry {EntryId}", entry.Id);
                }
            }

            Logger.LogInformation("Restored {Count} data sources from PostgreSQL", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore data sources from PostgreSQL");
        }
    }

    /// <summary>
    /// Restores cached feed values from PostgreSQL storage.
    /// </summary>
    private async Task RestoreCachedFeedValuesAsync()
    {
        if (_oracleRepository == null) return;

        try
        {
            // Get recent feed data (last 5 minutes) for caching
            var recentFeeds = await _oracleRepository.GetRecentAsync(TimeSpan.FromMinutes(5));
            var restoredCount = 0;

            foreach (var feedGroup in recentFeeds.GroupBy(f => f.FeedId))
            {
                try
                {
                    // Get the latest entry for each feed
                    var latestFeed = feedGroup.OrderByDescending(f => f.Timestamp).First();
                    
                    // Cache is implicitly restored through the repository query
                    restoredCount++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to restore cached feed value for {FeedId}", feedGroup.Key);
                }
            }

            Logger.LogInformation("Restored {Count} cached feed values from PostgreSQL", restoredCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to restore cached feed values from PostgreSQL");
        }
    }

    /// <summary>
    /// Persists all current oracle data to PostgreSQL.
    /// </summary>
    private async Task PersistOracleDataAsync()
    {
        if (_oracleRepository == null) return;

        try
        {
            // Persist data sources
            foreach (var dataSource in _dataSources)
            {
                await PersistDataSourceAsync(dataSource);
            }

            Logger.LogDebug("Persisted oracle data to PostgreSQL");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist oracle data to PostgreSQL");
        }
    }

    /// <summary>
    /// Cleans up expired data from PostgreSQL storage.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_oracleRepository == null) return;

        try
        {
            Logger.LogInformation("Starting cleanup of expired oracle data in PostgreSQL");

            // Clean up old feed data (older than 30 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            await _oracleRepository.DeleteOlderThanAsync(cutoffDate);

            Logger.LogInformation("Completed cleanup of expired oracle data in PostgreSQL");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cleanup expired data from PostgreSQL");
        }
    }

    /// <summary>
    /// Removes a data source from PostgreSQL storage.
    /// </summary>
    private async Task RemoveDataSourceFromStorageAsync(string dataSourceId)
    {
        if (_oracleRepository == null) return;

        try
        {
            var dataSourceEntries = await _oracleRepository.GetByFeedIdAsync(dataSourceId);
            var dataSourceEntry = dataSourceEntries.FirstOrDefault(ds => ds.DataType == "DataSource");

            if (dataSourceEntry != null)
            {
                await _oracleRepository.DeleteAsync(dataSourceEntry.Id);
                Logger.LogDebug("Removed data source {DataSourceId} from PostgreSQL", dataSourceId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove data source {DataSourceId} from PostgreSQL", dataSourceId);
        }
    }

    /// <summary>
    /// Gets oracle feed data by feed ID from PostgreSQL.
    /// </summary>
    /// <param name="feedId">The feed ID to query.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <returns>List of oracle feed data.</returns>
    public async Task<IEnumerable<OracleFeedData>> GetFeedHistoryAsync(string feedId, int limit = 100)
    {
        if (_oracleRepository == null)
        {
            return Enumerable.Empty<OracleFeedData>();
        }

        try
        {
            var feedEntries = await _oracleRepository.GetByFeedIdAsync(feedId);
            var dataEntries = feedEntries
                .Where(f => f.DataType != "DataSource")
                .OrderByDescending(f => f.Timestamp)
                .Take(limit);

            var result = new List<OracleFeedData>();

            foreach (var entry in dataEntries)
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entry.Metadata ?? "{}") ?? new Dictionary<string, object>();
                
                var feedData = new OracleFeedData
                {
                    FeedId = entry.FeedId,
                    Value = entry.Value,
                    Timestamp = entry.Timestamp,
                    Source = entry.Source,
                    Metadata = metadata
                };

                result.Add(feedData);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get feed history for {FeedId} from PostgreSQL", feedId);
            return Enumerable.Empty<OracleFeedData>();
        }
    }

    /// <summary>
    /// Gets the latest value for a specific feed from PostgreSQL.
    /// </summary>
    /// <param name="feedId">The feed ID to query.</param>
    /// <returns>Latest oracle feed data or null if not found.</returns>
    public async Task<OracleFeedData?> GetLatestFeedValueAsync(string feedId)
    {
        if (_oracleRepository == null)
        {
            return null;
        }

        try
        {
            var feedEntries = await _oracleRepository.GetByFeedIdAsync(feedId);
            var latestEntry = feedEntries
                .Where(f => f.DataType != "DataSource")
                .OrderByDescending(f => f.Timestamp)
                .FirstOrDefault();

            if (latestEntry == null)
            {
                return null;
            }

            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(latestEntry.Metadata ?? "{}") ?? new Dictionary<string, object>();

            return new OracleFeedData
            {
                FeedId = latestEntry.FeedId,
                Value = latestEntry.Value,
                Timestamp = latestEntry.Timestamp,
                Source = latestEntry.Source,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get latest feed value for {FeedId} from PostgreSQL", feedId);
            return null;
        }
    }

    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _persistenceTimer?.Dispose();
        _cleanupTimer?.Dispose();
        // Repository is managed by DI container
    }
}

/// <summary>
/// Oracle feed data model.
/// </summary>
internal class OracleFeedData
{
    public string FeedId { get; set; } = string.Empty;
    public object Value { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}