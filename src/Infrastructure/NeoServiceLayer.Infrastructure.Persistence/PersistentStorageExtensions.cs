using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Persistence;

/// <summary>
/// Extension methods for persistent storage providers to add common functionality.
/// </summary>
public static class PersistentStorageExtensions
{
    /// <summary>
    /// Performs an atomic operation using transactions if supported.
    /// </summary>
    public static async Task<T> ExecuteTransactionAsync<T>(
        this IPersistentStorageProvider storage,
        Func<IStorageTransaction?, Task<T>> operation,
        ILogger? logger = null)
    {
        if (storage.SupportsTransactions)
        {
            using var transaction = await storage.BeginTransactionAsync();
            try
            {
                var result = await operation(transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            return await operation(null);
        }
    }

    /// <summary>
    /// Stores data with standardized error handling and logging.
    /// </summary>
    public static async Task<bool> StoreWithLoggingAsync(
        this IPersistentStorageProvider storage,
        string key,
        byte[] data,
        StorageOptions? options = null,
        ILogger? logger = null,
        string? operationName = null)
    {
        try
        {
            await storage.StoreAsync(key, data, options);
            logger?.LogDebug("Successfully stored {OperationName} data at key {Key}", 
                operationName ?? "persistent", key);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to store {OperationName} data at key {Key}", 
                operationName ?? "persistent", key);
            return false;
        }
    }

    /// <summary>
    /// Retrieves data with standardized error handling and logging.
    /// </summary>
    public static async Task<byte[]?> RetrieveWithLoggingAsync(
        this IPersistentStorageProvider storage,
        string key,
        ILogger? logger = null,
        string? operationName = null)
    {
        try
        {
            var data = await storage.RetrieveAsync(key);
            if (data != null)
            {
                logger?.LogDebug("Successfully retrieved {OperationName} data from key {Key}",
                    operationName ?? "persistent", key);
            }
            return data;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to retrieve {OperationName} data from key {Key}",
                operationName ?? "persistent", key);
            return null;
        }
    }

    /// <summary>
    /// Stores an object as JSON with consistent serialization.
    /// </summary>
    public static async Task<bool> StoreObjectAsync<T>(
        this IPersistentStorageProvider storage,
        string key,
        T obj,
        StorageOptions? options = null,
        ILogger? logger = null,
        string? operationName = null)
    {
        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(obj);
            return await storage.StoreWithLoggingAsync(key, data, options, logger, operationName);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to serialize and store {OperationName} object at key {Key}",
                operationName ?? "persistent", key);
            return false;
        }
    }

    /// <summary>
    /// Retrieves an object from JSON with consistent deserialization.
    /// </summary>
    public static async Task<T?> RetrieveObjectAsync<T>(
        this IPersistentStorageProvider storage,
        string key,
        ILogger? logger = null,
        string? operationName = null) where T : class
    {
        try
        {
            var data = await storage.RetrieveWithLoggingAsync(key, logger, operationName);
            if (data != null)
            {
                return JsonSerializer.Deserialize<T>(data);
            }
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to retrieve and deserialize {OperationName} object from key {Key}",
                operationName ?? "persistent", key);
            return null;
        }
    }

    /// <summary>
    /// Manages an index by adding or removing an item.
    /// </summary>
    public static async Task<bool> UpdateIndexAsync(
        this IPersistentStorageProvider storage,
        string indexKey,
        string itemId,
        bool add = true,
        ILogger? logger = null)
    {
        try
        {
            var existingData = await storage.RetrieveAsync(indexKey);
            
            HashSet<string> items;
            if (existingData != null)
            {
                items = JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>();
            }
            else
            {
                items = new HashSet<string>();
            }

            bool changed = add ? items.Add(itemId) : items.Remove(itemId);
            
            if (changed)
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(items);
                await storage.StoreAsync(indexKey, data, new StorageOptions
                {
                    Encrypt = false,
                    Compress = true
                });
                
                logger?.LogDebug("{Action} item {ItemId} in index {IndexKey}",
                    add ? "Added" : "Removed", itemId, indexKey);
            }
            
            return changed;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to update index {IndexKey} with item {ItemId}",
                indexKey, itemId);
            return false;
        }
    }

    /// <summary>
    /// Performs cleanup of expired keys based on pattern and age.
    /// </summary>
    public static async Task<int> CleanupExpiredKeysAsync(
        this IPersistentStorageProvider storage,
        string pattern,
        TimeSpan maxAge,
        Func<string, DateTime?>? timestampExtractor = null,
        ILogger? logger = null)
    {
        try
        {
            var keys = await storage.ListKeysAsync(pattern);
            var cutoffDate = DateTime.UtcNow - maxAge;
            int deletedCount = 0;

            foreach (var key in keys)
            {
                DateTime? timestamp = null;
                
                if (timestampExtractor != null)
                {
                    timestamp = timestampExtractor(key);
                }
                else
                {
                    // Try to extract timestamp from key format: prefix:timestamp:suffix
                    var parts = key.Split(':');
                    if (parts.Length >= 2)
                    {
                        foreach (var part in parts)
                        {
                            if (DateTime.TryParseExact(part, "yyyyMMddHHmmss", null,
                                System.Globalization.DateTimeStyles.None, out var parsedTime))
                            {
                                timestamp = parsedTime;
                                break;
                            }
                        }
                    }
                }

                if (timestamp.HasValue && timestamp.Value < cutoffDate)
                {
                    await storage.DeleteAsync(key);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                logger?.LogInformation("Cleaned up {Count} expired keys matching pattern {Pattern}",
                    deletedCount, pattern);
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during cleanup of expired keys with pattern {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// Validates storage integrity and reports issues.
    /// </summary>
    public static async Task<StorageValidationResult> ValidateStorageAsync(
        this IPersistentStorageProvider storage,
        ILogger? logger = null)
    {
        try
        {
            var validationResult = await storage.ValidateIntegrityAsync();
            
            if (validationResult.IsValid)
            {
                logger?.LogInformation("Storage validation passed");
            }
            else
            {
                logger?.LogWarning("Storage validation failed: {Errors}",
                    string.Join(", ", validationResult.Errors ?? new List<string>()));
            }
            
            return validationResult;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during storage validation");
            return new StorageValidationResult
            {
                IsValid = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Creates a backup of service data.
    /// </summary>
    public static async Task<bool> BackupServiceDataAsync(
        this IPersistentStorageProvider storage,
        string servicePrefix,
        string backupPath,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Starting backup of service data with prefix {Prefix} to {Path}",
                servicePrefix, backupPath);
            
            var success = await storage.BackupAsync(backupPath);
            
            if (success)
            {
                logger?.LogInformation("Successfully backed up service data to {Path}", backupPath);
            }
            else
            {
                logger?.LogError("Failed to backup service data to {Path}", backupPath);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during backup of service data to {Path}", backupPath);
            return false;
        }
    }

    /// <summary>
    /// Restores service data from backup.
    /// </summary>
    public static async Task<bool> RestoreServiceDataAsync(
        this IPersistentStorageProvider storage,
        string backupPath,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Starting restore of service data from {Path}", backupPath);
            
            var success = await storage.RestoreAsync(backupPath);
            
            if (success)
            {
                logger?.LogInformation("Successfully restored service data from {Path}", backupPath);
            }
            else
            {
                logger?.LogError("Failed to restore service data from {Path}", backupPath);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during restore of service data from {Path}", backupPath);
            return false;
        }
    }

    /// <summary>
    /// Compacts storage to reclaim space.
    /// </summary>
    public static async Task<bool> CompactServiceStorageAsync(
        this IPersistentStorageProvider storage,
        ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Starting storage compaction");
            
            var success = await storage.CompactAsync();
            
            if (success)
            {
                logger?.LogInformation("Successfully compacted storage");
            }
            else
            {
                logger?.LogWarning("Storage compaction completed with warnings or was not needed");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during storage compaction");
            return false;
        }
    }

    /// <summary>
    /// Gets storage statistics for monitoring.
    /// </summary>
    public static async Task<StorageStatistics> GetStorageStatisticsAsync(
        this IPersistentStorageProvider storage,
        string servicePrefix,
        ILogger? logger = null)
    {
        try
        {
            var stats = await storage.GetStatisticsAsync();
            
            logger?.LogDebug("Retrieved storage statistics: TotalSize={TotalSize}",
                stats.TotalSize);
            
            return stats;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error retrieving storage statistics");
            return new StorageStatistics();
        }
    }
}

/// <summary>
/// Common storage key patterns and utilities.
/// </summary>
public static class StorageKeyPatterns
{
    /// <summary>
    /// Creates a timestamped key for historical data.
    /// </summary>
    public static string CreateTimestampedKey(string prefix, string identifier, DateTime? timestamp = null)
    {
        var ts = (timestamp ?? DateTime.UtcNow).ToString("yyyyMMddHHmmss");
        return $"{prefix}{identifier}:{ts}";
    }

    /// <summary>
    /// Creates an index key for categorized data.
    /// </summary>
    public static string CreateIndexKey(string prefix, string category, string value)
    {
        return $"{prefix}index:{category}:{value}";
    }

    /// <summary>
    /// Creates a metadata key for an entity.
    /// </summary>
    public static string CreateMetadataKey(string prefix, string entityId)
    {
        return $"{prefix}metadata:{entityId}";
    }

    /// <summary>
    /// Creates a statistics key for aggregated data.
    /// </summary>
    public static string CreateStatsKey(string prefix, string statsType, DateTime? timestamp = null)
    {
        if (timestamp.HasValue)
        {
            var ts = timestamp.Value.ToString("yyyyMMddHHmmss");
            return $"{prefix}stats:{statsType}:{ts}";
        }
        return $"{prefix}stats:{statsType}";
    }

    /// <summary>
    /// Extracts timestamp from timestamped key.
    /// </summary>
    public static DateTime? ExtractTimestamp(string key)
    {
        var parts = key.Split(':');
        foreach (var part in parts)
        {
            if (DateTime.TryParseExact(part, "yyyyMMddHHmmss", null,
                System.Globalization.DateTimeStyles.None, out var timestamp))
            {
                return timestamp;
            }
        }
        return null;
    }
}