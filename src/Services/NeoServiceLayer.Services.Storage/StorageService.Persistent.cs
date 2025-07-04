using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Persistent storage extensions for StorageService metadata management.
/// </summary>
public partial class StorageService
{
    private readonly IPersistentStorageProvider? _persistentMetadataStorage;
    private const string METADATA_PREFIX = "storage:metadata:";
    private const string INDEX_PREFIX = "storage:index:";
    private const string STATS_KEY = "storage:statistics";

    /// <summary>
    /// Initializes persistent metadata storage.
    /// </summary>
    private async Task InitializePersistentMetadataAsync()
    {
        if (_persistentMetadataStorage == null)
        {
            Logger.LogWarning("Persistent metadata storage not available, using in-memory cache only");
            return;
        }

        try
        {
            Logger.LogInformation("Initializing persistent metadata storage...");

            if (!_persistentMetadataStorage.IsInitialized)
            {
                await _persistentMetadataStorage.InitializeAsync();
            }

            // Load existing metadata into cache
            await LoadMetadataFromPersistentStorageAsync();

            Logger.LogInformation("Persistent metadata storage initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing persistent metadata storage");
        }
    }

    /// <summary>
    /// Loads metadata from persistent storage into memory cache.
    /// </summary>
    private async Task LoadMetadataFromPersistentStorageAsync()
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            Logger.LogInformation("Loading metadata from persistent storage...");

            var metadataKeys = await _persistentMetadataStorage.ListKeysAsync(METADATA_PREFIX);

            foreach (var key in metadataKeys)
            {
                var data = await _persistentMetadataStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var metadata = JsonSerializer.Deserialize<StorageMetadata>(data);
                    if (metadata != null)
                    {
                        // Extract storage key from metadata key
                        var storageKey = key.Substring(METADATA_PREFIX.Length);
                        _metadataCache[storageKey] = metadata;
                    }
                }
            }

            Logger.LogInformation("Loaded {Count} metadata entries from persistent storage", _metadataCache.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading metadata from persistent storage");
        }
    }

    /// <summary>
    /// Persists metadata to storage.
    /// </summary>
    private async Task PersistMetadataAsync(string storageKey, StorageMetadata metadata)
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            var key = $"{METADATA_PREFIX}{storageKey}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metadata);

            await _persistentMetadataStorage.StoreAsync(key, data, new Infrastructure.Persistence.StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "StorageMetadata",
                    ["StorageKey"] = storageKey,
                    ["CreatedAt"] = metadata.CreatedAt.ToString("O"),
                    ["Size"] = metadata.Size.ToString(),
                    ["StorageClass"] = metadata.StorageClass
                }
            });

            // Also update indexes for efficient queries
            await UpdateMetadataIndexesAsync(storageKey, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting metadata for key {Key}", storageKey);
        }
    }

    /// <summary>
    /// Removes metadata from persistent storage.
    /// </summary>
    private async Task RemovePersistedMetadataAsync(string storageKey)
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            var key = $"{METADATA_PREFIX}{storageKey}";
            await _persistentMetadataStorage.DeleteAsync(key);

            // Remove from indexes
            await RemoveFromMetadataIndexesAsync(storageKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing persisted metadata for key {Key}", storageKey);
        }
    }

    /// <summary>
    /// Updates metadata indexes for efficient queries.
    /// </summary>
    private async Task UpdateMetadataIndexesAsync(string storageKey, StorageMetadata metadata)
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            // Index by owner
            if (!string.IsNullOrEmpty(metadata.Owner))
            {
                var ownerIndexKey = $"{INDEX_PREFIX}owner:{metadata.Owner}:{storageKey}";
                var indexData = JsonSerializer.SerializeToUtf8Bytes(new MetadataIndex
                {
                    StorageKey = storageKey,
                    IndexedAt = DateTime.UtcNow
                });

                await _persistentMetadataStorage.StoreAsync(ownerIndexKey, indexData, new Infrastructure.Persistence.StorageOptions
                {
                    Encrypt = false,
                    Compress = false
                });
            }

            // Index by storage class
            var classIndexKey = $"{INDEX_PREFIX}class:{metadata.StorageClass}:{storageKey}";
            var classIndexData = JsonSerializer.SerializeToUtf8Bytes(new MetadataIndex
            {
                StorageKey = storageKey,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentMetadataStorage.StoreAsync(classIndexKey, classIndexData, new Infrastructure.Persistence.StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by creation date (for time-based queries)
            var dateIndexKey = $"{INDEX_PREFIX}date:{metadata.CreatedAt:yyyyMMdd}:{storageKey}";
            var dateIndexData = JsonSerializer.SerializeToUtf8Bytes(new MetadataIndex
            {
                StorageKey = storageKey,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentMetadataStorage.StoreAsync(dateIndexKey, dateIndexData, new Infrastructure.Persistence.StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating metadata indexes for key {Key}", storageKey);
        }
    }

    /// <summary>
    /// Removes metadata from all indexes.
    /// </summary>
    private async Task RemoveFromMetadataIndexesAsync(string storageKey)
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            // Find and remove all index entries for this storage key
            var indexKeys = await _persistentMetadataStorage.ListKeysAsync(INDEX_PREFIX);
            var keysToDelete = indexKeys.Where(k => k.EndsWith($":{storageKey}")).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentMetadataStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing metadata indexes for key {Key}", storageKey);
        }
    }

    /// <summary>
    /// Queries metadata by owner using indexes.
    /// </summary>
    private async Task<List<StorageMetadata>> QueryMetadataByOwnerAsync(string owner)
    {
        if (_persistentMetadataStorage == null)
        {
            // Fallback to in-memory cache
            return _metadataCache.Values.Where(m => m.Owner == owner).ToList();
        }

        var results = new List<StorageMetadata>();

        try
        {
            var ownerIndexPrefix = $"{INDEX_PREFIX}owner:{owner}:";
            var indexKeys = await _persistentMetadataStorage.ListKeysAsync(ownerIndexPrefix);

            foreach (var indexKey in indexKeys)
            {
                // Extract storage key from index key
                var storageKey = indexKey.Substring(ownerIndexPrefix.Length);

                // Load metadata
                var metadataKey = $"{METADATA_PREFIX}{storageKey}";
                var data = await _persistentMetadataStorage.RetrieveAsync(metadataKey);

                if (data != null)
                {
                    var metadata = JsonSerializer.Deserialize<StorageMetadata>(data);
                    if (metadata != null)
                    {
                        results.Add(metadata);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error querying metadata by owner {Owner}", owner);
            // Fallback to in-memory cache
            return _metadataCache.Values.Where(m => m.Owner == owner).ToList();
        }

        return results;
    }

    /// <summary>
    /// Persists storage statistics periodically.
    /// </summary>
    private async Task PersistStorageStatisticsAsync()
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            var stats = new InternalStorageStatistics
            {
                TotalItems = _metadataCache.Count,
                TotalSize = _metadataCache.Values.Sum(m => m.Size),
                ItemsByClass = _metadataCache.Values
                    .GroupBy(m => m.StorageClass)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ItemsByOwner = _metadataCache.Values
                    .Where(m => !string.IsNullOrEmpty(m.Owner))
                    .GroupBy(m => m.Owner)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastUpdated = DateTime.UtcNow,
                RequestCount = _requestCount,
                SuccessCount = _successCount,
                FailureCount = _failureCount
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentMetadataStorage.StoreAsync(STATS_KEY, data, new Infrastructure.Persistence.StorageOptions
            {
                Encrypt = false,
                Compress = true,
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Statistics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting storage statistics");
        }
    }

    /// <summary>
    /// Performs periodic maintenance on persistent metadata storage.
    /// </summary>
    private async Task PerformMetadataMaintenanceAsync()
    {
        if (_persistentMetadataStorage == null) return;

        try
        {
            Logger.LogInformation("Performing metadata maintenance...");

            // Validate storage integrity
            var validationResult = await _persistentMetadataStorage.ValidateIntegrityAsync();
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Metadata storage validation failed: {Errors}",
                    string.Join(", ", validationResult.Errors));
            }

            // Compact storage if supported
            if (_persistentMetadataStorage.SupportsCompression)
            {
                await _persistentMetadataStorage.CompactAsync();
            }

            // Update statistics
            await PersistStorageStatisticsAsync();

            Logger.LogInformation("Metadata maintenance completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during metadata maintenance");
        }
    }
}

/// <summary>
/// Metadata index entry.
/// </summary>
internal class MetadataIndex
{
    public string StorageKey { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; }
}

/// <summary>
/// Internal storage statistics for persistence.
/// </summary>
internal class InternalStorageStatistics
{
    public int TotalItems { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<string, int> ItemsByClass { get; set; } = new();
    public Dictionary<string, int> ItemsByOwner { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public int RequestCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
