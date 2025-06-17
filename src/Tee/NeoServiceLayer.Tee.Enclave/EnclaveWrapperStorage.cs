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
    /// Lists all stored data keys in the enclave's secure storage.
    /// </summary>
    /// <returns>JSON string containing array of storage keys and metadata.</returns>
    /// <exception cref="EnclaveException">
    /// Thrown when key enumeration fails due to storage system errors.
    /// </exception>
    public string ListStorageKeys()
    {
        EnsureInitialized();

        byte[] resultBytes = new byte[8192]; // 8KB buffer for key list
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_list_keys(
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                Logger.LogError("Failed to list storage keys. Error code: {ErrorCode}", result);
                throw new EnclaveException($"Failed to list storage keys. Error code: {result}");
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
    /// Gets comprehensive storage usage statistics from the enclave's secure storage.
    /// </summary>
    /// <returns>JSON string containing detailed storage usage information.</returns>
    /// <exception cref="EnclaveException">
    /// Thrown when storage usage retrieval fails due to storage system errors.
    /// </exception>
    public string GetStorageUsage()
    {
        EnsureInitialized();

        byte[] resultBytes = new byte[4096]; // 4KB buffer for usage statistics
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_storage_get_usage(
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                Logger.LogError("Failed to get storage usage. Error code: {ErrorCode}", result);
                throw new EnclaveException($"Failed to get storage usage. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            string usageInfo = Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
            
            Logger.LogDebug("Retrieved storage usage information successfully");
            return usageInfo;
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }
}
