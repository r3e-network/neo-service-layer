using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage
{
    public class OcclumFileStorageProviderTests : IDisposable
    {
        private readonly Mock<ILogger<OcclumFileStorageProvider>> _loggerMock;
        private readonly OcclumFileStorageOptions _options;
        private readonly OcclumFileStorageProvider _provider;
        private readonly string _testDirectory;

        public OcclumFileStorageProviderTests()
        {
            _loggerMock = new Mock<ILogger<OcclumFileStorageProvider>>();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"occlum_storage_test_{Guid.NewGuid()}");
            _options = new OcclumFileStorageOptions
            {
                StorageDirectory = _testDirectory
            };
            _provider = new OcclumFileStorageProvider(_loggerMock.Object, _options);
        }

        public void Dispose()
        {
            _provider.Dispose();

            // Clean up the test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task InitializeAsync_CreatesDirectories()
        {
            // Act
            await _provider.InitializeAsync();

            // Assert
            Assert.True(Directory.Exists(_testDirectory));
            Assert.True(Directory.Exists(Path.Combine(_testDirectory, "data")));
            Assert.True(Directory.Exists(Path.Combine(_testDirectory, "metadata")));
            Assert.True(Directory.Exists(Path.Combine(_testDirectory, "journal")));
        }

        [Fact]
        public async Task WriteAsync_ReadAsync_ReturnsCorrectData()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            await _provider.WriteAsync(key, data);
            byte[] result = await _provider.ReadAsync(key);

            // Assert
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
            await _provider.WriteAsync(key, data);
            bool exists = await _provider.ExistsAsync(key);

            // Assert
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
        public async Task GetSizeAsync_ReturnsCorrectSize()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            await _provider.WriteAsync(key, data);

            // Act
            long size = await _provider.GetSizeAsync(key);

            // Assert
            Assert.Equal(data.Length, size);
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
            Assert.False(metadata.IsChunked);
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
        public async Task WriteChunkedAsync_ReadAsync_ReturnsCorrectData()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] chunk1 = Encoding.UTF8.GetBytes("chunk1");
            byte[] chunk2 = Encoding.UTF8.GetBytes("chunk2");
            var chunks = new[] { chunk1, chunk2 };
            int chunkSize = 10;

            // Act
            await _provider.WriteChunkedAsync(key, chunks, chunkSize);
            byte[] result = await _provider.ReadAsync(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chunk1.Length + chunk2.Length, result.Length);
            
            // Verify the combined data
            byte[] expectedData = new byte[chunk1.Length + chunk2.Length];
            Array.Copy(chunk1, 0, expectedData, 0, chunk1.Length);
            Array.Copy(chunk2, 0, expectedData, chunk1.Length, chunk2.Length);
            Assert.Equal(expectedData, result);
        }

        [Fact]
        public async Task WriteStreamAsync_ReadStreamAsync_ReturnsCorrectData()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data");
            using var inputStream = new MemoryStream(data);

            // Act
            await _provider.WriteStreamAsync(key, inputStream);
            using var outputStream = await _provider.ReadStreamAsync(key);

            // Assert
            Assert.NotNull(outputStream);
            using var resultStream = new MemoryStream();
            await outputStream.CopyToAsync(resultStream);
            byte[] result = resultStream.ToArray();
            Assert.Equal(data, result);
        }

        [Fact]
        public async Task ReadChunkedAsync_ReturnsCorrectChunks()
        {
            // Arrange
            await _provider.InitializeAsync();
            string key = "test_key";
            byte[] data = Encoding.UTF8.GetBytes("test_data_that_will_be_chunked");
            await _provider.WriteAsync(key, data);
            int chunkSize = 5;

            // Act
            var chunks = await _provider.ReadChunkedAsync(key, chunkSize);

            // Assert
            Assert.NotNull(chunks);
            var chunksList = chunks.ToList();
            int expectedChunkCount = (int)Math.Ceiling((double)data.Length / chunkSize);
            Assert.Equal(expectedChunkCount, chunksList.Count);
            
            // Verify each chunk
            byte[] reconstructedData = new byte[0];
            foreach (var chunk in chunksList)
            {
                byte[] newArray = new byte[reconstructedData.Length + chunk.Length];
                Array.Copy(reconstructedData, 0, newArray, 0, reconstructedData.Length);
                Array.Copy(chunk, 0, newArray, reconstructedData.Length, chunk.Length);
                reconstructedData = newArray;
            }
            Assert.Equal(data, reconstructedData);
        }

        [Fact]
        public async Task FlushAsync_CompactAsync_DoNotThrow()
        {
            // Arrange
            await _provider.InitializeAsync();

            // Act & Assert
            await _provider.FlushAsync(); // Should not throw
            await _provider.CompactAsync(); // Should not throw
        }
    }
}
