using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage
{
    public class PersistentStorageIntegrationTests : IDisposable
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<PersistentStorageFactory>> _factoryLoggerMock;
        private readonly Mock<ILogger<PersistentStorageManager>> _managerLoggerMock;
        private readonly Mock<ILogger<OcclumFileStorageProvider>> _occlumLoggerMock;
        private readonly Mock<ILogger<RocksDBStorageProvider>> _rocksDbLoggerMock;
        private readonly Mock<ILogger<LevelDBStorageProvider>> _levelDbLoggerMock;
        private readonly PersistentStorageFactory _factory;
        private readonly PersistentStorageManager _manager;
        private readonly string _testDirectory;

        public PersistentStorageIntegrationTests()
        {
            _factoryLoggerMock = new Mock<ILogger<PersistentStorageFactory>>();
            _managerLoggerMock = new Mock<ILogger<PersistentStorageManager>>();
            _occlumLoggerMock = new Mock<ILogger<OcclumFileStorageProvider>>();
            _rocksDbLoggerMock = new Mock<ILogger<RocksDBStorageProvider>>();
            _levelDbLoggerMock = new Mock<ILogger<LevelDBStorageProvider>>();
            
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(PersistentStorageFactory).FullName)))
                .Returns(_factoryLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(OcclumFileStorageProvider).FullName)))
                .Returns(_occlumLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(RocksDBStorageProvider).FullName)))
                .Returns(_rocksDbLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.Is<string>(s => s == typeof(LevelDBStorageProvider).FullName)))
                .Returns(_levelDbLoggerMock.Object);
            
            _testDirectory = Path.Combine(Path.GetTempPath(), $"storage_integration_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            _factory = new PersistentStorageFactory(_loggerFactoryMock.Object);
            _manager = new PersistentStorageManager(_managerLoggerMock.Object, _factory);
        }

        public void Dispose()
        {
            _manager.Dispose();
            
            // Clean up the test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }

        [Fact]
        public async Task CreateProviderAsync_OcclumFileStorageProvider_CreatesAndInitializes()
        {
            // Arrange
            string providerName = "occlum_provider";
            var options = new OcclumFileStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "occlum_storage")
            };

            // Act
            var provider = await _manager.CreateProviderAsync(providerName, PersistentStorageProviderType.OcclumFile, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<OcclumFileStorageProvider>(provider);
            Assert.True(Directory.Exists(options.StorageDirectory));
            
            // Verify we can get the provider by name
            var retrievedProvider = _manager.GetProvider(providerName);
            Assert.Same(provider, retrievedProvider);
        }

        [Fact]
        public async Task CreateProviderAsync_RocksDBStorageProvider_CreatesAndInitializes()
        {
            // Arrange
            string providerName = "rocksdb_provider";
            var options = new RocksDBStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "rocksdb_storage")
            };

            // Act
            var provider = await _manager.CreateProviderAsync(providerName, PersistentStorageProviderType.RocksDB, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<RocksDBStorageProvider>(provider);
            Assert.True(Directory.Exists(options.StorageDirectory));
            
            // Verify we can get the provider by name
            var retrievedProvider = _manager.GetProvider(providerName);
            Assert.Same(provider, retrievedProvider);
        }

        [Fact]
        public async Task CreateProviderAsync_LevelDBStorageProvider_CreatesAndInitializes()
        {
            // Arrange
            string providerName = "leveldb_provider";
            var options = new LevelDBStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "leveldb_storage")
            };

            // Act
            var provider = await _manager.CreateProviderAsync(providerName, PersistentStorageProviderType.LevelDB, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<LevelDBStorageProvider>(provider);
            Assert.True(Directory.Exists(options.StorageDirectory));
            
            // Verify we can get the provider by name
            var retrievedProvider = _manager.GetProvider(providerName);
            Assert.Same(provider, retrievedProvider);
        }

        [Fact]
        public async Task RemoveProviderAsync_RemovesProvider()
        {
            // Arrange
            string providerName = "test_provider";
            var options = new OcclumFileStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "test_storage")
            };
            await _manager.CreateProviderAsync(providerName, PersistentStorageProviderType.OcclumFile, options);

            // Act
            bool result = await _manager.RemoveProviderAsync(providerName);

            // Assert
            Assert.True(result);
            Assert.Null(_manager.GetProvider(providerName));
        }

        [Fact]
        public async Task GetAllProviders_ReturnsAllProviders()
        {
            // Arrange
            string provider1Name = "provider1";
            string provider2Name = "provider2";
            var options1 = new OcclumFileStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "storage1")
            };
            var options2 = new RocksDBStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "storage2")
            };
            
            await _manager.CreateProviderAsync(provider1Name, PersistentStorageProviderType.OcclumFile, options1);
            await _manager.CreateProviderAsync(provider2Name, PersistentStorageProviderType.RocksDB, options2);

            // Act
            var providers = _manager.GetAllProviders();

            // Assert
            Assert.Equal(2, providers.Count);
            Assert.Contains(provider1Name, providers.Keys);
            Assert.Contains(provider2Name, providers.Keys);
        }

        [Fact]
        public async Task CrossProviderTest_WriteWithOneProviderReadWithAnother()
        {
            // Arrange
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            
            var occlumOptions = new OcclumFileStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "occlum_storage")
            };
            var rocksDbOptions = new RocksDBStorageOptions
            {
                StorageDirectory = Path.Combine(_testDirectory, "rocksdb_storage")
            };
            
            var occlumProvider = await _manager.CreateProviderAsync("occlum", PersistentStorageProviderType.OcclumFile, occlumOptions);
            var rocksDbProvider = await _manager.CreateProviderAsync("rocksdb", PersistentStorageProviderType.RocksDB, rocksDbOptions);

            // Act - Write with Occlum provider
            await occlumProvider.WriteAsync(key, data);
            
            // Read with both providers
            byte[] occlumResult = await occlumProvider.ReadAsync(key);
            
            // Assert
            Assert.NotNull(occlumResult);
            Assert.Equal(data, occlumResult);
            
            // RocksDB provider should not see the data written by Occlum provider
            byte[] rocksDbResult = await rocksDbProvider.ReadAsync(key);
            Assert.Null(rocksDbResult);
            
            // Now write with RocksDB provider and read with both
            await rocksDbProvider.WriteAsync(key, data);
            
            // Both providers should now have the data, but they're separate instances
            occlumResult = await occlumProvider.ReadAsync(key);
            rocksDbResult = await rocksDbProvider.ReadAsync(key);
            
            Assert.NotNull(occlumResult);
            Assert.NotNull(rocksDbResult);
            Assert.Equal(data, occlumResult);
            Assert.Equal(data, rocksDbResult);
        }
    }
}
