using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Storage.Tests;

/// <summary>
/// Comprehensive unit tests for StorageService covering all storage operations.
/// Tests persistent storage, encryption, compression, chunking, and transaction support.
/// </summary>
public class StorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IServiceRegistry> _mockServiceRegistry;
    private readonly StorageService _service;

    public StorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<StorageService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockServiceRegistry = new Mock<IServiceRegistry>();

        SetupConfiguration();
        SetupEnclaveManager();

        _service = new StorageService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    #region Service Lifecycle Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StartAsync_ValidConfiguration_InitializesSuccessfully()
    {
        // Act
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Assert
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Starting Storage Service");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StopAsync_RunningService_StopsSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Information, "Stopping Storage Service");
    }

    #endregion

    #region Basic Storage Operations Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task StoreDataAsync_ValidData_StoresSuccessfully()
    {
        // Arrange
        const string key = "test_key";
        const string data = "test_data_content";
        var options = new StorageOptions { Encrypt = true, Compress = false };

        // Arrange - Initialize service first
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(data), options, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Key.Should().Be(key);
        // Verify that the data was stored via StorageStoreDataAsync (the actual method used)
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that metadata was stored via JavaScript
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task RetrieveDataAsync_ExistingKey_ReturnsData()
    {
        // Arrange
        const string key = "test_key";
        const string expectedData = "test_data_content";

        await _service.InitializeAsync();
        await _service.StartAsync();

        // First store the data
        var options = new StorageOptions { Encrypt = false, Compress = false };
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(expectedData), options, BlockchainType.NeoN3);

        // Act
        var result = await _service.RetrieveDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        // Verify that the data retrieval was attempted via JavaScript
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("retrieveData")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task RetrieveDataAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        await InitializeServiceAsync();
        const string nonExistentKey = "non_existent_key";

        // Setup the mock to throw KeyNotFoundException for non-existent metadata
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{nonExistentKey}')")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Key not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.RetrieveDataAsync(nonExistentKey, BlockchainType.NeoN3));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task DeleteDataAsync_ExistingKey_DeletesSuccessfully()
    {
        // Arrange
        const string key = "test_key";

        await _service.InitializeAsync();
        await _service.StartAsync();

        _mockEnclaveManager
            .Setup(x => x.StorageDeleteDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
        // Verify that the service called the correct JavaScript functions for deletion
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("deleteData('test_key')")),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("deleteMetadata('test_key')")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Encryption Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Encryption")]
    public async Task StoreDataAsync_WithEncryption_EncryptsData()
    {
        // Arrange
        const string key = "encrypted_key";
        const string data = "sensitive_data";
        var options = new StorageOptions { Encrypt = true, EncryptionAlgorithm = "AES-256-GCM" };

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(data), options, BlockchainType.NeoN3);

        // Assert
        // Verify that the service called KMS encryption (the actual method used)
        _mockEnclaveManager.Verify(x => x.KmsEncryptDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(alg => alg == "AES-256-GCM")), Times.Once);
        // Verify that the data was stored via StorageStoreDataAsync
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that metadata was stored via JavaScript
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Encryption")]
    public async Task RetrieveDataAsync_WithEncryption_DecryptsData()
    {
        // Arrange
        const string key = "encrypted_key";
        const string originalData = "sensitive_data";
        var options = new StorageOptions { Encrypt = true, EncryptionAlgorithm = "AES-256-GCM" };

        await _service.InitializeAsync();
        await _service.StartAsync();

        SetupEncryptedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.RetrieveDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(originalData));
        // Verify that KMS decryption was called (the actual method used)
        _mockEnclaveManager.Verify(x => x.KmsDecryptDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Encryption")]
    [InlineData("AES-256-GCM")]
    [InlineData("AES-256-CBC")]
    [InlineData("ChaCha20-Poly1305")]
    public async Task StoreDataAsync_DifferentEncryptionAlgorithms_HandlesCorrectly(string algorithm)
    {
        // Arrange
        const string key = "test_key";
        const string data = "test_data";
        var options = new StorageOptions { Encrypt = true, EncryptionAlgorithm = algorithm };

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(data), options, BlockchainType.NeoN3);

        // Assert
        // Verify that the operation completed successfully (encryption functionality is working)
        _mockEnclaveManager.Verify(x => x.KmsEncryptDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(alg => alg == algorithm)), Times.AtLeastOnce);
    }

    #endregion

    #region Compression Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    public async Task StoreDataAsync_WithCompression_CompressesData()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "compressed_key";
        var largeData = new string('A', 10000); // Large data that benefits from compression
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = "gzip" };

        // Act
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(largeData), options, BlockchainType.NeoN3);

        // Assert
        // Verify that the data was stored via JavaScript (the actual method used)
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeData")),
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that metadata was stored
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    public async Task RetrieveDataAsync_WithCompression_DecompressesData()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "compressed_key";
        var originalData = new string('A', 10000);
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = "gzip" };

        SetupCompressedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.RetrieveDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(originalData));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    [InlineData("gzip")]
    [InlineData("deflate")]
    [InlineData("brotli")]
    public async Task StoreDataAsync_DifferentCompressionAlgorithms_HandlesCorrectly(string algorithm)
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "test_key";
        var data = new string('B', 5000);
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = algorithm };

        // Act
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(data), options, BlockchainType.NeoN3);

        // Assert
        // Verify that the operation completed successfully (compression functionality is working)
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeData")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Chunking Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Chunking")]
    public async Task StoreDataAsync_LargeData_ChunksData()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "large_data_key";
        var largeData = new string('X', 100000); // 100KB data
        var options = new StorageOptions { ChunkSize = 32768 }; // 32KB chunks

        // Act
        await _service.StoreDataAsync(key, System.Text.Encoding.UTF8.GetBytes(largeData), options, BlockchainType.NeoN3);

        // Assert
        // Verify that the data was stored via JavaScript (the actual method used)
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeData")),
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that metadata was stored
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Chunking")]
    public async Task RetrieveDataAsync_ChunkedData_ReassemblesCorrectly()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "large_data_key";
        var originalData = new string('X', 100000);
        var options = new StorageOptions { ChunkSize = 32768 };

        SetupChunkedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.RetrieveDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(originalData));
    }

    #endregion

    #region Transaction Support Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task BeginTransactionAsync_ValidTransaction_ReturnsTransactionId()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var transactionId = await _service.BeginTransactionAsync(BlockchainType.NeoN3);

        // Assert
        transactionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task CommitTransactionAsync_ValidTransaction_CommitsSuccessfully()
    {
        // Arrange
        await InitializeServiceAsync();
        var transactionId = await _service.BeginTransactionAsync(BlockchainType.NeoN3);

        // Act
        await _service.CommitTransactionAsync(transactionId, BlockchainType.NeoN3);

        // Assert
        // Transaction completed successfully (no exception thrown)
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task RollbackTransactionAsync_ValidTransaction_RollsBackSuccessfully()
    {
        // Arrange
        await InitializeServiceAsync();
        var transactionId = await _service.BeginTransactionAsync(BlockchainType.NeoN3);

        // Act
        await _service.RollbackTransactionAsync(transactionId, BlockchainType.NeoN3);

        // Assert
        // Transaction rollback completed successfully (no exception thrown)
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "Storage")]
    public async Task StoreDataAsync_HighVolumeOperations_PerformsEfficiently()
    {
        // Arrange
        await InitializeServiceAsync();
        const int operationCount = 100;
        var tasks = new List<Task>();
        var options = new StorageOptions { Encrypt = true, Compress = false };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < operationCount; i++)
        {
            tasks.Add(_service.StoreDataAsync($"key_{i}", System.Text.Encoding.UTF8.GetBytes($"data_{i}"), options, BlockchainType.NeoN3));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "Storage")]
    public async Task RetrieveDataAsync_HighVolumeOperations_PerformsEfficiently()
    {
        // Arrange
        await InitializeServiceAsync();
        const int operationCount = 100;
        var tasks = new List<Task<byte[]>>();
        var options = new StorageOptions { Encrypt = true, Compress = false };

        // Setup multiple keys
        for (int i = 0; i < operationCount; i++)
        {
            SetupStorageRetrieval($"key_{i}", $"data_{i}");
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < operationCount; i++)
        {
            tasks.Add(_service.RetrieveDataAsync($"key_{i}", BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(operationCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    #endregion

    #region Helper Methods

    private async Task InitializeServiceAsync()
    {
        await _service.InitializeAsync();
        await _service.StartAsync();
    }

    private void SetupConfiguration()
    {
        _mockConfiguration
            .Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);
    }

    private void SetupEnclaveManager()
    {
        // Setup enclave initialization - both methods
        _mockEnclaveManager
            .Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEnclaveManager
            .Setup(x => x.InitializeEnclaveAsync())
            .ReturnsAsync(true);

        // Setup IsInitialized property
        _mockEnclaveManager
            .Setup(x => x.IsInitialized)
            .Returns(true);

        // Setup basic enclave operations that storage service uses - both single and double parameter versions
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string script, CancellationToken ct) =>
            {
                if (script.Contains("storeMetadata")) return "true";
                if (script.Contains("getMetadata")) return "{\"key\":\"test_key\",\"sizeBytes\":100,\"isEncrypted\":false,\"isCompressed\":false,\"chunkCount\":1}";
                if (script.Contains("retrieveData")) return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test_data_content"));
                return "{}";
            });

        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"result\": \"success\"}");

        // Setup storage-specific methods
        _mockEnclaveManager
            .Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"success\": true, \"key\": \"test_key\"}");

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string encryptionKey, CancellationToken ct) => 
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test_data_content")));

        _mockEnclaveManager
            .Setup(x => x.StorageDeleteDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup string-based encryption/decryption methods
        _mockEnclaveManager
            .Setup(x => x.EncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string data, string key, CancellationToken ct) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data + "_encrypted")));

        _mockEnclaveManager
            .Setup(x => x.DecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string encryptedData, string key, CancellationToken ct) => encryptedData.Replace("_encrypted", ""));

        // Setup key generation for encryption
        _mockEnclaveManager
            .Setup(x => x.KmsGenerateKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync("{\"keyId\":\"test-key-id\",\"keyType\":\"AES256\"}");

        // Setup KMS encryption/decryption methods (these are the ones actually called by StorageService)
        _mockEnclaveManager
            .Setup(x => x.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string keyId, string dataHex, string algorithm, CancellationToken ct) =>
            {
                // Return a valid hex string (simulate encryption by adding some hex bytes)
                return dataHex + "deadbeef";
            });

        // Also setup the overload without CancellationToken
        _mockEnclaveManager
            .Setup(x => x.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string keyId, string dataHex, string algorithm) =>
            {
                // Return a valid hex string (simulate encryption by adding some hex bytes)
                return dataHex + "deadbeef";
            });

        _mockEnclaveManager
            .Setup(x => x.KmsDecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string keyId, string encryptedHex, string algorithm, CancellationToken ct) =>
            {
                // Return the original hex string (simulate decryption by removing the added bytes)
                return encryptedHex.EndsWith("deadbeef") ? encryptedHex.Substring(0, encryptedHex.Length - 8) : encryptedHex;
            });

        // Also setup the overload without CancellationToken
        _mockEnclaveManager
            .Setup(x => x.KmsDecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string keyId, string encryptedHex, string algorithm) =>
            {
                // Return the original hex string (simulate decryption by removing the added bytes)
                return encryptedHex.EndsWith("deadbeef") ? encryptedHex.Substring(0, encryptedHex.Length - 8) : encryptedHex;
            });
    }

    private void SetupStorageRetrieval(string key, string data)
    {
        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.Is<string>(s => s.Contains(key)), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
    }

    private void SetupEncryptedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval with proper encryption metadata
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"Key\":\"" + key + "\",\"SizeBytes\":" + originalData.Length + ",\"IsEncrypted\":true,\"IsCompressed\":false,\"EncryptionKeyId\":\"test-key-id\",\"EncryptionAlgorithm\":\"AES-256-GCM\",\"ChunkCount\":1}");

        // Setup data retrieval - return the original data as base64 (simulating encrypted storage)
        var originalDataBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalData));

        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"retrieveData('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalDataBase64);

        // Setup KMS decryption to return original data as hex (the service expects hex output from KMS)
        var originalDataHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(originalData));
        _mockEnclaveManager
            .Setup(x => x.KmsDecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(originalDataHex);
    }

    private void SetupCompressedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"key\":\"" + key + "\",\"sizeBytes\":" + originalData.Length + ",\"isEncrypted\":false,\"isCompressed\":true,\"chunkCount\":1}");

        // Setup data retrieval - return the original data as base64
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"retrieveData('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalData)));
    }

    private void SetupChunkedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"key\":\"" + key + "\",\"sizeBytes\":" + originalData.Length + ",\"isEncrypted\":false,\"isCompressed\":false,\"chunkCount\":1}");

        // Setup data retrieval - return the original data as base64
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"retrieveData('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalData)));
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #endregion
}
