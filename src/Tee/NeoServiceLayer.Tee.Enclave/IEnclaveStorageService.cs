namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Interface for enclave storage services.
/// </summary>
public interface IEnclaveStorageService
{
    /// <summary>
    /// Stores data securely in the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>Storage result as JSON string.</returns>
    Task<string> StoreAsync(string key, byte[] data, string encryptionKey, bool compress = false);

    /// <summary>
    /// Retrieves data from secure enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    Task<byte[]> RetrieveAsync(string key, string encryptionKey);

    /// <summary>
    /// Deletes data from enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>Deletion result as JSON string.</returns>
    Task<string> DeleteAsync(string key);

    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>Metadata as JSON string.</returns>
    Task<string> GetMetadataAsync(string key);
}
