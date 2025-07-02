using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Storage.Models;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Metadata and management operations for the Storage Service.
/// </summary>
public partial class StorageService
{
    /// <inheritdoc/>
    public async Task<bool> DeleteDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        try
        {
            IncrementRequestCounters();

            // Get metadata
            StorageMetadata? metadata = null;
            lock (_metadataCache)
            {
                _metadataCache.TryGetValue(key, out metadata);
            }

            if (metadata == null)
            {
                // Try to get metadata from the enclave
                try
                {
                    metadata = await GetStorageMetadataAsync(key, blockchainType);
                }
                catch
                {
                    // If metadata doesn't exist, return true (already deleted)
                    return true;
                }
            }

            // Delete chunks if necessary using real storage
            if (metadata.ChunkCount > 1)
            {
                await DeleteChunkedDataAsync(key, metadata);
            }
            else
            {
                // Delete single chunk
                await DeleteSingleChunkAsync(key);
            }

            // Remove from cache
            lock (_metadataCache)
            {
                _metadataCache.Remove(key);
            }

            RecordSuccess();
            UpdateMetric("TotalStoredBytes", _metadataCache.Values.Sum(m => m.SizeBytes));
            return true;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error deleting data with key {Key} for blockchain {BlockchainType}",
                key, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<StorageMetadata> GetStorageMetadataAsync(string key, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        try
        {
            IncrementRequestCounters();

            // Check the cache first
            lock (_metadataCache)
            {
                if (_metadataCache.TryGetValue(key, out var cachedMetadata))
                {
                    RecordSuccess();
                    return cachedMetadata;
                }
            }

            // Get metadata from the enclave
            string metadataJson = await _enclaveManager.ExecuteJavaScriptAsync($"getMetadata('{key}')");

            // Parse the result
            var metadata = JsonSerializer.Deserialize<StorageMetadata>(metadataJson) ??
                throw new InvalidOperationException("Failed to deserialize storage metadata.");

            // Update the cache
            lock (_metadataCache)
            {
                _metadataCache[key] = metadata;
            }

            RecordSuccess();
            return metadata;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error getting metadata for key {Key} for blockchain {BlockchainType}",
                key, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        try
        {
            IncrementRequestCounters();

            // Check the cache first
            lock (_metadataCache)
            {
                if (_metadataCache.ContainsKey(key))
                {
                    RecordSuccess();
                    return true;
                }
            }

            // Check in the enclave
            try
            {
                await GetStorageMetadataAsync(key, blockchainType);
                RecordSuccess();
                return true;
            }
            catch (KeyNotFoundException)
            {
                RecordSuccess();
                return false;
            }
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error checking existence of key {Key} for blockchain {BlockchainType}",
                key, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string[]> ListKeysAsync(string prefix, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            IncrementRequestCounters();

            var keys = new List<string>();

            // Get keys from cache
            lock (_metadataCache)
            {
                keys.AddRange(_metadataCache.Keys.Where(k => string.IsNullOrEmpty(prefix) || k.StartsWith(prefix)));
            }

            // Also get keys from enclave (in case cache is not complete)
            try
            {
                string keysJson = await _enclaveManager.ExecuteJavaScriptAsync($"listKeys('{prefix}')");
                var enclaveKeys = JsonSerializer.Deserialize<string[]>(keysJson);
                if (enclaveKeys != null)
                {
                    keys.AddRange(enclaveKeys.Where(k => !keys.Contains(k)));
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get keys from enclave, using cache only");
            }

            RecordSuccess();
            return keys.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error listing keys with prefix {Prefix} for blockchain {BlockchainType}",
                prefix, blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Updates metadata for a storage item.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="metadata">The updated metadata.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    public async Task<bool> UpdateMetadataAsync(string key, StorageMetadata metadata, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        try
        {
            // Update metadata in the enclave
            string metadataJson = JsonSerializer.Serialize(metadata);
            await _enclaveManager.ExecuteJavaScriptAsync($"updateMetadata('{key}', {metadataJson})");

            // Update the cache
            lock (_metadataCache)
            {
                _metadataCache[key] = metadata;
            }

            Logger.LogDebug("Updated metadata for key {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating metadata for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes chunked data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="metadata">The storage metadata.</param>
    private async Task DeleteChunkedDataAsync(string key, StorageMetadata metadata)
    {
        for (int i = 0; i < metadata.ChunkCount; i++)
        {
            string chunkKey = $"{key}.chunk{i}";
            bool deleteResult = await _enclaveManager.StorageDeleteDataAsync(chunkKey);

            // Verify deletion was successful
            if (!deleteResult)
            {
                Logger.LogWarning("Failed to delete chunk {ChunkKey}", chunkKey);
            }
        }
    }

    /// <summary>
    /// Deletes a single chunk.
    /// </summary>
    /// <param name="key">The storage key.</param>
    private async Task DeleteSingleChunkAsync(string key)
    {
        bool deleteResult = await _enclaveManager.StorageDeleteDataAsync(key);

        // Verify deletion was successful
        if (!deleteResult)
        {
            throw new InvalidOperationException($"Failed to delete data with key {key}");
        }
    }

    /// <summary>
    /// Gets storage usage information.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Storage usage information.</returns>
    public async Task<StorageUsage> GetStorageUsageAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            var usage = new StorageUsage();

            lock (_metadataCache)
            {
                usage.TotalItems = _metadataCache.Count;
                usage.TotalSizeBytes = _metadataCache.Values.Sum(m => m.SizeBytes);
                usage.CompressedItems = _metadataCache.Values.Count(m => m.IsCompressed);
                usage.EncryptedItems = _metadataCache.Values.Count(m => m.IsEncrypted);
                usage.ChunkedItems = _metadataCache.Values.Count(m => m.ChunkCount > 1);
            }

            // Get additional usage information from enclave
            try
            {
                string usageJson = await _enclaveManager.ExecuteJavaScriptAsync("getStorageUsage()");
                var enclaveUsage = JsonSerializer.Deserialize<Dictionary<string, object>>(usageJson);
                if (enclaveUsage != null)
                {
                    if (enclaveUsage.TryGetValue("available_space", out var availableSpace))
                    {
                        usage.AvailableSpaceBytes = Convert.ToInt64(availableSpace);
                    }
                    if (enclaveUsage.TryGetValue("used_space", out var usedSpace))
                    {
                        usage.UsedSpaceBytes = Convert.ToInt64(usedSpace);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get enclave storage usage information");
            }

            return usage;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting storage usage for blockchain {BlockchainType}", blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Cleans up expired storage items.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Number of items cleaned up.</returns>
    public async Task<int> CleanupExpiredItemsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            var expiredKeys = new List<string>();
            var now = DateTime.UtcNow;

            lock (_metadataCache)
            {
                expiredKeys.AddRange(_metadataCache
                    .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
                    .Select(kvp => kvp.Key));
            }

            var cleanedCount = 0;
            foreach (var key in expiredKeys)
            {
                try
                {
                    await DeleteDataAsync(key, blockchainType);
                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to delete expired item with key {Key}", key);
                }
            }

            if (cleanedCount > 0)
            {
                Logger.LogInformation("Cleaned up {CleanedCount} expired storage items", cleanedCount);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cleaning up expired items for blockchain {BlockchainType}", blockchainType);
            throw;
        }
    }
}
