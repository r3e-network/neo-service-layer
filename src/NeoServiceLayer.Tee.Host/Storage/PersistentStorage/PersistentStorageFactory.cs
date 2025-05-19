using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Storage.PersistentStorage
{
    /// <summary>
    /// Factory for creating persistent storage providers.
    /// </summary>
    public class PersistentStorageFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentStorageFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public PersistentStorageFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates a persistent storage provider.
        /// </summary>
        /// <param name="providerType">The type of provider to create.</param>
        /// <param name="options">The options for the provider.</param>
        /// <returns>The created provider.</returns>
        public IPersistentStorageProvider CreateProvider(PersistentStorageProviderType providerType, object options = null)
        {
            switch (providerType)
            {
                case PersistentStorageProviderType.OcclumFile:
                    return new OcclumFileStorageProvider(
                        _loggerFactory.CreateLogger<OcclumFileStorageProvider>(),
                        options as OcclumFileStorageOptions);

                case PersistentStorageProviderType.RocksDB:
                    return new RocksDBStorageProvider(
                        _loggerFactory.CreateLogger<RocksDBStorageProvider>(),
                        options as RocksDBStorageOptions);

                case PersistentStorageProviderType.LevelDB:
                    return new LevelDBStorageProvider(
                        _loggerFactory.CreateLogger<LevelDBStorageProvider>(),
                        options as LevelDBStorageOptions);

                case PersistentStorageProviderType.Sqlite:
                    return new SqliteStorageProvider(
                        _loggerFactory.CreateLogger<SqliteStorageProvider>(),
                        options as SqliteStorageOptions);

                default:
                    throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType));
            }
        }
    }

    /// <summary>
    /// Types of persistent storage providers.
    /// </summary>
    public enum PersistentStorageProviderType
    {
        /// <summary>
        /// File-based storage provider optimized for Occlum LibOS.
        /// </summary>
        OcclumFile,

        /// <summary>
        /// RocksDB storage provider.
        /// </summary>
        RocksDB,

        /// <summary>
        /// LevelDB storage provider.
        /// </summary>
        LevelDB,

        /// <summary>
        /// SQLite storage provider.
        /// </summary>
        Sqlite
    }
}
