using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Backup.Models;

namespace NeoServiceLayer.Services.Backup;

/// <summary>
/// Backup operations for the Backup Service.
/// </summary>
public partial class BackupService
{
    /// <inheritdoc/>
    public async Task<BackupResult> CreateBackupAsync(BackupRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var backupId = Guid.NewGuid().ToString();

        try
        {
            Logger.LogInformation("Creating backup {BackupId} for {DataType} on {Blockchain}",
                backupId, request.DataType, blockchainType);

            // Create backup job
            var job = new BackupJob
            {
                BackupId = backupId,
                Request = request,
                Status = BackupStatus.InProgress,
                StartedAt = DateTime.UtcNow,
                BlockchainType = blockchainType
            };

            lock (_jobsLock)
            {
                _activeJobs[backupId] = job;
            }

            // Perform backup operation
            var backupData = await PerformBackupAsync(request, blockchainType);

            // Store backup
            var storageLocation = await StoreBackupAsync(backupId, backupData, request);

            // Update job status
            job.Status = BackupStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.StorageLocation = storageLocation;

            var result = new BackupResult
            {
                BackupId = backupId,
                Success = true,
                StorageLocation = storageLocation,
                BackupSize = backupData.Length,
                CreatedAt = job.StartedAt,
                CompletedAt = job.CompletedAt.Value,
                Metadata = CreateBackupMetadata(request, backupData)
            };

            Logger.LogInformation("Backup {BackupId} completed successfully. Size: {Size} bytes",
                backupId, backupData.Length);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create backup {BackupId}", backupId);

            // Update job status
            lock (_jobsLock)
            {
                if (_activeJobs.TryGetValue(backupId, out var job))
                {
                    job.Status = BackupStatus.Failed;
                    job.ErrorMessage = ex.Message;
                }
            }

            return new BackupResult
            {
                BackupId = backupId,
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            };
        }
        finally
        {
            // Remove from active jobs
            lock (_jobsLock)
            {
                _activeJobs.Remove(backupId);
            }
        }
    }

    /// <summary>
    /// Performs the actual backup operation based on data type.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The backup data.</returns>
    private async Task<byte[]> PerformBackupAsync(BackupRequest request, BlockchainType blockchainType)
    {
        // Perform actual backup based on data type
        return request.DataType.ToLowerInvariant() switch
        {
            "blockchain_state" => await BackupBlockchainStateAsync(request, blockchainType),
            "transaction_history" => await BackupTransactionHistoryAsync(request, blockchainType),
            "smart_contracts" => await BackupSmartContractsAsync(request, blockchainType),
            "user_data" => await BackupUserDataAsync(request, blockchainType),
            "configuration" => await BackupConfigurationAsync(request, blockchainType),
            "service_data" => await BackupServiceDataAsync(request, blockchainType),
            "logs" => await BackupLogsAsync(request, blockchainType),
            _ => await BackupGenericDataAsync(request, blockchainType)
        };
    }

    /// <summary>
    /// Stores backup data to configured storage location.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="backupData">The backup data.</param>
    /// <param name="request">The backup request.</param>
    /// <returns>The storage location.</returns>
    private async Task<string> StoreBackupAsync(string backupId, byte[] backupData, BackupRequest request)
    {
        try
        {
            // Apply compression if requested
            var dataToStore = request.CompressionType switch
            {
                "gzip" => await CompressDataAsync(backupData, "gzip"),
                "zip" => await CompressDataAsync(backupData, "zip"),
                _ => backupData
            };

            // Apply encryption if requested
            if (request.EncryptionEnabled)
            {
                dataToStore = await EncryptDataAsync(dataToStore, request.EncryptionKey);
            }

            // Store to configured storage backend
            var storageLocation = await StoreToStorageBackendAsync(backupId, dataToStore, request);

            Logger.LogDebug("Stored backup {BackupId} to {StorageLocation}. Size: {Size} bytes",
                backupId, storageLocation, dataToStore.Length);

            return storageLocation;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to store backup {BackupId}", backupId);
            throw;
        }
    }

    /// <summary>
    /// Creates backup metadata.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="backupData">The backup data.</param>
    /// <returns>Backup metadata.</returns>
    private Dictionary<string, object> CreateBackupMetadata(BackupRequest request, byte[] backupData)
    {
        return new Dictionary<string, object>
        {
            ["data_type"] = request.DataType,
            ["compression"] = request.CompressionType,
            ["encryption"] = request.EncryptionEnabled,
            ["size"] = backupData.Length,
            ["checksum"] = ComputeChecksum(backupData),
            ["created_at"] = DateTime.UtcNow.ToString("O"),
            ["version"] = "1.0"
        };
    }

    /// <summary>
    /// Compresses data using the specified compression type.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="compressionType">The compression type.</param>
    /// <returns>Compressed data.</returns>
    private async Task<byte[]> CompressDataAsync(byte[] data, string compressionType)
    {
        await Task.Delay(100); // Simulate compression

        return compressionType.ToLowerInvariant() switch
        {
            "gzip" => CompressWithGzip(data),
            "zip" => CompressWithZip(data),
            _ => data
        };
    }

    /// <summary>
    /// Encrypts data using the specified encryption key.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>Encrypted data.</returns>
    private async Task<byte[]> EncryptDataAsync(byte[] data, string? encryptionKey)
    {
        await Task.Delay(50); // Simulate encryption

        if (string.IsNullOrEmpty(encryptionKey))
        {
            // Use default encryption key
            encryptionKey = "default_backup_encryption_key";
        }

        // In production, use proper encryption
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(encryptionKey));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();

        // Prepend IV to encrypted data
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }

        return msEncrypt.ToArray();
    }

    /// <summary>
    /// Stores data to the storage backend.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="request">The backup request.</param>
    /// <returns>Storage location.</returns>
    private async Task<string> StoreToStorageBackendAsync(string backupId, byte[] data, BackupRequest request)
    {
        await Task.Delay(200); // Simulate storage operation

        // In production, this would store to actual storage backends
        var storageType = request.StorageLocation ?? "local";
        
        return storageType.ToLowerInvariant() switch
        {
            "local" => $"file://backups/{backupId}.bak",
            "cloud" => $"cloud://backup-bucket/{backupId}.bak",
            "s3" => $"s3://backup-bucket/{backupId}.bak",
            "azure" => $"azure://backup-container/{backupId}.bak",
            _ => $"backup://storage/{backupId}.bak"
        };
    }

    /// <summary>
    /// Compresses data with GZip.
    /// </summary>
    /// <param name="data">Data to compress.</param>
    /// <returns>Compressed data.</returns>
    private byte[] CompressWithGzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Compresses data with ZIP.
    /// </summary>
    /// <param name="data">Data to compress.</param>
    /// <returns>Compressed data.</returns>
    private byte[] CompressWithZip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("backup.dat");
            using var entryStream = entry.Open();
            entryStream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Computes checksum for data integrity verification.
    /// </summary>
    /// <param name="data">The data to checksum.</param>
    /// <returns>Checksum string.</returns>
    private string ComputeChecksum(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }
}
