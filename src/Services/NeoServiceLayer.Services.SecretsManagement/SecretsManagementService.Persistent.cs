using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text.Json;


namespace NeoServiceLayer.Services.SecretsManagement;

/// <summary>
/// Persistent storage extensions for SecretsManagementService.
/// </summary>
public partial class SecretsManagementService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string SECRET_METADATA_PREFIX = "secrets:metadata:";
    private const string SECRET_INDEX_PREFIX = "secrets:index:";
    private const string SECRET_AUDIT_PREFIX = "secrets:audit:";
    private const string SECRET_STATS_KEY = "secrets:statistics";

    /// <summary>
    /// Loads persistent secret metadata from storage.
    /// </summary>
    private async Task LoadPersistentSecretsAsync()
    {
        if (_persistentStorage == null)
        {
            _persistentStorageUnavailable(Logger, null);
            return;
        }

        try
        {
            _loadingPersistentSecrets(Logger, null);

            // Load secret metadata
            var metadataKeys = await _persistentStorage.ListKeysAsync(SECRET_METADATA_PREFIX);
            foreach (var key in metadataKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var metadata = JsonSerializer.Deserialize<SecretMetadata>(data);
                    if (metadata != null && !metadata.IsDeleted)
                    {
                        _secretCache[metadata.SecretId] = metadata;
                    }
                }
            }
            _persistentSecretsLoaded(Logger, _secretCache.Count, null);

            // Load statistics
            var statsData = await _persistentStorage.RetrieveAsync(SECRET_STATS_KEY);
            if (statsData != null)
            {
                var stats = JsonSerializer.Deserialize<SecretStatistics>(statsData);
                if (stats != null)
                {
                    _totalSecretsCreated = stats.TotalCreated;
                    _totalSecretsAccessed = stats.TotalAccessed;
                    _totalSecretsRotated = stats.TotalRotated;
                }
            }
        }
        catch (Exception ex)
        {
            _persistentSecretsLoadError(Logger, ex);
        }
    }

    /// <summary>
    /// Persists secret metadata to storage.
    /// </summary>
    private async Task PersistSecretMetadataAsync(SecretMetadata metadata)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SECRET_METADATA_PREFIX}{metadata.SecretId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metadata);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "SecretMetadata",
                    ["SecretId"] = metadata.SecretId,
                    ["Name"] = metadata.Name,
                    ["CreatedAt"] = metadata.CreatedAt.ToString("O"),
                    ["Version"] = metadata.Version.ToString()
                }
            });

            // Update indexes
            await UpdateSecretIndexesAsync(metadata);

            // Audit log
            await PersistAuditLogAsync(new SecretAuditEntry
            {
                SecretId = metadata.SecretId,
                Action = "MetadataUpdated",
                Timestamp = DateTime.UtcNow,
                Version = metadata.Version
            });
        }
        catch (Exception ex)
        {
            _secretMetadataPersistError(Logger, metadata.SecretId, ex);
        }
    }

    /// <summary>
    /// Removes secret metadata from persistent storage.
    /// </summary>
    private async Task RemovePersistedSecretMetadataAsync(string secretId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SECRET_METADATA_PREFIX}{secretId}";

            // Mark as deleted instead of removing (for audit trail)
            var data = await _persistentStorage.RetrieveAsync(key);
            if (data != null)
            {
                var metadata = JsonSerializer.Deserialize<SecretMetadata>(data);
                if (metadata != null)
                {
                    metadata.IsDeleted = true;
                    metadata.DeletedAt = DateTime.UtcNow;
                    await PersistSecretMetadataAsync(metadata);
                }
            }

            // Remove from indexes
            await RemoveFromSecretIndexesAsync(secretId);

            // Audit log
            await PersistAuditLogAsync(new SecretAuditEntry
            {
                SecretId = secretId,
                Action = "Deleted",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _secretMetadataRemovalError(Logger, secretId, ex);
        }
    }

    /// <summary>
    /// Updates secret indexes for efficient queries.
    /// </summary>
    private async Task UpdateSecretIndexesAsync(SecretMetadata metadata)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Index by name
            var nameIndexKey = $"{SECRET_INDEX_PREFIX}name:{metadata.Name}";
            var indexData = JsonSerializer.SerializeToUtf8Bytes(new SecretIndex
            {
                SecretId = metadata.SecretId,
                IndexedAt = DateTime.UtcNow
            });

            await _persistentStorage.StoreAsync(nameIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by type
            var typeIndexKey = $"{SECRET_INDEX_PREFIX}type:{metadata.Type}:{metadata.SecretId}";
            await _persistentStorage.StoreAsync(typeIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });

            // Index by creation date
            var dateIndexKey = $"{SECRET_INDEX_PREFIX}date:{metadata.CreatedAt:yyyyMMdd}:{metadata.SecretId}";
            await _persistentStorage.StoreAsync(dateIndexKey, indexData, new StorageOptions
            {
                Encrypt = false,
                Compress = false
            });
        }
        catch (Exception ex)
        {
            _secretIndexUpdateError(Logger, metadata.SecretId, ex);
        }
    }

    /// <summary>
    /// Removes secret from all indexes.
    /// </summary>
    private async Task RemoveFromSecretIndexesAsync(string secretId)
    {
        if (_persistentStorage == null) return;

        try
        {
            var indexKeys = await _persistentStorage.ListKeysAsync(SECRET_INDEX_PREFIX);
            var keysToDelete = indexKeys.Where(k => k.Contains($":{secretId}") || k.EndsWith(secretId)).ToList();

            foreach (var key in keysToDelete)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _secretIndexRemovalError(Logger, secretId, ex);
        }
    }

    /// <summary>
    /// Persists audit log entry.
    /// </summary>
    private async Task PersistAuditLogAsync(SecretAuditEntry entry)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SECRET_AUDIT_PREFIX}{entry.Timestamp.Ticks}:{entry.SecretId}:{Guid.NewGuid()}";
            var data = JsonSerializer.SerializeToUtf8Bytes(entry);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(365), // Keep audit logs for 1 year
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "AuditLog",
                    ["SecretId"] = entry.SecretId,
                    ["Action"] = entry.Action,
                    ["Timestamp"] = entry.Timestamp.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            _auditLogPersistError(Logger, entry.SecretId, ex);
        }
    }

    /// <summary>
    /// Retrieves audit history for a secret.
    /// </summary>
    private async Task<List<SecretAuditEntry>> GetSecretAuditHistoryAsync(string secretId)
    {
        if (_persistentStorage == null) return new List<SecretAuditEntry>();

        var auditEntries = new List<SecretAuditEntry>();

        try
        {
            var auditKeys = await _persistentStorage.ListKeysAsync(SECRET_AUDIT_PREFIX);
            var secretAuditKeys = auditKeys.Where(k => k.Contains($":{secretId}:")).ToList();

            foreach (var key in secretAuditKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var entry = JsonSerializer.Deserialize<SecretAuditEntry>(data);
                    if (entry != null)
                    {
                        auditEntries.Add(entry);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _auditHistoryRetrievalError(Logger, secretId, ex);
        }

        return auditEntries.OrderByDescending(e => e.Timestamp).ToList();
    }

    /// <summary>
    /// Persists service statistics.
    /// </summary>
    private async Task PersistStatisticsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var stats = new SecretStatistics
            {
                TotalCreated = _totalSecretsCreated,
                TotalAccessed = _totalSecretsAccessed,
                TotalRotated = _totalSecretsRotated,
                ActiveSecrets = _secretCache.Count(kvp => !kvp.Value.IsDeleted),
                SecretsByType = _secretCache.Values
                    .Where(s => !s.IsDeleted)
                    .GroupBy(s => s.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LastUpdated = DateTime.UtcNow
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(stats);

            await _persistentStorage.StoreAsync(SECRET_STATS_KEY, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                Metadata = new Dictionary<string, object>
                {
                    ["Type"] = "Statistics",
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            _statisticsPersistError(Logger, ex);
        }
    }

    /// <summary>
    /// Performs periodic cleanup of old data.
    /// </summary>
    private async Task CleanupOldSecretsDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Clean up deleted secrets older than 30 days
            var metadataKeys = await _persistentStorage.ListKeysAsync(SECRET_METADATA_PREFIX);

            foreach (var key in metadataKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var metadata = JsonSerializer.Deserialize<SecretMetadata>(data);
                    if (metadata != null && metadata.IsDeleted &&
                        metadata.DeletedAt.HasValue &&
                        DateTime.UtcNow - metadata.DeletedAt.Value > TimeSpan.FromDays(30))
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            _cleanupCompleted(Logger, null);
        }
        catch (Exception ex)
        {
            _cleanupError(Logger, ex);
        }
    }
}

/// <summary>
/// Secret index entry.
/// </summary>
internal class SecretIndex
{
    public string SecretId { get; set; } = string.Empty;
    public DateTime IndexedAt { get; set; }
}

/// <summary>
/// Secret audit entry.
/// </summary>
internal class SecretAuditEntry
{
    public string SecretId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? Version { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

/// <summary>
/// Secret statistics.
/// </summary>
internal class SecretStatistics
{
    public int TotalCreated { get; set; }
    public int TotalAccessed { get; set; }
    public int TotalRotated { get; set; }
    public int ActiveSecrets { get; set; }
    public Dictionary<string, int> SecretsByType { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
