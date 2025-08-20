/// <summary>
/// Interface for enclave storage services.
/// </summary>
    /// <summary>
    /// Stores data securely in the enclave.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <param name="compress">Whether to compress the data.</param>
    /// <returns>Storage result as JSON string.</returns>
    /// <summary>
    /// Retrieves data from secure enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="encryptionKey">The encryption key.</param>
    /// <returns>The decrypted data.</returns>
    /// <summary>
    /// Deletes data from enclave storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>Deletion result as JSON string.</returns>
    /// <summary>
    /// Gets metadata for stored data.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>Metadata as JSON string.</returns>
