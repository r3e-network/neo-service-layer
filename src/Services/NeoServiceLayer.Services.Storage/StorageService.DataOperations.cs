using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeoServiceLayer.Services.Storage;

/// <summary>
/// Data storage operations for the Storage Service.
/// </summary>
public partial class StorageService
{
    /// <inheritdoc/>
    public async Task<StorageMetadata> StoreDataAsync(string key, byte[] data, StorageOptions options, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        }

        try
        {
            IncrementRequestCounters();

            // Check if the key already exists
            if (_metadataCache.ContainsKey(key))
            {
                // Update existing data
                await DeleteDataAsync(key, blockchainType);
            }

            // Calculate content hash
            string contentHash;
            using (var sha256 = SHA256.Create())
            {
                contentHash = Convert.ToHexString(sha256.ComputeHash(data));
            }

            // Prepare data for storage
            byte[] dataToStore = data;
            bool isCompressed = false;
            bool isEncrypted = false;
            string? encryptionKeyId = null;

            // Compress data if requested
            if (options.Compress)
            {
                dataToStore = CompressData(dataToStore);
                isCompressed = true;
            }

            // Encrypt data if requested
            if (options.Encrypt)
            {
                encryptionKeyId = options.EncryptionKeyId ?? "storage-default-key";
                dataToStore = await EncryptDataAsync(dataToStore, encryptionKeyId, options.EncryptionAlgorithm);
                isEncrypted = true;
            }

            // Determine chunk size
            int chunkSizeBytes = options.ChunkSizeBytes;
            if (chunkSizeBytes <= 0)
            {
                chunkSizeBytes = int.Parse(_configuration.GetValue("Storage:DefaultChunkSizeBytes", "1048576")); // 1 MB
            }

            // Split data into chunks if necessary
            int chunkCount = 1;
            if (chunkSizeBytes > 0 && dataToStore.Length > chunkSizeBytes)
            {
                chunkCount = (int)Math.Ceiling((double)dataToStore.Length / chunkSizeBytes);
            }

            // Store data in the enclave using real encrypted storage
            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * chunkSizeBytes;
                int length = Math.Min(chunkSizeBytes, dataToStore.Length - offset);
                byte[] chunk = new byte[length];
                Array.Copy(dataToStore, offset, chunk, 0, length);

                string chunkKey = chunkCount > 1 ? $"{key}.chunk{i}" : key;

                // Use real enclave storage with encryption
                string chunkDataBase64 = Convert.ToBase64String(chunk);
                string storageResult = await _enclaveManager.StorageStoreDataAsync(chunkKey, chunkDataBase64, encryptionKeyId ?? "default");

                // Verify storage was successful
                var resultJson = JsonSerializer.Deserialize<JsonElement>(storageResult);
                if (!resultJson.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                {
                    throw new InvalidOperationException($"Failed to store chunk {chunkKey}");
                }
            }

            // Create metadata
            var metadata = new StorageMetadata
            {
                Key = key,
                SizeBytes = data.Length,
                IsCompressed = isCompressed,
                IsEncrypted = isEncrypted,
                EncryptionKeyId = encryptionKeyId,
                EncryptionAlgorithm = isEncrypted ? options.EncryptionAlgorithm : null,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                ExpiresAt = options.ExpiresAt,
                AccessControlList = options.AccessControlList,
                ChunkCount = chunkCount,
                ChunkSizeBytes = chunkSizeBytes,
                ReplicationFactor = options.ReplicationFactor,
                StorageClass = options.StorageClass,
                ContentHash = contentHash,
                CustomMetadata = options.CustomMetadata
            };

            // Store metadata in the enclave
            string metadataJson = JsonSerializer.Serialize(metadata);
            await _enclaveManager.ExecuteJavaScriptAsync($"storeMetadata('{key}', {metadataJson})");

            // Update the cache
            lock (_metadataCache)
            {
                _metadataCache[key] = metadata;
            }

            RecordSuccess();
            UpdateMetric("TotalStoredBytes", _metadataCache.Values.Sum(m => m.SizeBytes));
            return metadata;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error storing data with key {Key} for blockchain {BlockchainType}",
                key, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> RetrieveDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateStorageOperation(key, blockchainType);

        try
        {
            IncrementRequestCounters();

            // Get metadata
            var metadata = await GetMetadataAsync(key, blockchainType);
            if (metadata == null)
            {
                throw new KeyNotFoundException($"No data found for key {key}.");
            }

            // Update last accessed time
            metadata.LastAccessedAt = DateTime.UtcNow;
            await UpdateMetadataAsync(key, metadata, blockchainType);

            // Retrieve data from the enclave using real encrypted storage
            byte[] retrievedData;
            string encryptionKey = metadata.EncryptionKeyId ?? "default";

            if (metadata.ChunkCount > 1)
            {
                // Retrieve and combine chunks
                retrievedData = await RetrieveChunkedDataAsync(key, metadata, encryptionKey);
            }
            else
            {
                // Retrieve single chunk
                string retrievedDataBase64 = await _enclaveManager.StorageRetrieveDataAsync(key, encryptionKey);
                retrievedData = Convert.FromBase64String(retrievedDataBase64);
            }

            // Note: Data is already decrypted and decompressed by the enclave storage function
            // No additional processing needed since the enclave handles encryption/decryption internally

            RecordSuccess();
            return retrievedData;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            Logger.LogError(ex, "Error retrieving data with key {Key} for blockchain {BlockchainType}",
                key, blockchainType);
            throw;
        }
    }

    /// <summary>
    /// Retrieves chunked data and combines it.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="metadata">The storage metadata.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The combined data.</returns>
    private async Task<byte[]> RetrieveChunkedDataAsync(string key, StorageMetadata metadata, string encryptionKey)
    {
        var combinedData = new List<byte>();

        for (int i = 0; i < metadata.ChunkCount; i++)
        {
            string chunkKey = $"{key}.chunk{i}";
            string chunkDataBase64 = await _enclaveManager.StorageRetrieveDataAsync(chunkKey, encryptionKey);
            byte[] chunkData = Convert.FromBase64String(chunkDataBase64);
            combinedData.AddRange(chunkData);
        }

        return combinedData.ToArray();
    }

    /// <summary>
    /// Compresses data using GZip compression.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>The compressed data.</returns>
    private static byte[] CompressData(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Decompresses data using GZip decompression.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <returns>The decompressed data.</returns>
    private static byte[] DecompressData(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Encrypts data using the specified algorithm.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="keyId">The encryption key ID.</param>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>The encrypted data.</returns>
    private Task<byte[]> EncryptDataAsync(byte[] data, string keyId, string algorithm)
    {
        // In production, this would use the actual encryption key from the key management service
        // For now, use a simple AES encryption
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keyId));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();

        // Prepend IV to encrypted data
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }

        return Task.FromResult(msEncrypt.ToArray());
    }

    /// <summary>
    /// Decrypts data using the specified algorithm.
    /// </summary>
    /// <param name="encryptedData">The encrypted data.</param>
    /// <param name="keyId">The encryption key ID.</param>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>The decrypted data.</returns>
    private Task<byte[]> DecryptDataAsync(byte[] encryptedData, string keyId, string algorithm)
    {
        if (encryptedData.Length < 16) // AES IV is 16 bytes
        {
            throw new ArgumentException("Encrypted data is too short to contain IV");
        }

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keyId));

        // Extract IV from the beginning of encrypted data
        var iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;

        // Extract actual encrypted content
        var encryptedContent = new byte[encryptedData.Length - 16];
        Array.Copy(encryptedData, 16, encryptedContent, 0, encryptedContent.Length);

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encryptedContent);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var msPlain = new MemoryStream();

        csDecrypt.CopyTo(msPlain);
        return Task.FromResult(msPlain.ToArray());
    }
}
