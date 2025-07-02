using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.KeyManagement;

/// <summary>
/// Persistent storage implementation for KeyManagementService.
/// </summary>
public partial class KeyManagementService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private Timer? _persistenceTimer;
    private Timer? _cleanupTimer;
    
    // Storage key prefixes
    private const string KEY_METADATA_PREFIX = "key:metadata:";
    private const string KEY_USAGE_PREFIX = "key:usage:";
    private const string KEY_AUDIT_PREFIX = "key:audit:";
    private const string KEY_INDEX_PREFIX = "key:index:";
    private const string KEY_STATS_PREFIX = "key:stats:";
    
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyManagementService"/> class with persistent storage.
    /// </summary>
    public KeyManagementService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<KeyManagementService> logger,
        IPersistentStorageProvider? persistentStorage)
        : this(enclaveManager, configuration, logger)
    {
        _persistentStorage = persistentStorage;
        
        if (_persistentStorage != null)
        {
            // Initialize persistence timer to save cache periodically
            _persistenceTimer = new Timer(
                async _ => await PersistCacheAsync(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
            
            // Initialize cleanup timer for old audit logs
            _cleanupTimer = new Timer(
                async _ => await CleanupExpiredDataAsync(),
                null,
                TimeSpan.FromHours(24),
                TimeSpan.FromHours(24));
        }
    }
    
    /// <summary>
    /// Loads persistent key metadata on service initialization.
    /// </summary>
    private async Task LoadPersistentKeysAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogDebug("No persistent storage configured for KeyManagementService");
            return;
        }
        
        try
        {
            Logger.LogInformation("Loading persistent key metadata");
            
            // Load all key metadata
            var pattern = $"{KEY_METADATA_PREFIX}*";
            var keys = await _persistentStorage.ListKeysAsync(pattern);
            
            int loadedCount = 0;
            foreach (var key in keys)
            {
                try
                {
                    var data = await _persistentStorage.RetrieveAsync(key);
                    if (data != null)
                    {
                        var metadata = JsonSerializer.Deserialize<KeyMetadata>(data);
                        if (metadata != null)
                        {
                            var keyId = key.Substring(KEY_METADATA_PREFIX.Length);
                            lock (_keyCache)
                            {
                                _keyCache[keyId] = metadata;
                            }
                            loadedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load key metadata from {Key}", key);
                }
            }
            
            Logger.LogInformation("Loaded {Count} key metadata entries from persistent storage", loadedCount);
            
            // Load service statistics
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent key metadata");
        }
    }
    
    /// <summary>
    /// Persists a key metadata entry.
    /// </summary>
    private async Task PersistKeyMetadataAsync(KeyMetadata metadata)
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var key = $"{KEY_METADATA_PREFIX}{metadata.KeyId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metadata);
            
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = false,
                TimeToLive = null // Key metadata should not expire
            });
            
            // Update index for key type
            await UpdateKeyTypeIndexAsync(metadata.KeyType, metadata.KeyId);
            
            // Update usage index
            await UpdateKeyUsageIndexAsync(metadata.KeyUsage, metadata.KeyId);
            
            Logger.LogDebug("Persisted key metadata for {KeyId}", metadata.KeyId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist key metadata for {KeyId}", metadata.KeyId);
        }
    }
    
    /// <summary>
    /// Records key usage audit log.
    /// </summary>
    private async Task RecordKeyUsageAsync(string keyId, string operation, string details = "")
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var auditEntry = new
            {
                KeyId = keyId,
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Details = details,
                RequestId = Guid.NewGuid().ToString()
            };
            
            var key = $"{KEY_AUDIT_PREFIX}{keyId}:{auditEntry.Timestamp:yyyyMMddHHmmss}:{auditEntry.RequestId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(auditEntry);
            
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90) // Keep audit logs for 90 days
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record key usage audit for {KeyId}", keyId);
        }
    }
    
    /// <summary>
    /// Updates key type index.
    /// </summary>
    private async Task UpdateKeyTypeIndexAsync(string keyType, string keyId)
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var key = $"{KEY_INDEX_PREFIX}type:{keyType}";
            var existingData = await _persistentStorage.RetrieveAsync(key);
            
            HashSet<string> keyIds;
            if (existingData != null)
            {
                keyIds = JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>();
            }
            else
            {
                keyIds = new HashSet<string>();
            }
            
            keyIds.Add(keyId);
            
            var data = JsonSerializer.SerializeToUtf8Bytes(keyIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update key type index for {KeyType}", keyType);
        }
    }
    
    /// <summary>
    /// Updates key usage index.
    /// </summary>
    private async Task UpdateKeyUsageIndexAsync(string keyUsage, string keyId)
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var key = $"{KEY_INDEX_PREFIX}usage:{keyUsage}";
            var existingData = await _persistentStorage.RetrieveAsync(key);
            
            HashSet<string> keyIds;
            if (existingData != null)
            {
                keyIds = JsonSerializer.Deserialize<HashSet<string>>(existingData) ?? new HashSet<string>();
            }
            else
            {
                keyIds = new HashSet<string>();
            }
            
            keyIds.Add(keyId);
            
            var data = JsonSerializer.SerializeToUtf8Bytes(keyIds);
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update key usage index for {KeyUsage}", keyUsage);
        }
    }
    
    /// <summary>
    /// Removes key from all indexes.
    /// </summary>
    private async Task RemoveKeyFromIndexesAsync(string keyId, KeyMetadata metadata)
    {
        if (_persistentStorage == null) return;
        
        try
        {
            // Remove from type index
            var typeKey = $"{KEY_INDEX_PREFIX}type:{metadata.KeyType}";
            var typeData = await _persistentStorage.RetrieveAsync(typeKey);
            if (typeData != null)
            {
                var keyIds = JsonSerializer.Deserialize<HashSet<string>>(typeData) ?? new HashSet<string>();
                if (keyIds.Remove(keyId))
                {
                    var updatedData = JsonSerializer.SerializeToUtf8Bytes(keyIds);
                    await _persistentStorage.StoreAsync(typeKey, updatedData, new StorageOptions
                    {
                        Encrypt = false,
                        Compress = true
                    });
                }
            }
            
            // Remove from usage index
            var usageKey = $"{KEY_INDEX_PREFIX}usage:{metadata.KeyUsage}";
            var usageData = await _persistentStorage.RetrieveAsync(usageKey);
            if (usageData != null)
            {
                var keyIds = JsonSerializer.Deserialize<HashSet<string>>(usageData) ?? new HashSet<string>();
                if (keyIds.Remove(keyId))
                {
                    var updatedData = JsonSerializer.SerializeToUtf8Bytes(keyIds);
                    await _persistentStorage.StoreAsync(usageKey, updatedData, new StorageOptions
                    {
                        Encrypt = false,
                        Compress = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to remove key {KeyId} from indexes", keyId);
        }
    }
    
    /// <summary>
    /// Persists the entire key cache.
    /// </summary>
    private async Task PersistCacheAsync()
    {
        if (_persistentStorage == null) return;
        
        try
        {
            List<KeyMetadata> keysToSave;
            lock (_keyCache)
            {
                keysToSave = _keyCache.Values.ToList();
            }
            
            int savedCount = 0;
            foreach (var metadata in keysToSave)
            {
                await PersistKeyMetadataAsync(metadata);
                savedCount++;
            }
            
            Logger.LogDebug("Persisted {Count} key metadata entries", savedCount);
            
            // Persist service statistics
            await PersistStatisticsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting key cache");
        }
    }
    
    /// <summary>
    /// Persists service statistics.
    /// </summary>
    private async Task PersistStatisticsAsync()
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var stats = new
            {
                RequestCount = _requestCount,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                LastRequestTime = _lastRequestTime,
                TotalKeys = _keyCache.Count,
                Timestamp = DateTime.UtcNow
            };
            
            var key = $"{KEY_STATS_PREFIX}current";
            var data = JsonSerializer.SerializeToUtf8Bytes(stats);
            
            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
            
            // Also store historical stats
            var historyKey = $"{KEY_STATS_PREFIX}history:{stats.Timestamp:yyyyMMddHHmmss}";
            await _persistentStorage.StoreAsync(historyKey, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(30) // Keep history for 30 days
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to persist service statistics");
        }
    }
    
    /// <summary>
    /// Loads service statistics from persistent storage.
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        if (_persistentStorage == null) return;
        
        try
        {
            var key = $"{KEY_STATS_PREFIX}current";
            var data = await _persistentStorage.RetrieveAsync(key);
            
            if (data != null)
            {
                var stats = JsonSerializer.Deserialize<dynamic>(data);
                if (stats != null)
                {
                    _requestCount = stats.RequestCount ?? 0;
                    _successCount = stats.SuccessCount ?? 0;
                    _failureCount = stats.FailureCount ?? 0;
                    _lastRequestTime = stats.LastRequestTime ?? DateTime.MinValue;
                    
                    Logger.LogInformation("Loaded service statistics: Requests={Requests}, Success={Success}, Failures={Failures}",
                        _requestCount, _successCount, _failureCount);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load service statistics");
        }
    }
    
    /// <summary>
    /// Cleans up expired data including old audit logs and expired keys.
    /// </summary>
    private async Task CleanupExpiredDataAsync()
    {
        if (_persistentStorage == null) return;
        
        try
        {
            Logger.LogDebug("Starting cleanup of expired data");
            
            // Clean up old audit logs (90 days)
            var auditDeletedCount = await _persistentStorage.CleanupExpiredKeysAsync(
                $"{KEY_AUDIT_PREFIX}*",
                TimeSpan.FromDays(90),
                StorageKeyPatterns.ExtractTimestamp,
                Logger);
            
            // Clean up old statistics (30 days)
            var statsDeletedCount = await _persistentStorage.CleanupExpiredKeysAsync(
                $"{KEY_STATS_PREFIX}history:*",
                TimeSpan.FromDays(30),
                StorageKeyPatterns.ExtractTimestamp,
                Logger);
            
            // Validate storage integrity
            await _persistentStorage.ValidateStorageAsync(Logger);
            
            // Compact storage if many items were deleted
            if (auditDeletedCount + statsDeletedCount > 100)
            {
                await _persistentStorage.CompactServiceStorageAsync(Logger);
            }
            
            Logger.LogInformation("Cleanup completed: {AuditCount} audit logs, {StatsCount} statistics entries",
                auditDeletedCount, statsDeletedCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during cleanup of expired data");
        }
    }
    
    
    /// <summary>
    /// Disposes persistence resources.
    /// </summary>
    private void DisposePersistenceResources()
    {
        _persistenceTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _persistentStorage?.Dispose();
    }
    
    /// <summary>
    /// Creates a backup of all key metadata and audit logs.
    /// </summary>
    public async Task<bool> BackupKeyDataAsync(string backupPath)
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("No persistent storage configured, cannot create backup");
            return false;
        }
        
        return await _persistentStorage.BackupServiceDataAsync("key:", backupPath, Logger);
    }
    
    /// <summary>
    /// Restores key data from backup.
    /// </summary>
    public async Task<bool> RestoreKeyDataAsync(string backupPath)
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("No persistent storage configured, cannot restore backup");
            return false;
        }
        
        var success = await _persistentStorage.RestoreServiceDataAsync(backupPath, Logger);
        
        if (success)
        {
            // Reload keys after restore
            await LoadPersistentKeysAsync();
        }
        
        return success;
    }
    
    /// <summary>
    /// Validates the integrity of stored key data.
    /// </summary>
    public async Task<bool> ValidateKeyStorageIntegrityAsync()
    {
        if (_persistentStorage == null)
        {
            return true; // No storage to validate
        }
        
        var result = await _persistentStorage.ValidateStorageAsync(Logger);
        return result.IsValid;
    }
    
    /// <summary>
    /// Gets storage statistics for key management data.
    /// </summary>
    public async Task<StorageStatistics> GetKeyStorageStatisticsAsync()
    {
        if (_persistentStorage == null)
        {
            return new StorageStatistics();
        }
        
        return await _persistentStorage.GetStorageStatisticsAsync("key:", Logger);
    }
}