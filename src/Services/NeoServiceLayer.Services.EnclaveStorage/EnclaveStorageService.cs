using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.EnclaveStorage.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.EnclaveStorage;

/// <summary>
/// Service providing secure persistent storage within SGX enclave.
/// </summary>
public class EnclaveStorageService : EnclaveBlockchainServiceBase, IEnclaveStorageService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly IPersistentStorageProvider? _persistentStorage;
    private readonly ConcurrentDictionary<string, SealedDataItem> _sealedItems = new();
    private readonly ConcurrentDictionary<string, ServiceStorageInfo> _serviceStorage = new();
    private readonly object _storageLock = new();
    private long _totalStorageUsed;
    private readonly long _maxStorageSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveStorageService"/> class.
    /// </summary>
    public EnclaveStorageService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<EnclaveStorageService> logger,
        IPersistentStorageProvider? persistentStorage = null)
        : base("EnclaveStorage", "Secure Enclave Storage Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _persistentStorage = persistentStorage;
        _maxStorageSize = configuration.GetValue("MaxStorageSize", 1073741824L); // 1GB default

        // Add capabilities
        AddCapability<IEnclaveStorageService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxStorageSize", _maxStorageSize.ToString());
        SetMetadata("SupportedOperations", "Store,Retrieve,Delete,Seal,Unseal");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {

            // Load sealed items from persistent storage
            var items = await LoadFromPersistentStorageAsync<List<SealedDataItem>>("sealed_items");
            if (items != null)
            {
                foreach (var item in items)
                {
                    _sealedItems[item.Key] = item;
                    UpdateServiceStorage(item.Service, item.Size);
                    _totalStorageUsed += item.Size;
                }
            }

            Logger.LogInformation("Enclave Storage Service initialized with {ItemCount} sealed items, {StorageUsed} bytes used",
                _sealedItems.Count, _totalStorageUsed);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize EnclaveStorage enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<SealDataResult> SealDataAsync(SealDataRequest request, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        if (string.IsNullOrEmpty(request.Key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(request));
        }

        if (request.Data == null || request.Data.Length == 0)
        {
            throw new ArgumentException("Data cannot be empty", nameof(request));
        }

        if (request.Data.Length > 1048576) // 1MB limit
        {
            throw new InvalidOperationException("Data size exceeds maximum limit of 1MB");
        }

        try
        {
            // Check storage quota
            if (_totalStorageUsed + request.Data.Length > _maxStorageSize)
            {
                throw new InvalidOperationException("Storage quota exceeded");
            }

            var storageId = $"seal_{Guid.NewGuid():N}";
            
            // Seal the data using enclave capabilities
            var sealedData = await SealDataInEnclaveAsync(request.Data, request.Policy);
            
            // Calculate fingerprint
            using var sha256 = SHA256.Create();
            var fingerprint = Convert.ToBase64String(sha256.ComputeHash(request.Data));

            var sealedItem = new SealedDataItem
            {
                Key = request.Key,
                StorageId = storageId,
                SealedData = sealedData,
                OriginalSize = request.Data.Length,
                SealedSize = sealedData.Length,
                Fingerprint = fingerprint,
                Service = request.Metadata?.GetValueOrDefault("service")?.ToString() ?? "default",
                PolicyType = request.Policy.Type,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(request.Policy.ExpirationHours),
                Metadata = request.Metadata,
                AccessCount = 0
            };

            lock (_storageLock)
            {
                _sealedItems[request.Key] = sealedItem;
                _totalStorageUsed += sealedItem.SealedSize;
                UpdateServiceStorage(sealedItem.Service, sealedItem.SealedSize);
            }

            // Persist to storage
            await SaveToPersistentStorageAsync("sealed_items", _sealedItems.Values.ToList());

            return new SealDataResult
            {
                Success = true,
                StorageId = storageId,
                SealedSize = sealedItem.SealedSize,
                Fingerprint = $"sha256:{fingerprint}",
                ExpiresAt = sealedItem.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seal data for key {Key}", request.Key);
            throw new InvalidOperationException($"Failed to seal data: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<UnsealDataResult> UnsealDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        if (!_sealedItems.TryGetValue(key, out var sealedItem))
        {
            throw new InvalidOperationException($"No sealed data found for key: {key}");
        }

        if (sealedItem.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Sealed data for key {key} has expired");
        }

        try
        {
            // Unseal the data using enclave capabilities
            var unsealedData = await UnsealDataInEnclaveAsync(sealedItem.SealedData, sealedItem.PolicyType);

            // Update access tracking
            sealedItem.LastAccessed = DateTime.UtcNow;
            sealedItem.AccessCount++;

            // Persist updated access info
            await SaveToPersistentStorageAsync("sealed_items", _sealedItems.Values.ToList());

            return new UnsealDataResult
            {
                Success = true,
                Data = unsealedData,
                Metadata = sealedItem.Metadata,
                Sealed = true,
                LastAccessed = sealedItem.LastAccessed
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unseal data for key {Key}", key);
            throw new InvalidOperationException($"Failed to unseal data: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<SealedItemsList> ListSealedItemsAsync(ListSealedItemsRequest request, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        await Task.CompletedTask;

        var query = _sealedItems.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Service))
        {
            query = query.Where(i => i.Service == request.Service);
        }

        if (!string.IsNullOrEmpty(request.Prefix))
        {
            query = query.Where(i => i.Key.StartsWith(request.Prefix));
        }

        var totalItems = query.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

        var items = query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new SealedItem
            {
                Key = i.Key,
                Size = i.SealedSize,
                Created = i.CreatedAt,
                LastAccessed = i.LastAccessed,
                ExpiresAt = i.ExpiresAt,
                Service = i.Service,
                PolicyType = i.PolicyType
            })
            .ToList();

        return new SealedItemsList
        {
            Items = items,
            TotalSize = query.Sum(i => i.SealedSize),
            ItemCount = totalItems,
            Page = request.Page,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc/>
    public async Task<DeleteSealedDataResult> DeleteSealedDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        if (!_sealedItems.TryRemove(key, out var sealedItem))
        {
            return new DeleteSealedDataResult
            {
                Success = false,
                Deleted = false,
                Shredded = false,
                Timestamp = DateTime.UtcNow
            };
        }

        lock (_storageLock)
        {
            _totalStorageUsed -= sealedItem.SealedSize;
            UpdateServiceStorage(sealedItem.Service, -sealedItem.SealedSize);
        }

        // Securely overwrite the data
        SecureDelete(sealedItem.SealedData);

        // Persist updated list
        await SaveToPersistentStorageAsync("sealed_items", _sealedItems.Values.ToList());

        return new DeleteSealedDataResult
        {
            Success = true,
            Deleted = true,
            Shredded = true,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<EnclaveStorageStatistics> GetStorageStatisticsAsync(BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        await Task.CompletedTask;

        return new EnclaveStorageStatistics
        {
            TotalItems = _sealedItems.Count,
            TotalSize = _totalStorageUsed,
            AvailableSpace = _maxStorageSize - _totalStorageUsed,
            ServiceCount = _serviceStorage.Count,
            ServiceStorage = _serviceStorage.ToDictionary(
                kvp => kvp.Key,
                kvp => new ServiceStorageInfo
                {
                    ServiceName = kvp.Key,
                    ItemCount = kvp.Value.ItemCount,
                    TotalSize = kvp.Value.TotalSize,
                    QuotaUsedPercent = (kvp.Value.TotalSize * 100.0) / _maxStorageSize
                }
            ),
            LastBackup = await GetLastBackupTimeAsync()
        };
    }

    /// <inheritdoc/>
    public async Task<BackupResult> BackupSealedDataAsync(BackupRequest request, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            var backupId = $"backup_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            var itemsToBackup = _sealedItems.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(request.ServiceFilter))
            {
                itemsToBackup = itemsToBackup.Where(i => i.Service == request.ServiceFilter);
            }

            var backedUpItems = 0;
            var totalSize = 0L;

            foreach (var item in itemsToBackup)
            {
                // Re-seal with backup key
                var backupData = await ResealForBackupAsync(item);
                
                // Save to backup location
                await SaveToPersistentStorageAsync($"{request.BackupLocation}/{backupId}/{item.Key}", backupData);
                
                backedUpItems++;
                totalSize += item.SealedSize;
            }

            // Save backup metadata
            await SaveToPersistentStorageAsync($"{request.BackupLocation}/{backupId}/metadata", new
            {
                BackupId = backupId,
                Timestamp = DateTime.UtcNow,
                ItemCount = backedUpItems,
                TotalSize = totalSize,
                ServiceFilter = request.ServiceFilter
            });

            return new BackupResult
            {
                Success = true,
                ItemsBackedUp = backedUpItems,
                TotalSize = totalSize,
                BackupId = backupId,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to backup sealed data");
            throw new InvalidOperationException($"Backup failed: {ex.Message}", ex);
        }
    }

    private async Task<byte[]> SealDataInEnclaveAsync(byte[] data, SealingPolicy policy)
    {
        // In a real implementation, this would use SGX sealing APIs
        // For now, simulate sealing with encryption
        await Task.CompletedTask;
        
        using (var aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();
            
            // Encrypt the data
            byte[] encrypted;
            using (var encryptor = aes.CreateEncryptor())
            {
                encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            }
            
            // Calculate total size
            int ivLength = aes.IV.Length;
            int totalLength = 1 + ivLength + encrypted.Length;
            
            // Create sealed data array
            byte[] sealedData = new byte[totalLength];
            
            // Store policy type as first byte
            sealedData[0] = (byte)policy.Type;
            
            // Copy IV
            Array.Copy(aes.IV, 0, sealedData, 1, ivLength);
            
            // Copy encrypted data
            Array.Copy(encrypted, 0, sealedData, 1 + ivLength, encrypted.Length);
            
            return sealedData;
        }
    }

    private async Task<byte[]> UnsealDataInEnclaveAsync(byte[] sealedData, SealingPolicyType policyType)
    {
        // In a real implementation, this would use SGX unsealing APIs
        await Task.CompletedTask;
        
        byte policyTypeByte = (byte)policyType;
        if (sealedData[0] != policyTypeByte)
        {
            throw new InvalidOperationException("Policy type mismatch");
        }
        
        var iv = new byte[16];
        Buffer.BlockCopy(sealedData, 1, iv, 0, iv.Length);
        
        var encrypted = new byte[sealedData.Length - 17];
        Buffer.BlockCopy(sealedData, 17, encrypted, 0, encrypted.Length);
        
        using (var aes = Aes.Create())
        {
            aes.IV = iv;
            // In production, key would be derived from enclave sealing key
            aes.GenerateKey();
            
            byte[] decrypted;
            using (var decryptor = aes.CreateDecryptor())
            {
                decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            }
            return decrypted;
        }
    }

    private async Task<byte[]> ResealForBackupAsync(SealedDataItem item)
    {
        // Unseal with current key and reseal with backup key
        var data = await UnsealDataInEnclaveAsync(item.SealedData, item.PolicyType);
        return await SealDataInEnclaveAsync(data, new SealingPolicy { Type = item.PolicyType });
    }

    private void UpdateServiceStorage(string service, long sizeChange)
    {
        _serviceStorage.AddOrUpdate(service,
            new ServiceStorageInfo 
            { 
                ServiceName = service, 
                ItemCount = sizeChange > 0 ? 1 : 0, 
                TotalSize = Math.Max(0, sizeChange) 
            },
            (key, existing) =>
            {
                existing.ItemCount += sizeChange > 0 ? 1 : -1;
                existing.TotalSize += sizeChange;
                return existing;
            });
    }

    private void SecureDelete(byte[] data)
    {
        // Overwrite with random data
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(data);
        Array.Clear(data, 0, data.Length);
    }

    private async Task<DateTime?> GetLastBackupTimeAsync()
    {
        var metadata = await LoadFromPersistentStorageAsync<dynamic>("last_backup");
        return metadata?.Timestamp as DateTime?;
    }


    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        await Task.CompletedTask;
        return ServiceHealth.Healthy;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        await Task.CompletedTask;
        return true;
    }

    private void ValidateBlockchainType(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }
    }

    private class SealedDataItem
    {
        public string Key { get; set; } = string.Empty;
        public string StorageId { get; set; } = string.Empty;
        public byte[] SealedData { get; set; } = Array.Empty<byte>();
        public int OriginalSize { get; set; }
        public int SealedSize { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public SealingPolicyType PolicyType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public int AccessCount { get; set; }
        public int Size => SealedSize;
    }

    private async Task<T?> LoadFromPersistentStorageAsync<T>(string key) where T : class
    {
        if (_persistentStorage == null) return null;
        
        try
        {
            return await _persistentStorage.RetrieveObjectAsync<T>(key, Logger, $"Loading {key}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load {Key} from persistent storage", key);
            return null;
        }
    }

    private async Task SaveToPersistentStorageAsync<T>(string key, T obj)
    {
        if (_persistentStorage == null) return;
        
        try
        {
            await _persistentStorage.StoreObjectAsync(key, obj, new StorageOptions
            {
                Encrypt = true,
                Compress = true
            }, Logger, $"Saving {key}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save {Key} to persistent storage", key);
        }
    }
}