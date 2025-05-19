using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Encryption
{
    /// <summary>
    /// Factory for creating encryption providers.
    /// </summary>
    public class EncryptionProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionProviderFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public EncryptionProviderFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates an encryption provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        public IEncryptionProvider CreateProvider(EncryptionProviderType providerType, object options = null)
        {
            switch (providerType)
            {
                case EncryptionProviderType.Aes:
                    return new AesEncryptionProvider(
                        _loggerFactory.CreateLogger<AesEncryptionProvider>(),
                        options as AesEncryptionOptions);

                case EncryptionProviderType.ChaCha20:
                    return new ChaCha20EncryptionProvider(
                        _loggerFactory.CreateLogger<ChaCha20EncryptionProvider>(),
                        options as ChaCha20EncryptionOptions);

                default:
                    throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType));
            }
        }
    }

    /// <summary>
    /// Types of encryption providers.
    /// </summary>
    public enum EncryptionProviderType
    {
        /// <summary>
        /// AES encryption provider.
        /// </summary>
        Aes,

        /// <summary>
        /// ChaCha20 encryption provider.
        /// </summary>
        ChaCha20
    }
}
