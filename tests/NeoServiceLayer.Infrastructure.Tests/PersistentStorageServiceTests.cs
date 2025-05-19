using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Services;
using NeoServiceLayer.Core.Storage;
using NeoServiceLayer.Infrastructure.Services;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests
{
    /// <summary>
    /// Tests for the persistent storage service.
    /// </summary>
    [Trait("Category", "Storage")]
    public class PersistentStorageServiceTests
    {
        private readonly Mock<IPersistentStorageProvider> _mockProvider;
        private readonly Mock<ILogger<PersistentStorageService>> _mockLogger;
        private readonly IPersistentStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the PersistentStorageServiceTests class.
        /// </summary>
        public PersistentStorageServiceTests()
        {
            _mockProvider = new Mock<IPersistentStorageProvider>();
            _mockLogger = new Mock<ILogger<PersistentStorageService>>();
            _storageService = new PersistentStorageService(_mockProvider.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeAsync_CallsProviderInitialize()
        {
            // Arrange
            var options = new PersistentStorageOptions
            {
                StoragePath = "/tmp/test",
                EnableEncryption = true,
                EncryptionKey = Encoding.UTF8.GetBytes("TestKey12345678901234567890"),
                EnableCompression = true
            };

            _mockProvider.Setup(p => p.InitializeAsync(It.IsAny<PersistentStorageOptions>()))
                .Returns(Task.CompletedTask);

            // Act
            await _storageService.InitializeAsync(options);

            // Assert
            _mockProvider.Verify(p => p.InitializeAsync(It.Is<PersistentStorageOptions>(o =>
                o.StoragePath == options.StoragePath &&
                o.EnableEncryption == options.EnableEncryption &&
                o.EnableCompression == options.EnableCompression)), Times.Once);
            Assert.True(_storageService.IsInitialized);
        }

        [Fact]
        public async Task ReadAsync_CallsProviderRead()
        {
            // Arrange
            var key = "test_key";
            var expectedData = Encoding.UTF8.GetBytes("Test Data");

            _mockProvider.Setup(p => p.ReadAsync(key, default))
                .ReturnsAsync(expectedData);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.ReadAsync(key);

            // Assert
            _mockProvider.Verify(p => p.ReadAsync(key, default), Times.Once);
            Assert.Equal(expectedData, result);
        }

        [Fact]
        public async Task WriteAsync_CallsProviderWrite()
        {
            // Arrange
            var key = "test_key";
            var data = Encoding.UTF8.GetBytes("Test Data");

            _mockProvider.Setup(p => p.WriteAsync(key, data, default))
                .Returns(Task.CompletedTask);

            // Initialize the service
            await InitializeService();

            // Act
            await _storageService.WriteAsync(key, data);

            // Assert
            _mockProvider.Verify(p => p.WriteAsync(key, data, default), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsProviderDelete()
        {
            // Arrange
            var key = "test_key";

            _mockProvider.Setup(p => p.DeleteAsync(key, default))
                .ReturnsAsync(true);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.DeleteAsync(key);

            // Assert
            _mockProvider.Verify(p => p.DeleteAsync(key, default), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_CallsProviderExists()
        {
            // Arrange
            var key = "test_key";

            _mockProvider.Setup(p => p.ExistsAsync(key, default))
                .ReturnsAsync(true);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.ExistsAsync(key);

            // Assert
            _mockProvider.Verify(p => p.ExistsAsync(key, default), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task ListKeysAsync_CallsProviderListKeys()
        {
            // Arrange
            var prefix = "test_prefix";
            var expectedKeys = new[] { "test_prefix/key1", "test_prefix/key2" };

            _mockProvider.Setup(p => p.ListKeysAsync(prefix, default))
                .ReturnsAsync(expectedKeys);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.ListKeysAsync(prefix);

            // Assert
            _mockProvider.Verify(p => p.ListKeysAsync(prefix, default), Times.Once);
            Assert.Equal(expectedKeys, result);
        }

        [Fact]
        public async Task GetSizeAsync_CallsProviderGetSize()
        {
            // Arrange
            var key = "test_key";
            var expectedSize = 1024L;

            _mockProvider.Setup(p => p.GetSizeAsync(key, default))
                .ReturnsAsync(expectedSize);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.GetSizeAsync(key);

            // Assert
            _mockProvider.Verify(p => p.GetSizeAsync(key, default), Times.Once);
            Assert.Equal(expectedSize, result);
        }

        [Fact]
        public void BeginTransaction_CallsProviderBeginTransaction()
        {
            // Arrange
            var mockTransaction = new Mock<IStorageTransaction>();
            _mockProvider.Setup(p => p.BeginTransaction())
                .Returns(mockTransaction.Object);

            // Initialize the service
            InitializeService().GetAwaiter().GetResult();

            // Act
            var result = _storageService.BeginTransaction();

            // Assert
            _mockProvider.Verify(p => p.BeginTransaction(), Times.Once);
            Assert.Same(mockTransaction.Object, result);
        }

        [Fact]
        public async Task ReadJsonAsync_DeserializesJsonData()
        {
            // Arrange
            var key = "test_json_key";
            var testObject = new TestObject { Id = 1, Name = "Test" };
            var jsonData = Encoding.UTF8.GetBytes("{\"id\":1,\"name\":\"Test\"}");

            _mockProvider.Setup(p => p.ReadAsync(key, default))
                .ReturnsAsync(jsonData);

            // Initialize the service
            await InitializeService();

            // Act
            var result = await _storageService.ReadJsonAsync<TestObject>(key);

            // Assert
            _mockProvider.Verify(p => p.ReadAsync(key, default), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(testObject.Id, result.Id);
            Assert.Equal(testObject.Name, result.Name);
        }

        [Fact]
        public async Task WriteJsonAsync_SerializesObjectToJson()
        {
            // Arrange
            var key = "test_json_key";
            var testObject = new TestObject { Id = 1, Name = "Test" };

            _mockProvider.Setup(p => p.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), default))
                .Returns(Task.CompletedTask);

            // Initialize the service
            await InitializeService();

            // Act
            await _storageService.WriteJsonAsync(key, testObject);

            // Assert
            _mockProvider.Verify(p => p.WriteAsync(
                key,
                It.Is<byte[]>(data => Encoding.UTF8.GetString(data).Contains("\"id\":1") && Encoding.UTF8.GetString(data).Contains("\"name\":\"Test\"")),
                default), Times.Once);
        }

        private async Task InitializeService()
        {
            var options = new PersistentStorageOptions
            {
                StoragePath = "/tmp/test",
                EnableEncryption = true,
                EncryptionKey = Encoding.UTF8.GetBytes("TestKey12345678901234567890"),
                EnableCompression = true
            };

            _mockProvider.Setup(p => p.InitializeAsync(It.IsAny<PersistentStorageOptions>()))
                .Returns(Task.CompletedTask);

            await _storageService.InitializeAsync(options);
        }

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
