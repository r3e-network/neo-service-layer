using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage.Compression
{
    /// <summary>
    /// Factory for creating compression providers.
    /// </summary>
    public class CompressionProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionProviderFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public CompressionProviderFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates a compression provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        public ICompressionProvider CreateProvider(CompressionProviderType providerType, object options = null)
        {
            switch (providerType)
            {
                case CompressionProviderType.GZip:
                    return new GZipCompressionProvider(
                        _loggerFactory.CreateLogger<GZipCompressionProvider>(),
                        options as GZipCompressionOptions);

                case CompressionProviderType.Brotli:
                    return new BrotliCompressionProvider(
                        _loggerFactory.CreateLogger<BrotliCompressionProvider>(),
                        options as BrotliCompressionOptions);

                default:
                    throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType));
            }
        }
    }

    /// <summary>
    /// Types of compression providers.
    /// </summary>
    public enum CompressionProviderType
    {
        /// <summary>
        /// GZip compression provider.
        /// </summary>
        GZip,

        /// <summary>
        /// Brotli compression provider.
        /// </summary>
        Brotli
    }
}
