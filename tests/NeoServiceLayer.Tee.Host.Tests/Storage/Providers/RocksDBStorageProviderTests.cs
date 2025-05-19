using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.Providers;
using NeoServiceLayer.Tee.Shared.Storage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.Providers
{
    public class RocksDBStorageProviderTests : IDisposable
    {
        private readonly Mock<ILogger<RocksDBStorageProvider>> _loggerMock;
        private readonly RocksDBStorageOptions _options;
        private readonly RocksDBStorageProvider _provider;
        private readonly string _testDirectory;

        public RocksDBStorageProviderTests()
        {
            _loggerMock = new Mock<ILogger<RocksDBStorageProvider>>();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"rocksdb_storage_test_{Guid.NewGuid()}");
            _options = new RocksDBStorageOptions
            {
                StoragePath = _testDirectory
            };
            _provider = new RocksDBStorageProvider(_loggerMock.Object, _options);
        }

        public void Dispose()
        {
            _provider.Dispose();

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
        public async Task InitializeAsync_CreatesDirectories()
        {
            // Act
            var result = await _provider.InitializeAsync();

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(_testDirectory));
        }

        [Fact]
        public async Task WriteAsync_ReadAsync_ReturnsCorrectData()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            var writeResult = await _provider.WriteAsync(key, data);
            byte[] result = await _provider.ReadAsync(key);

            // Assert
            Assert.True(writeResult);
            Assert.NotNull(result);
            Assert.Equal(data, result);
        }

        [Fact]
        public async Task WriteAsync_ExistsAsync_ReturnsTrue()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            var writeResult = await _provider.WriteAsync(key, data);
            bool exists = await _provider.ExistsAsync(key);

            // Assert
            Assert.True(writeResult);
            Assert.True(exists);
        }

        [Fact]
        public async Task DeleteAsync_ExistsAsync_ReturnsFalse()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            await _provider.WriteAsync(key, data);

            // Act
            bool deleteResult = await _provider.DeleteAsync(key);
            bool exists = await _provider.ExistsAsync(key);

            // Assert
            Assert.True(deleteResult);
            Assert.False(exists);
        }

        [Fact]
        public async Task GetAllKeysAsync_ReturnsAllKeys()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key1 = "test_key_1";
            string key2 = "test_key_2";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            await _provider.WriteAsync(key1, data);
            await _provider.WriteAsync(key2, data);

            // Act
            var keys = await _provider.GetAllKeysAsync();

            // Assert
            Assert.Equal(2, keys.Count);
            Assert.Contains(key1, keys);
            Assert.Contains(key2, keys);
        }

        [Fact]
        public async Task GetMetadataAsync_ReturnsCorrectMetadata()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            await _provider.WriteAsync(key, data);

            // Act
            var metadata = await _provider.GetMetadataAsync(key);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(key, metadata.Key);
            Assert.Equal(data.Length, metadata.Size);
        }

        [Fact]
        public async Task UpdateMetadataAsync_UpdatesMetadata()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            await _provider.WriteAsync(key, data);
            var metadata = await _provider.GetMetadataAsync(key);
            metadata.ContentType = "text/plain";

            // Act
            bool updateResult = await _provider.UpdateMetadataAsync(key, metadata);
            var updatedMetadata = await _provider.GetMetadataAsync(key);

            // Assert
            Assert.True(updateResult);
            Assert.Equal("text/plain", updatedMetadata.ContentType);
        }

        [Fact]
        public async Task FlushAsync_CompactAsync_DoNotThrow()
        {
            // Arrange
            await _provider.InitializeAsync();

            // Act & Assert
            var flushResult = await _provider.FlushAsync();
            var compactResult = await _provider.CompactAsync();

            // Assert
            Assert.True(flushResult);
            Assert.True(compactResult);
        }

        [Fact]
        public async Task TransactionSupport_WorksCorrectly()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act - Begin transaction
            string transactionId = await _provider.BeginTransactionAsync();
            Assert.NotNull(transactionId);

            // Write in transaction
            bool writeResult = await _provider.WriteInTransactionAsync(transactionId, key, data);
            Assert.True(writeResult);

            // Commit transaction
            bool commitResult = await _provider.CommitTransactionAsync(transactionId);
            Assert.True(commitResult);

            // Verify data was written
            bool exists = await _provider.ExistsAsync(key);
            Assert.True(exists);

            byte[] readData = await _provider.ReadAsync(key);
            Assert.Equal(data, readData);

            // Begin another transaction for rollback test
            string transactionId2 = await _provider.BeginTransactionAsync();
            Assert.NotNull(transactionId2);

            // Delete in transaction
            bool deleteResult = await _provider.DeleteInTransactionAsync(transactionId2, key);
            Assert.True(deleteResult);

            // Rollback transaction
            bool rollbackResult = await _provider.RollbackTransactionAsync(transactionId2);
            Assert.True(rollbackResult);

            // Verify data still exists after rollback
            exists = await _provider.ExistsAsync(key);
            Assert.True(exists);
        }
    }
}
