using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption
{
    /// <summary>
    /// Interface for encryption providers.
    /// </summary>
    public interface IEncryptionProvider : IDisposable
    {
        /// <summary>
        /// Gets the name of the encryption provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the encryption provider.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initializes the encryption provider.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Encrypts data.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="context">Optional context information for authenticated encryption.</param>
        /// <returns>The encrypted data.</returns>
        Task<byte[]> EncryptAsync(byte[] data, byte[] context = null);

        /// <summary>
        /// Decrypts data.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="context">Optional context information for authenticated encryption.</param>
        /// <returns>The decrypted data.</returns>
        Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] context = null);

        /// <summary>
        /// Rotates the encryption key.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RotateKeyAsync();

        /// <summary>
        /// Gets information about the encryption provider.
        /// </summary>
        /// <returns>Information about the encryption provider.</returns>
        EncryptionProviderInfo GetProviderInfo();
    }
}
