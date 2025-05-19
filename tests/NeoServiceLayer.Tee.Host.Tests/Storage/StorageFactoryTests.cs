using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage;
using NeoServiceLayer.Tee.Host.Storage.Providers;
using NeoServiceLayer.Tee.Shared.Storage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage
{
    public class StorageFactoryTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<FileStorageProvider>> _fileLoggerMock;
        private readonly Mock<ILogger<OcclumFileStorageProvider>> _occlumLoggerMock;
        private readonly Mock<ILogger<RocksDBStorageProvider>> _rocksDbLoggerMock;
        private readonly Mock<ILogger<LevelDBStorageProvider>> _levelDbLoggerMock;
        private readonly StorageFactory _factory;

        public StorageFactoryTests()
        {
            _fileLoggerMock = new Mock<ILogger<FileStorageProvider>>();
            _occlumLoggerMock = new Mock<ILogger<OcclumFileStorageProvider>>();
            _rocksDbLoggerMock = new Mock<ILogger<RocksDBStorageProvider>>();
            _levelDbLoggerMock = new Mock<ILogger<LevelDBStorageProvider>>();

            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger<FileStorageProvider>()).Returns(_fileLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger<OcclumFileStorageProvider>()).Returns(_occlumLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger<RocksDBStorageProvider>()).Returns(_rocksDbLoggerMock.Object);
            _loggerFactoryMock.Setup(f => f.CreateLogger<LevelDBStorageProvider>()).Returns(_levelDbLoggerMock.Object);

            _factory = new StorageFactory(_loggerFactoryMock.Object);
        }

        [Fact]
        public void CreateStorageProvider_FileStorage_ReturnsFileStorageProvider()
        {
            // Arrange
            var options = new FileStorageOptions
            {
                StoragePath = Path.Combine(Path.GetTempPath(), "file_storage_test")
            };

            // Act
            var provider = _factory.CreateStorageProvider(StorageProviderType.File, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<FileStorageProvider>(provider);
        }

        [Fact]
        public void CreateStorageProvider_OcclumFileStorage_ReturnsOcclumFileStorageProvider()
        {
            // Arrange
            var options = new OcclumFileStorageOptions
            {
                StoragePath = Path.Combine(Path.GetTempPath(), "occlum_file_storage_test")
            };

            // Act
            var provider = _factory.CreateStorageProvider(StorageProviderType.OcclumFile, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<OcclumFileStorageProvider>(provider);
        }

        [Fact]
        public void CreateStorageProvider_RocksDB_ReturnsRocksDBStorageProvider()
        {
            // Arrange
            var options = new RocksDBStorageOptions
            {
                StoragePath = Path.Combine(Path.GetTempPath(), "rocksdb_storage_test")
            };

            // Act
            var provider = _factory.CreateStorageProvider(StorageProviderType.RocksDB, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<RocksDBStorageProvider>(provider);
        }

        [Fact]
        public void CreateStorageProvider_LevelDB_ReturnsLevelDBStorageProvider()
        {
            // Arrange
            var options = new LevelDBStorageOptions
            {
                StoragePath = Path.Combine(Path.GetTempPath(), "leveldb_storage_test")
            };

            // Act
            var provider = _factory.CreateStorageProvider(StorageProviderType.LevelDB, options);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<LevelDBStorageProvider>(provider);
        }

        [Fact]
        public void CreateStorageProvider_InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var options = new FileStorageOptions();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.CreateStorageProvider((StorageProviderType)999, options));
        }

        [Fact]
        public void CreateStorageProvider_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _factory.CreateStorageProvider(StorageProviderType.File, null));
        }

        [Fact]
        public void CreateStorageProvider_MismatchedOptions_ThrowsArgumentException()
        {
            // Arrange
            var options = new RocksDBStorageOptions();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.CreateStorageProvider(StorageProviderType.File, options));
        }
    }
}
