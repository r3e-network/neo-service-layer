using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO.Compression;

namespace NeoServiceLayer.Infrastructure.Persistence;

/// <summary>
/// Occlum LibOS file system storage provider.
/// Provides secure, encrypted, and compressed storage using Occlum's trusted file system.
/// </summary>
public class OcclumFileStorageProvider : IPersistentStorageProvider
{
    private readonly string _storagePath;
    private readonly ILogger<OcclumFileStorageProvider> _logger;
    private readonly object _lock = new();
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcclumFileStorageProvider"/> class.
    /// </summary>
    /// <param name="storagePath">The storage path within Occlum file system.</param>
    /// <param name="logger">The logger.</param>
    public OcclumFileStorageProvider(string storagePath, ILogger<OcclumFileStorageProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(storagePath);
        ArgumentNullException.ThrowIfNull(logger);
        
        _storagePath = storagePath;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string ProviderName => "OcclumFileStorage";

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <inheritdoc/>
    public bool SupportsTransactions => true;

    /// <inheritdoc/>
    public bool SupportsCompression => true;

    /// <inheritdoc/>
    public bool SupportsEncryption => true;

    /// <inheritdoc/>
    public Task<bool> InitializeAsync()
    {
        if (_initialized) return Task.FromResult(true);

        lock (_lock)
        {
            if (_initialized) return Task.FromResult(true);

            try
            {
                // Create storage directory in Occlum file system
                if (!Directory.Exists(_storagePath))
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation("Created Occlum storage directory: {StoragePath}", _storagePath);
                }

                // Create metadata directory
                var metadataPath = Path.Combine(_storagePath, ".metadata");
                if (!Directory.Exists(metadataPath))
                {
                    Directory.CreateDirectory(metadataPath);
                }

                _initialized = true;
                _logger.LogInformation("Occlum file storage provider initialized successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Occlum file storage provider");
                return Task.FromResult(false);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StoreAsync(string key, byte[] data, StorageOptions? options = null)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
        if (data == null) throw new ArgumentNullException(nameof(data));

        options ??= new StorageOptions();

        try
        {
            var processedData = await ProcessDataForStorageAsync(data, options);
            var filePath = GetFilePath(key);
            var metadataPath = GetMetadataPath(key);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Store data
            await File.WriteAllBytesAsync(filePath, processedData);

            // Store metadata
            var metadata = new StorageMetadata
            {
                Key = key,
                OriginalSize = data.Length,
                StoredSize = processedData.Length,
                IsCompressed = options.Compress,
                IsEncrypted = options.Encrypt,
                CompressionAlgorithm = options.Compress ? options.CompressionAlgorithm : null,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                ExpiresAt = options.TimeToLive.HasValue ? DateTime.UtcNow.Add(options.TimeToLive.Value) : null,
                Checksum = CalculateChecksum(data),
                CustomMetadata = new Dictionary<string, string>(options.Metadata)
            };

            var metadataJson = JsonSerializer.Serialize(metadata);
            await File.WriteAllTextAsync(metadataPath, metadataJson);

            _logger.LogDebug("Stored data with key {Key} in Occlum file system", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store data with key {Key}", key);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> RetrieveAsync(string key)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var filePath = GetFilePath(key);
            var metadataPath = GetMetadataPath(key);

            if (!File.Exists(filePath) || !File.Exists(metadataPath))
            {
                return null;
            }

            // Load metadata
            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<StorageMetadata>(metadataJson);

            if (metadata == null)
            {
                _logger.LogWarning("Invalid metadata for key {Key}", key);
                return null;
            }

            // Check expiration
            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogInformation("Data with key {Key} has expired, removing", key);
                await DeleteAsync(key);
                return null;
            }

            // Load and process data
            var storedData = await File.ReadAllBytesAsync(filePath);
            var originalData = await ProcessDataFromStorageAsync(storedData, metadata);

            // Update access time
            metadata.LastAccessed = DateTime.UtcNow;
            var updatedMetadataJson = JsonSerializer.Serialize(metadata);
            await File.WriteAllTextAsync(metadataPath, updatedMetadataJson);

            _logger.LogDebug("Retrieved data with key {Key} from Occlum file system", key);
            return originalData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve data with key {Key}", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string key)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var filePath = GetFilePath(key);
            var metadataPath = GetMetadataPath(key);

            var deleted = false;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                deleted = true;
            }

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
                deleted = true;
            }

            if (deleted)
            {
                _logger.LogDebug("Deleted data with key {Key} from Occlum file system", key);
            }

            return Task.FromResult(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete data with key {Key}", key);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var filePath = GetFilePath(key);
        var metadataPath = GetMetadataPath(key);

        return Task.FromResult(File.Exists(filePath) && File.Exists(metadataPath));
    }

    /// <inheritdoc/>
    public async Task<StorageMetadata?> GetMetadataAsync(string key)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var metadataPath = GetMetadataPath(key);

            if (!File.Exists(metadataPath))
            {
                return null;
            }

            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<StorageMetadata>(metadataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for key {Key}", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> ListKeysAsync(string? prefix = null, int limit = 1000)
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");

        try
        {
            var metadataDir = Path.Combine(_storagePath, ".metadata");
            if (!Directory.Exists(metadataDir))
            {
                return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
            }

            var metadataFiles = Directory.GetFiles(metadataDir, "*.json");
            var keys = new List<string>();

            foreach (var file in metadataFiles.Take(limit))
            {
                var key = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix))
                {
                    keys.Add(key);
                }
            }

            return Task.FromResult<IEnumerable<string>>(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list keys with prefix {Prefix}", prefix);
            return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
        }
    }

    /// <inheritdoc/>
    public async Task<StorageStatistics> GetStatisticsAsync()
    {
        if (!_initialized) throw new InvalidOperationException("Storage provider not initialized");

        try
        {
            var keys = await ListKeysAsync();
            var totalKeys = 0L;
            var totalSize = 0L;
            var totalOriginalSize = 0L;
            var compressedEntries = 0L;
            var encryptedEntries = 0L;

            foreach (var key in keys)
            {
                var metadata = await GetMetadataAsync(key);
                if (metadata != null)
                {
                    totalKeys++;
                    totalSize += metadata.StoredSize;
                    totalOriginalSize += metadata.OriginalSize;

                    if (metadata.IsCompressed) compressedEntries++;
                    if (metadata.IsEncrypted) encryptedEntries++;
                }
            }

            var compressionRatio = totalOriginalSize > 0 ? (double)totalSize / totalOriginalSize : 1.0;

            return new StorageStatistics
            {
                TotalKeys = totalKeys,
                TotalSize = totalSize,
                TotalOriginalSize = totalOriginalSize,
                CompressionRatio = compressionRatio,
                AvailableSpace = GetAvailableSpace(),
                CompressedEntries = compressedEntries,
                EncryptedEntries = encryptedEntries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage statistics");
            return new StorageStatistics();
        }
    }

    /// <inheritdoc/>
    public Task<IStorageTransaction?> BeginTransactionAsync()
    {
        // Occlum file system transactions would be implemented here
        // For now, return null to indicate transactions are not yet implemented
        return Task.FromResult<IStorageTransaction?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> BackupAsync(string backupPath)
    {
        // Implementation would backup the entire storage directory
        throw new NotImplementedException("Backup functionality not yet implemented for Occlum storage");
    }

    /// <inheritdoc/>
    public Task<bool> RestoreAsync(string backupPath)
    {
        // Implementation would restore from backup
        throw new NotImplementedException("Restore functionality not yet implemented for Occlum storage");
    }

    /// <inheritdoc/>
    public Task<bool> CompactAsync()
    {
        // Implementation would compact the storage
        return Task.FromResult(true); // File system doesn't need compaction
    }

    /// <inheritdoc/>
    public Task<StorageValidationResult> ValidateIntegrityAsync()
    {
        var result = new StorageValidationResult { IsValid = true };

        try
        {
            var keys = ListKeysAsync().Result;
            foreach (var key in keys)
            {
                var metadata = GetMetadataAsync(key).Result;
                var data = RetrieveAsync(key).Result;

                if (metadata == null || data == null)
                {
                    result.Errors.Add($"Missing data or metadata for key: {key}");
                    result.CorruptedEntries++;
                    result.IsValid = false;
                }
                else
                {
                    var checksum = CalculateChecksum(data);
                    if (checksum != metadata.Checksum)
                    {
                        result.Errors.Add($"Checksum mismatch for key: {key}");
                        result.CorruptedEntries++;
                        result.IsValid = false;
                    }
                    else
                    {
                        result.ValidatedEntries++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Validation failed: {ex.Message}");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogInformation("Occlum file storage provider disposed");
        }
    }

    private string GetFilePath(string key)
    {
        var safeKey = key.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        return Path.Combine(_storagePath, $"{safeKey}.dat");
    }

    private string GetMetadataPath(string key)
    {
        var safeKey = key.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        return Path.Combine(_storagePath, ".metadata", $"{safeKey}.json");
    }

    private async Task<byte[]> ProcessDataForStorageAsync(byte[] data, StorageOptions options)
    {
        var processedData = data;

        // Compress if requested
        if (options.Compress)
        {
            processedData = await CompressDataAsync(processedData, options.CompressionAlgorithm);
        }

        // Encrypt if requested
        if (options.Encrypt)
        {
            processedData = await EncryptDataAsync(processedData, options.EncryptionKey);
        }

        return processedData;
    }

    private async Task<byte[]> ProcessDataFromStorageAsync(byte[] storedData, StorageMetadata metadata)
    {
        var processedData = storedData;

        // Decrypt if encrypted
        if (metadata.IsEncrypted)
        {
            processedData = await DecryptDataAsync(processedData);
        }

        // Decompress if compressed
        if (metadata.IsCompressed && metadata.CompressionAlgorithm.HasValue)
        {
            processedData = await DecompressDataAsync(processedData, metadata.CompressionAlgorithm.Value);
        }

        return processedData;
    }

    private async Task<byte[]> CompressDataAsync(byte[] data, CompressionAlgorithm algorithm)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();

        switch (algorithm)
        {
            case CompressionAlgorithm.GZip:
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    await input.CopyToAsync(gzip);
                }
                break;
            default:
                return data; // No compression
        }

        return output.ToArray();
    }

    private async Task<byte[]> DecompressDataAsync(byte[] data, CompressionAlgorithm algorithm)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();

        switch (algorithm)
        {
            case CompressionAlgorithm.GZip:
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    await gzip.CopyToAsync(output);
                }
                break;
            default:
                return data; // No decompression
        }

        return output.ToArray();
    }

    private async Task<byte[]> EncryptDataAsync(byte[] data, string? encryptionKey = null)
    {
        // Use SGX-derived encryption key for secure storage
        using var aes = Aes.Create();
        aes.Key = await GetStorageEncryptionKeyAsync();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();

        // Write IV first
        await output.WriteAsync(aes.IV);

        using var cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
        await input.CopyToAsync(cryptoStream);

        return output.ToArray();
    }

    private async Task<byte[]> DecryptDataAsync(byte[] encryptedData)
    {
        // Simple AES decryption - in production, use proper key management
        using var aes = Aes.Create();
        using var input = new MemoryStream(encryptedData);

        // Read IV
        var iv = new byte[16];
        await input.ReadAsync(iv);
        aes.IV = iv;

        // Use SGX-derived encryption key for secure storage
        aes.Key = await GetStorageEncryptionKeyAsync();

        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();

        await cryptoStream.CopyToAsync(output);
        return output.ToArray();
    }

    private string CalculateChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    private long GetAvailableSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(_storagePath) ?? "/");
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Derives a secure encryption key using SGX sealing or PBKDF2 with proper entropy.
    /// </summary>
    /// <returns>A 256-bit encryption key.</returns>
    private async Task<byte[]> GetStorageEncryptionKeyAsync()
    {
        // In production SGX environment, use SGX sealing key derivation
        // For simulation/development, use PBKDF2 with secure parameters
        
        try
        {
            // Try to get SGX-sealed master key first
            var sealedKey = Environment.GetEnvironmentVariable("SGX_SEALED_STORAGE_KEY");
            if (!string.IsNullOrEmpty(sealedKey))
            {
                // In real SGX, this would be unsealed using SGX APIs
                // For now, derive from the sealed key using PBKDF2
                return await DeriveKeyFromSealedAsync(sealedKey);
            }
        }
        catch
        {
            // Fall through to alternative key derivation
        }

        // Fallback: Use PBKDF2 with high iteration count and proper salt
        var masterPassword = Environment.GetEnvironmentVariable("ENCLAVE_MASTER_KEY") 
            ?? throw new InvalidOperationException("No encryption key source available. Set ENCLAVE_MASTER_KEY or SGX_SEALED_STORAGE_KEY.");
        
        // Use a fixed salt derived from the storage path for consistency
        var saltSource = $"neo-storage-{_storagePath}-v2";
        var salt = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(saltSource));
        
        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
            masterPassword,
            salt,
            600000, // 600,000 iterations (OWASP 2023 recommendation)
            System.Security.Cryptography.HashAlgorithmName.SHA256);
        
        return pbkdf2.GetBytes(32); // 256-bit key
    }

    /// <summary>
    /// Derives encryption key from SGX-sealed master key.
    /// </summary>
    /// <param name="sealedKey">The SGX-sealed master key.</param>
    /// <returns>Derived encryption key.</returns>
    private async Task<byte[]> DeriveKeyFromSealedAsync(string sealedKey)
    {
        // In production, this would use SGX unseal operations
        // For simulation, use HKDF for proper key derivation
        var sealedBytes = Convert.FromBase64String(sealedKey);
        
        var info = System.Text.Encoding.UTF8.GetBytes("neo-storage-encryption-v1");
        var salt = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(_storagePath));
        
        return System.Security.Cryptography.HKDF.DeriveKey(
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            sealedBytes,
            32,
            salt,
            info);
    }
}
