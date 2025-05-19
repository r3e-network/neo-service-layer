using System;

namespace NeoServiceLayer.Tee.Shared.Storage
{
    /// <summary>
    /// Interface for a factory that creates storage providers.
    /// </summary>
    public interface IStorageFactory
    {
        /// <summary>
        /// Creates a storage provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        IStorageProvider CreateProvider(StorageProviderType providerType, object options = null);

        /// <summary>
        /// Creates a persistent storage provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        IPersistentStorageProvider CreatePersistentProvider(StorageProviderType providerType, object options = null);

        /// <summary>
        /// Creates a secure storage provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        ISecureStorageProvider CreateSecureProvider(StorageProviderType providerType, object options = null);
    }
}
