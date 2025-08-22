using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
// using NeoServiceLayer.Services.EnclaveStorage;
// using NeoServiceLayer.Services.EnclaveStorage.Models; // TODO: Add project reference when circular dependency resolved
using NeoServiceLayer.Core.Domain;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Production implementation of confidential storage service
    /// Integrates with existing SGX EnclaveStorageService for secure data persistence
    /// </summary>
    public class ConfidentialStorageService : IConfidentialStorageService
    {
        // private readonly IEnclaveStorageService _enclaveStorageService; // TODO: Restore when circular dependency resolved
        private readonly ILogger<ConfidentialStorageService> _logger;
        private readonly Dictionary<string, ConfidentialStorageTransaction> _activeTransactions;
        private readonly object _transactionsLock = new();

        public ConfidentialStorageService(
            // IEnclaveStorageService enclaveStorageService, // TODO: Restore when circular dependency resolved
            ILogger<ConfidentialStorageService> logger)
        {
            // _enclaveStorageService = enclaveStorageService ?? throw new ArgumentNullException(nameof(enclaveStorageService)); // TODO: Restore
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeTransactions = new Dictionary<string, ConfidentialStorageTransaction>();
        }

        public async Task<ConfidentialStorageResult> StoreAsync<T>(
            string key,
            T data,
            ConfidentialStorageOptions? storageOptions = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            storageOptions ??= new ConfidentialStorageOptions();

            _logger.LogDebug("Storing confidential data for key {Key}", key);

            try
            {
                // Serialize data to JSON
                var jsonData = JsonSerializer.Serialize(data);
                var dataBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                var originalSize = dataBytes.Length;

                // Create sealing request
                var sealingRequest = new SealDataRequest
                {
                    Key = key,
                    Data = dataBytes,
                    Metadata = new Dictionary<string, object>(storageOptions.Metadata)
                    {
                        ["originalSize"] = originalSize,
                        ["dataType"] = typeof(T).FullName ?? "unknown",
                        ["compressionEnabled"] = storageOptions.EnableCompression,
                        ["deduplicationEnabled"] = storageOptions.EnableDeduplication,
                        ["createdAt"] = DateTime.UtcNow.ToString("O"),
                        ["expiresAt"] = storageOptions.ExpiresAt?.ToString("O") ?? ""
                    },
                    Policy = new SealingPolicy
                    {
                        Type = MapSealingPolicy(storageOptions.SealingPolicy),
                        ExpirationHours = storageOptions.ExpiresAt.HasValue
                            ? (int)(storageOptions.ExpiresAt.Value - DateTime.UtcNow).TotalHours
                            : 8760 // 1 year default
                    }
                };

                // Store using enclave storage service
                var sealResult = await _enclaveStorageService.SealDataAsync(sealingRequest, BlockchainType.NeoN3);

                if (!sealResult.Success)
                {
                    _logger.LogError("Failed to seal confidential data for key {Key}: {Error}", key, sealResult.ErrorMessage);
                    return new ConfidentialStorageResult
                    {
                        Success = false,
                        ErrorMessage = sealResult.ErrorMessage
                    };
                }

                var compressionRatio = originalSize > 0 ? (double)sealResult.SealedSize / originalSize : 1.0;

                _logger.LogDebug("Successfully stored confidential data for key {Key}, sealed size: {SealedSize} bytes", 
                    key, sealResult.SealedSize);

                return new ConfidentialStorageResult
                {
                    Success = true,
                    StorageId = sealResult.StorageId,
                    SealedDataSize = sealResult.SealedSize,
                    OriginalDataSize = originalSize,
                    CompressionRatio = compressionRatio,
                    DataFingerprint = sealResult.Fingerprint,
                    ExpiresAt = sealResult.ExpiresAt,
                    Metadata = new Dictionary<string, object>
                    {
                        ["sealingPolicy"] = storageOptions.SealingPolicy.ToString(),
                        ["compressionEnabled"] = storageOptions.EnableCompression,
                        ["compressionRatio"] = compressionRatio
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while storing confidential data for key {Key}", key);
                return new ConfidentialStorageResult
                {
                    Success = false,
                    ErrorMessage = $"Storage operation failed: {ex.Message}"
                };
            }
        }

        public async Task<ConfidentialRetrievalResult<T>> RetrieveAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _logger.LogDebug("Retrieving confidential data for key {Key}", key);

            try
            {
                // Retrieve and unseal data from enclave storage
                var unsealResult = await _enclaveStorageService.UnsealDataAsync(key, BlockchainType.NeoN3);

                if (!unsealResult.Success)
                {
                    _logger.LogWarning("Failed to unseal confidential data for key {Key}: {Error}", key, unsealResult.ErrorMessage);
                    return new ConfidentialRetrievalResult<T>
                    {
                        Success = false,
                        ErrorMessage = unsealResult.ErrorMessage
                    };
                }

                // Deserialize data
                var jsonData = System.Text.Encoding.UTF8.GetString(unsealResult.Data);
                T? data;
                try
                {
                    data = JsonSerializer.Deserialize<T>(jsonData);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize retrieved data for key {Key}", key);
                    return new ConfidentialRetrievalResult<T>
                    {
                        Success = false,
                        ErrorMessage = $"Data deserialization failed: {ex.Message}"
                    };
                }

                _logger.LogDebug("Successfully retrieved confidential data for key {Key}", key);

                return new ConfidentialRetrievalResult<T>
                {
                    Success = true,
                    Data = data,
                    Metadata = unsealResult.Metadata ?? new Dictionary<string, object>(),
                    LastAccessedAt = unsealResult.LastAccessed,
                    DataFingerprint = ComputeDataFingerprint(jsonData)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while retrieving confidential data for key {Key}", key);
                return new ConfidentialRetrievalResult<T>
                {
                    Success = false,
                    ErrorMessage = $"Retrieval operation failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                var listResult = await _enclaveStorageService.ListSealedItemsAsync(
                    new ListSealedItemsRequest
                    {
                        Prefix = key,
                        MaxItems = 1
                    }, 
                    BlockchainType.NeoN3);

                return listResult.Items.Any(item => item.Key == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while checking existence of key {Key}", key);
                return false;
            }
        }

        public async Task<ConfidentialDeletionResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            _logger.LogDebug("Deleting confidential data for key {Key}", key);

            try
            {
                var deleteResult = await _enclaveStorageService.DeleteSealedDataAsync(key, BlockchainType.NeoN3);

                _logger.LogDebug("Confidential data deletion for key {Key}: Success={Success}, Found={Found}", 
                    key, deleteResult.Success, deleteResult.Deleted);

                return new ConfidentialDeletionResult
                {
                    Success = deleteResult.Success,
                    DataFound = deleteResult.Deleted,
                    SecurelyWiped = deleteResult.Shredded,
                    ErrorMessage = deleteResult.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting confidential data for key {Key}", key);
                return new ConfidentialDeletionResult
                {
                    Success = false,
                    ErrorMessage = $"Deletion operation failed: {ex.Message}"
                };
            }
        }

        public async Task<ConfidentialKeyListResult> ListKeysAsync(
            string keyPattern = "*",
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing confidential keys with pattern {Pattern}", keyPattern);

            try
            {
                var listRequest = new ListSealedItemsRequest
                {
                    Prefix = keyPattern == "*" ? null : keyPattern,
                    MaxItems = 1000 // Configurable limit
                };

                var listResult = await _enclaveStorageService.ListSealedItemsAsync(listRequest, BlockchainType.NeoN3);

                var keys = listResult.Items.Select(item => new ConfidentialKeyInfo
                {
                    Key = item.Key,
                    SealedDataSize = item.Size,
                    StoredAt = item.Created,
                    LastAccessedAt = item.LastAccessed,
                    ExpiresAt = item.ExpiresAt,
                    SealingPolicy = MapSealingPolicyType(item.PolicyType),
                    AccessCount = 0 // Would need to track this separately
                }).ToList();

                _logger.LogDebug("Found {Count} keys matching pattern {Pattern}", keys.Count, keyPattern);

                return new ConfidentialKeyListResult
                {
                    Success = true,
                    Keys = keys,
                    TotalCount = keys.Count,
                    Pattern = keyPattern
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while listing keys with pattern {Pattern}", keyPattern);
                return new ConfidentialKeyListResult
                {
                    Success = false,
                    ErrorMessage = $"Key listing failed: {ex.Message}",
                    Pattern = keyPattern
                };
            }
        }

        public async Task<ConfidentialBackupResult> CreateBackupAsync(
            ConfidentialBackupRequest backupRequest,
            CancellationToken cancellationToken = default)
        {
            if (backupRequest == null)
                throw new ArgumentNullException(nameof(backupRequest));

            _logger.LogInformation("Creating confidential backup {BackupName}", backupRequest.BackupName);

            try
            {
                var backupEnclaveRequest = new BackupRequest
                {
                    BackupLocation = backupRequest.Destination,
                    ServiceFilter = null // Backup all services
                };

                var backupResult = await _enclaveStorageService.BackupSealedDataAsync(backupEnclaveRequest, BlockchainType.NeoN3);

                if (!backupResult.Success)
                {
                    return new ConfidentialBackupResult
                    {
                        Success = false,
                        ErrorMessage = "Enclave backup operation failed"
                    };
                }

                _logger.LogInformation("Confidential backup {BackupName} created successfully with {Count} items", 
                    backupRequest.BackupName, backupResult.ItemsBackedUp);

                return new ConfidentialBackupResult
                {
                    Success = true,
                    BackupId = backupResult.BackupId,
                    KeysBackedUp = backupResult.ItemsBackedUp,
                    TotalDataSize = backupResult.TotalSize,
                    BackupFileSize = backupResult.TotalSize, // Assume same size for now
                    CompressionRatio = 1.0,
                    BackupLocation = backupRequest.Destination
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating backup {BackupName}", backupRequest.BackupName);
                return new ConfidentialBackupResult
                {
                    Success = false,
                    ErrorMessage = $"Backup creation failed: {ex.Message}"
                };
            }
        }

        public async Task<ConfidentialRestoreResult> RestoreBackupAsync(
            ConfidentialRestoreRequest restoreRequest,
            CancellationToken cancellationToken = default)
        {
            if (restoreRequest == null)
                throw new ArgumentNullException(nameof(restoreRequest));

            _logger.LogInformation("Restoring confidential backup {BackupId}", restoreRequest.BackupId);

            try
            {
                // Note: The current EnclaveStorageService doesn't have a restore method
                // In production, this would be implemented
                await Task.CompletedTask;

                _logger.LogWarning("Backup restore not yet implemented in EnclaveStorageService");
                
                return new ConfidentialRestoreResult
                {
                    Success = false,
                    ErrorMessage = "Restore functionality not yet implemented"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while restoring backup {BackupId}", restoreRequest.BackupId);
                return new ConfidentialRestoreResult
                {
                    Success = false,
                    ErrorMessage = $"Restore operation failed: {ex.Message}"
                };
            }
        }

        public async Task<ConfidentialStorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving confidential storage statistics");

            try
            {
                var stats = await _enclaveStorageService.GetStorageStatisticsAsync(BlockchainType.NeoN3);

                var overallCompressionRatio = stats.TotalSize > 0 
                    ? (double)stats.TotalSize / Math.Max(1, stats.TotalSize) 
                    : 1.0;

                return new ConfidentialStorageStatistics
                {
                    TotalKeys = stats.TotalItems,
                    TotalSealedDataSize = stats.TotalSize,
                    TotalOriginalDataSize = stats.TotalSize, // Would track this separately in production
                    OverallCompressionRatio = overallCompressionRatio,
                    AvailableSpace = stats.AvailableSpace,
                    UtilizationPercent = stats.TotalSize > 0 
                        ? ((double)stats.TotalSize / (stats.TotalSize + stats.AvailableSpace)) * 100 
                        : 0,
                    ExpiredKeys = 0, // Would need to calculate this
                    HealthStatus = StorageHealthStatus.Healthy,
                    LastBackupTime = stats.LastBackup,
                    AdditionalMetrics = new Dictionary<string, object>
                    {
                        ["serviceCount"] = stats.ServiceCount,
                        ["serviceStorage"] = stats.ServiceStorage
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while retrieving storage statistics");
                return new ConfidentialStorageStatistics
                {
                    HealthStatus = StorageHealthStatus.Failed,
                    AdditionalMetrics = new Dictionary<string, object>
                    {
                        ["error"] = ex.Message
                    }
                };
            }
        }

        public async Task<ConfidentialIntegrityResult> CheckIntegrityAsync(
            string? key = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking confidential storage integrity for key pattern {Key}", key ?? "all");

            try
            {
                // Get all keys to check
                var keysToCheck = new List<string>();
                if (string.IsNullOrEmpty(key))
                {
                    var allKeys = await ListKeysAsync("*", cancellationToken);
                    keysToCheck = allKeys.Keys.Select(k => k.Key).ToList();
                }
                else
                {
                    keysToCheck.Add(key);
                }

                var results = new List<KeyIntegrityResult>();
                var corruptedKeys = new List<string>();

                foreach (var keyToCheck in keysToCheck)
                {
                    try
                    {
                        // Try to retrieve and verify the data
                        var retrievalResult = await RetrieveAsync<object>(keyToCheck, cancellationToken);
                        
                        var integrityResult = new KeyIntegrityResult
                        {
                            Key = keyToCheck,
                            IsValid = retrievalResult.Success,
                            ActualFingerprint = retrievalResult.DataFingerprint,
                            ErrorMessage = retrievalResult.ErrorMessage
                        };

                        results.Add(integrityResult);

                        if (!retrievalResult.Success)
                        {
                            corruptedKeys.Add(keyToCheck);
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new KeyIntegrityResult
                        {
                            Key = keyToCheck,
                            IsValid = false,
                            ErrorMessage = ex.Message
                        });
                        corruptedKeys.Add(keyToCheck);
                    }
                }

                var integrityValid = corruptedKeys.Count == 0;

                _logger.LogDebug("Integrity check completed: {Valid} keys valid, {Corrupted} keys corrupted", 
                    results.Count - corruptedKeys.Count, corruptedKeys.Count);

                return new ConfidentialIntegrityResult
                {
                    IntegrityValid = integrityValid,
                    KeysChecked = results.Count,
                    CorruptedKeys = corruptedKeys.Count,
                    CorruptedKeyList = corruptedKeys,
                    DetailedResults = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during integrity check");
                return new ConfidentialIntegrityResult
                {
                    IntegrityValid = false,
                    KeysChecked = 0,
                    CorruptedKeys = 0,
                    DetailedResults = new List<KeyIntegrityResult>
                    {
                        new KeyIntegrityResult
                        {
                            Key = key ?? "all",
                            IsValid = false,
                            ErrorMessage = $"Integrity check failed: {ex.Message}"
                        }
                    }
                };
            }
        }

        public async Task<IConfidentialStorageTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transactionId = Guid.NewGuid().ToString();
            var transaction = new ConfidentialStorageTransaction(transactionId, this, _logger);

            lock (_transactionsLock)
            {
                _activeTransactions[transactionId] = transaction;
            }

            // Set up cleanup on dispose
            transaction.TransactionDisposed += (sender, args) =>
            {
                lock (_transactionsLock)
                {
                    _activeTransactions.Remove(transactionId);
                }
            };

            _logger.LogDebug("Created confidential storage transaction {TransactionId}", transactionId);
            return transaction;
        }

        private NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType MapSealingPolicy(SealingPolicy policy)
        {
            return policy switch
            {
                SealingPolicy.MrEnclave => NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrEnclave,
                SealingPolicy.MrSigner => NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrSigner,
                SealingPolicy.Custom => NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrSigner, // Default fallback
                _ => NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrSigner
            };
        }

        private SealingPolicy MapSealingPolicyType(NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType policyType)
        {
            return policyType switch
            {
                NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrEnclave => SealingPolicy.MrEnclave,
                NeoServiceLayer.Services.EnclaveStorage.Models.SealingPolicyType.MrSigner => SealingPolicy.MrSigner,
                _ => SealingPolicy.MrSigner
            };
        }

        private string ComputeDataFingerprint(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Implementation of confidential storage transaction
    /// </summary>
    internal class ConfidentialStorageTransaction : IConfidentialStorageTransaction
    {
        private readonly ConfidentialStorageService _storageService;
        private readonly ILogger _logger;
        private readonly List<TransactionOperation> _operations;
        private bool _disposed = false;

        public string TransactionId { get; }
        public bool IsActive { get; private set; }

        public event EventHandler? TransactionDisposed;

        public ConfidentialStorageTransaction(
            string transactionId,
            ConfidentialStorageService storageService,
            ILogger logger)
        {
            TransactionId = transactionId;
            _storageService = storageService;
            _logger = logger;
            _operations = new List<TransactionOperation>();
            IsActive = true;
        }

        public async Task<ConfidentialStorageResult> StoreAsync<T>(
            string key,
            T data,
            ConfidentialStorageOptions? storageOptions = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                throw new InvalidOperationException("Transaction is not active");

            // Add to transaction operations (not executed until commit)
            _operations.Add(new TransactionOperation
            {
                Type = TransactionOperationType.Store,
                Key = key,
                Data = data,
                StorageOptions = storageOptions
            });

            // Return success for now - actual operation happens on commit
            return new ConfidentialStorageResult
            {
                Success = true,
                StorageId = $"tx_{TransactionId}_{_operations.Count}",
                Metadata = new Dictionary<string, object>
                {
                    ["transactionId"] = TransactionId,
                    ["operationIndex"] = _operations.Count - 1,
                    ["deferred"] = true
                }
            };
        }

        public async Task<ConfidentialDeletionResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                throw new InvalidOperationException("Transaction is not active");

            // Add to transaction operations (not executed until commit)
            _operations.Add(new TransactionOperation
            {
                Type = TransactionOperationType.Delete,
                Key = key
            });

            // Return success for now - actual operation happens on commit
            return new ConfidentialDeletionResult
            {
                Success = true,
                DataFound = true // Assumed for transaction
            };
        }

        public async Task<ConfidentialTransactionResult> CommitAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                throw new InvalidOperationException("Transaction is not active");

            _logger.LogDebug("Committing transaction {TransactionId} with {Count} operations", 
                TransactionId, _operations.Count);

            try
            {
                // Execute all operations
                foreach (var operation in _operations)
                {
                    switch (operation.Type)
                    {
                        case TransactionOperationType.Store:
                            await _storageService.StoreAsync(
                                operation.Key,
                                operation.Data!,
                                operation.StorageOptions,
                                cancellationToken);
                            break;

                        case TransactionOperationType.Delete:
                            await _storageService.DeleteAsync(operation.Key, cancellationToken);
                            break;
                    }
                }

                IsActive = false;
                _logger.LogDebug("Transaction {TransactionId} committed successfully", TransactionId);

                return new ConfidentialTransactionResult
                {
                    Success = true,
                    OperationCount = _operations.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction {TransactionId}", TransactionId);
                IsActive = false;
                
                return new ConfidentialTransactionResult
                {
                    Success = false,
                    OperationCount = _operations.Count,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ConfidentialTransactionResult> RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                throw new InvalidOperationException("Transaction is not active");

            _logger.LogDebug("Rolling back transaction {TransactionId} with {Count} operations", 
                TransactionId, _operations.Count);

            // Clear operations and mark as inactive
            _operations.Clear();
            IsActive = false;

            return new ConfidentialTransactionResult
            {
                Success = true,
                OperationCount = 0
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (IsActive)
            {
                RollbackAsync().GetAwaiter().GetResult();
            }

            TransactionDisposed?.Invoke(this, EventArgs.Empty);
            _disposed = true;
        }

        private class TransactionOperation
        {
            public TransactionOperationType Type { get; set; }
            public string Key { get; set; } = string.Empty;
            public object? Data { get; set; }
            public ConfidentialStorageOptions? StorageOptions { get; set; }
        }

        private enum TransactionOperationType
        {
            Store,
            Delete
        }
    }
}