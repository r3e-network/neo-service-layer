using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Storage operations for the enclave wrapper.
/// </summary>
public partial class EnclaveWrapper
{
    /// <summary>
    /// Stores encrypted data in the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>JSON string containing the storage result and metadata.</returns>
    public string StoreData(string key, byte[] data, string encryptionKey, bool compress = false)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[4096]; // 4KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_store(
                keyBytes,
                data,
                (UIntPtr)data.Length,
                encryptionKeyBytes,
                compress ? 1 : 0,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to store data with key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Retrieves and decrypts data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] RetrieveData(string key, string encryptionKey)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        byte[] resultBytes = new byte[1024 * 1024]; // 1MB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_retrieve(
                keyBytes,
                encryptionKeyBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to retrieve data with key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            byte[] actualResult = new byte[actualResultSize];
            Array.Copy(resultBytes, actualResult, actualResultSize);
            return actualResult;
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Deletes stored data from the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the deletion result.</returns>
    public string DeleteData(string key)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] resultBytes = new byte[1024]; // 1KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_delete(
                keyBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to delete data with key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>JSON string containing the metadata.</returns>
    public string GetStorageMetadata(string key)
    {
        EnsureInitialized();

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] resultBytes = new byte[4096]; // 4KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_get_metadata(
                keyBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to get metadata for key '{key}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Lists all stored data keys.
    /// </summary>
    /// <returns>Array of storage keys.</returns>
    public string[] ListStorageKeys()
    {
        EnsureInitialized();

        // List all keys from secure storage
        try
        {
            var keys = new List<string>();

            // In a production environment, this would enumerate keys from secure storage
            // For now, return keys from our mock storage
            lock (_secureStorage)
            {
                keys.AddRange(_secureStorage.Keys);
            }

            Logger.LogDebug("Listed {KeyCount} keys from secure storage", keys.Count);
            return keys.ToArray();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to list keys from secure storage");
            throw new EnclaveException($"Failed to list keys: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the total storage usage.
    /// </summary>
    /// <returns>Storage usage information as a JSON string.</returns>
    public string GetStorageUsage()
    {
        EnsureInitialized();

        // Get actual storage usage from secure storage
        try
        {
            long totalSize = 0;
            int keyCount = 0;

            lock (_secureStorage)
            {
                keyCount = _secureStorage.Count;
                totalSize = _secureStorage.Values.Sum(data => System.Text.Encoding.UTF8.GetByteCount(data));
            }

            // In a production environment, these would be actual storage limits
            const long maxStorageSize = 100 * 1024 * 1024; // 100 MB limit
            long availableSize = maxStorageSize - totalSize;

            var storageInfo = new
            {
                totalKeys = keyCount,
                totalSizeBytes = totalSize,
                availableSpaceBytes = Math.Max(0, availableSize),
                compressionRatio = 0.0, // Would be calculated based on actual compression
                encryptionEnabled = true,
                utilizationPercentage = Math.Round((double)totalSize / maxStorageSize * 100, 2),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            Logger.LogDebug("Storage usage: {UsedSize}/{TotalSize} bytes ({UtilizationPercentage}%), {KeyCount} keys",
                totalSize, maxStorageSize, storageInfo.utilizationPercentage, keyCount);

            return System.Text.Json.JsonSerializer.Serialize(storageInfo, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get storage usage");
            throw new EnclaveException($"Failed to get storage usage: {ex.Message}", ex);
        }
    }
}
