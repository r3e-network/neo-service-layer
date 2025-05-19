using System;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Storage.Providers;
using NeoServiceLayer.Tee.Shared.Interfaces;
using NeoServiceLayer.Tee.Shared.Storage;

namespace NeoServiceLayer.Tee.Host.Storage
{
    /// <summary>
    /// Factory for creating storage providers.
    /// </summary>
    public class StorageFactory : IStorageFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITeeInterface _teeInterface;

        /// <summary>
        /// Initializes a new instance of the StorageFactory class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public StorageFactory(ILoggerFactory loggerFactory)
            : this(loggerFactory, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StorageFactory class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        /// <param name="teeInterface">The TEE interface to use for secure storage providers.</param>
        public StorageFactory(ILoggerFactory loggerFactory, ITeeInterface teeInterface)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _teeInterface = teeInterface;
        }

        /// <inheritdoc/>
        public IStorageProvider CreateProvider(StorageProviderType providerType, object options = null)
        {
            switch (providerType)
            {
                case StorageProviderType.Memory:
                    return new MemoryStorageProvider(
                        _loggerFactory.CreateLogger<MemoryStorageProvider>());

                case StorageProviderType.File:
                    return new FileStorageProvider(
                        _loggerFactory.CreateLogger<FileStorageProvider>(),
                        options as FileStorageOptions);

                case StorageProviderType.OcclumFile:
                    return new OcclumFileStorageProvider(
                        _loggerFactory.CreateLogger<OcclumFileStorageProvider>(),
                        options as OcclumFileStorageOptions);

                case StorageProviderType.RocksDB:
                    return new RocksDBStorageProvider(
                        _loggerFactory.CreateLogger<RocksDBStorageProvider>(),
                        options as RocksDBStorageOptions);

                case StorageProviderType.LevelDB:
                    return new LevelDBStorageProvider(
                        _loggerFactory.CreateLogger<LevelDBStorageProvider>(),
                        options as LevelDBStorageOptions);

                case StorageProviderType.Sqlite:
                    return new SqliteStorageProvider(
                        _loggerFactory.CreateLogger<SqliteStorageProvider>(),
                        options as PersistentStorage.SqliteStorageOptions);

                case StorageProviderType.Secure:
                    if (_teeInterface == null)
                    {
                        throw new InvalidOperationException("TEE interface is required for secure storage providers");
                    }
                    return new SecureStorageProvider(
                        _loggerFactory.CreateLogger<SecureStorageProvider>(),
                        _teeInterface,
                        options as SecureStorageOptions);

                default:
                    throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType));
            }
        }

        /// <inheritdoc/>
        public IPersistentStorageProvider CreatePersistentProvider(StorageProviderType providerType, object options = null)
        {
            var provider = CreateProvider(providerType, options);
            if (provider is IPersistentStorageProvider persistentProvider)
            {
                return persistentProvider;
            }

            throw new ArgumentException($"Provider type {providerType} does not implement IPersistentStorageProvider", nameof(providerType));
        }

        /// <inheritdoc/>
        public ISecureStorageProvider CreateSecureProvider(StorageProviderType providerType, object options = null)
        {
            var provider = CreateProvider(providerType, options);
            if (provider is ISecureStorageProvider secureProvider)
            {
                return secureProvider;
            }

            throw new ArgumentException($"Provider type {providerType} does not implement ISecureStorageProvider", nameof(providerType));
        }
    }
}
