using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework.Interfaces;
using NeoServiceLayer.ServiceFramework.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for services that need SGX-based privacy-preserving storage.
/// </summary>
public abstract class SGXPersistenceBase
{
    private readonly IEnclaveStorageService? _enclaveStorage;
    private readonly ILogger _logger;
    private readonly string _serviceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SGXPersistenceBase"/> class.
    /// </summary>
    /// <param name="serviceName">The service name for storage isolation.</param>
    /// <param name="enclaveStorage">The enclave storage service.</param>
    /// <param name="logger">The logger.</param>
    protected SGXPersistenceBase(string serviceName, IEnclaveStorageService? enclaveStorage, ILogger logger)
    {
        _serviceName = serviceName;
        _enclaveStorage = enclaveStorage;
        _logger = logger;
    }

    /// <summary>
    /// Stores data securely in SGX enclave storage.
    /// </summary>
    /// <typeparam name="T">The type of data to store.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if stored successfully.</returns>
    protected async Task<bool> StoreSecurelyAsync<T>(string key, T data, Dictionary<string, object>? metadata, BlockchainType blockchainType)
    {
        if (_enclaveStorage == null)
        {
            _logger.LogWarning("Enclave storage not available, falling back to in-memory storage");
            return false;
        }

        try
        {
            var fullKey = $"{_serviceName}:{key}";
            var jsonData = JsonSerializer.Serialize(data);
            var dataBytes = Encoding.UTF8.GetBytes(jsonData);

            var request = new SealDataRequest
            {
                Key = fullKey,
                Data = dataBytes,
                Metadata = metadata ?? new Dictionary<string, object>
                {
                    ["service"] = _serviceName,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ["dataType"] = typeof(T).Name
                },
                Policy = new SealingPolicy
                {
                    Type = SealingPolicyType.MrSigner,
                    ExpirationHours = 8760 // 1 year
                }
            };

            var sealedData = await _enclaveStorage.SealDataAsync(request, CancellationToken.None);

            if (sealedData != null && sealedData.Length > 0)
            {
                _logger.LogDebug("Securely stored data with key {Key}", fullKey);
            }

            return sealedData != null && sealedData.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store data securely for key {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Retrieves data securely from SGX enclave storage.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The retrieved data or default value.</returns>
    protected async Task<T?> RetrieveSecurelyAsync<T>(string key, BlockchainType blockchainType)
    {
        if (_enclaveStorage == null)
        {
            _logger.LogWarning("Enclave storage not available, cannot retrieve data");
            return default;
        }

        try
        {
            var fullKey = $"{_serviceName}:{key}";
            var unsealedData = await _enclaveStorage.UnsealDataAsync(fullKey, CancellationToken.None);

            if (unsealedData == null || unsealedData.Length == 0)
            {
                _logger.LogDebug("No data found for key {Key}", fullKey);
                return default;
            }

            var jsonData = Encoding.UTF8.GetString(unsealedData);
            var data = JsonSerializer.Deserialize<T>(jsonData);

            _logger.LogDebug("Successfully retrieved data for key {Key}", fullKey);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data securely for key {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Deletes data securely from SGX enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if deleted successfully.</returns>
    protected async Task<bool> DeleteSecurelyAsync(string key, BlockchainType blockchainType)
    {
        if (_enclaveStorage == null)
        {
            _logger.LogWarning("Enclave storage not available, cannot delete data");
            return false;
        }

        try
        {
            var fullKey = $"{_serviceName}:{key}";
            var deleted = await _enclaveStorage.DeleteSealedDataAsync(fullKey, CancellationToken.None);

            if (deleted)
            {
                _logger.LogDebug("Successfully deleted data for key {Key}", fullKey);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete data securely for key {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Lists all stored items for this service.
    /// </summary>
    /// <param name="prefix">Optional key prefix filter.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of sealed items.</returns>
    protected async Task<SealedItemsList?> ListStoredItemsAsync(string? prefix, BlockchainType blockchainType)
    {
        if (_enclaveStorage == null)
        {
            _logger.LogWarning("Enclave storage not available, cannot list items");
            return null;
        }

        try
        {
            var request = new ListSealedItemsRequest
            {
                Prefix = string.IsNullOrEmpty(prefix) ? null : $"{_serviceName}:{prefix}",
                MaxItems = 1000
            };

            var result = await _enclaveStorage.ListSealedItemsAsync(request, CancellationToken.None);

            _logger.LogDebug("Listed {Count} sealed items for service {Service}",
                result.Items.Count, _serviceName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list stored items for service {Service}", _serviceName);
            return null;
        }
    }

    /// <summary>
    /// Stores multiple items in a batch operation.
    /// </summary>
    /// <typeparam name="T">The type of data to store.</typeparam>
    /// <param name="items">The items to store with their keys.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Dictionary of keys and their storage success status.</returns>
    protected async Task<Dictionary<string, bool>> StoreBatchSecurelyAsync<T>(
        Dictionary<string, T> items,
        BlockchainType blockchainType)
    {
        var results = new Dictionary<string, bool>();

        foreach (var (key, data) in items)
        {
            var success = await StoreSecurelyAsync(key, data, null, blockchainType);
            results[key] = success;
        }

        var successCount = results.Count(r => r.Value);
        _logger.LogInformation("Batch storage completed: {Success}/{Total} items stored successfully",
            successCount, items.Count);

        return results;
    }

    /// <summary>
    /// Creates a backup of all service data.
    /// </summary>
    /// <param name="backupLocation">The backup location.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The backup result.</returns>
    protected async Task<BackupResult?> BackupServiceDataAsync(string backupLocation, BlockchainType blockchainType)
    {
        // Backup functionality not available in minimal interface
        _logger.LogWarning("Backup functionality not available in minimal IEnclaveStorageService interface");
        await Task.CompletedTask;
        return new BackupResult
        {
            Success = false,
            Message = "Backup not available in minimal interface"
        };
    }

    /// <summary>
    /// Gets storage statistics for this service.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The service storage information.</returns>
    protected async Task<ServiceStorageInfo?> GetServiceStorageInfoAsync(BlockchainType blockchainType)
    {
        // Storage statistics functionality not available in minimal interface
        _logger.LogWarning("Storage statistics functionality not available in minimal IEnclaveStorageService interface");
        await Task.CompletedTask;
        return new ServiceStorageInfo
        {
            ServiceName = _serviceName,
            ItemCount = 0,
            StorageUsed = 0
        };
    }
}