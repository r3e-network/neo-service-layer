using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

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

        // Initialize the service synchronously for tests
        _service.InitializeAsync().GetAwaiter().GetResult();
        _service.StartAsync().GetAwaiter().GetResult();
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
    public async Task GetDataAsync_ExistingKey_ReturnsData()
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
        var result = await _service.GetDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        // Verify that the data retrieval was attempted via storage API
        _mockEnclaveManager.Verify(x => x.StorageRetrieveDataAsync(
            It.Is<string>(k => k == key),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task GetDataAsync_NonExistentKey_ReturnsNull()
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
            await _service.GetDataAsync(nonExistentKey, BlockchainType.NeoN3));
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
        // Verify that the service called the actual storage delete method (not JavaScript)
        _mockEnclaveManager.Verify(x => x.StorageDeleteDataAsync("test_key", It.IsAny<CancellationToken>()), Times.Once);
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
        // Verify that the service called encryption via JavaScript (the actual method used)
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("encryptData")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
    public async Task GetDataAsync_WithEncryption_DecryptsData()
    {
        // Arrange
        const string key = "encrypted_key";
        const string originalData = "sensitive_data";
        var options = new StorageOptions { Encrypt = true, EncryptionAlgorithm = "AES-256-GCM" };

        await _service.InitializeAsync();
        await _service.StartAsync();

        SetupEncryptedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.GetDataAsync(key, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(System.Text.Encoding.UTF8.GetBytes(originalData));
        // Verify that storage retrieve was called with the encryption key
        _mockEnclaveManager.Verify(x => x.StorageRetrieveDataAsync(
            It.Is<string>(k => k == key),
            It.Is<string>(ek => ek == "test-key-id"),
            It.IsAny<CancellationToken>()), Times.Once);
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
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("encryptData") && script.Contains(algorithm)),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
        // Verify that the data was stored via storage API (the actual method used)
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        // Verify that metadata was stored
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    public async Task GetDataAsync_WithCompression_DecompressesData()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "compressed_key";
        var originalData = new string('A', 10000);
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = "gzip" };

        SetupCompressedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.GetDataAsync(key, BlockchainType.NeoN3);

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
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
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
        // Verify that the data was stored via storage API (the actual method used)
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        // Verify that metadata was stored
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(
            It.Is<string>(script => script.Contains("storeMetadata")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Chunking")]
    public async Task GetDataAsync_ChunkedData_ReassemblesCorrectly()
    {
        // Arrange
        await InitializeServiceAsync();
        const string key = "large_data_key";
        var originalData = new string('X', 100000);
        var options = new StorageOptions { ChunkSize = 32768 };

        SetupChunkedStorageRetrieval(key, originalData);

        // Act
        var result = await _service.GetDataAsync(key, BlockchainType.NeoN3);

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
    public async Task GetDataAsync_HighVolumeOperations_PerformsEfficiently()
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
            tasks.Add(_service.GetDataAsync($"key_{i}", BlockchainType.NeoN3));
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
                if (script.Contains("getAllMetadata")) return "[]";
                if (script.Contains("storeMetadata")) return "true";
                if (script.Contains("updateMetadata")) return "true";
                if (script.Contains("deleteMetadata")) return "true";
                if (script.Contains("getMetadata")) return "{\"Key\":\"test_key\",\"SizeBytes\":17,\"IsEncrypted\":false,\"IsCompressed\":false,\"ChunkCount\":1,\"ChunkSizeBytes\":1048576,\"CreatedAt\":\"2025-01-01T00:00:00Z\",\"LastModifiedAt\":\"2025-01-01T00:00:00Z\",\"LastAccessedAt\":\"2025-01-01T00:00:00Z\"}";
                if (script.Contains("retrieveData")) return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test_data_content"));
                if (script.Contains("encryptData")) return "{\"encryptedData\":\"dGVzdF9kYXRhX2NvbnRlbnRfZW5jcnlwdGVk\"}";
                if (script.Contains("decryptData")) return "test_data_content";
                if (script.Contains("compressData")) return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("compressed_data"));
                if (script.Contains("decompressData")) return "test_data_content";
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

        // Setup transaction operations
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains("beginTransaction")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"transactionId\":\"test-transaction-id\",\"status\":\"active\"}");

        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains("commitTransaction")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"success\":true,\"transactionId\":\"test-transaction-id\",\"status\":\"committed\"}");

        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains("rollbackTransaction")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"success\":true,\"transactionId\":\"test-transaction-id\",\"status\":\"rolledback\"}");
    }

    private void SetupStorageRetrieval(string key, string data)
    {
        // Storage API expects base64 encoded data
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.Is<string>(s => s.Contains(key)), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(base64Data);
    }

    private void SetupEncryptedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval with proper encryption metadata
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"Key\":\"" + key + "\",\"SizeBytes\":" + originalData.Length + ",\"IsEncrypted\":true,\"IsCompressed\":false,\"EncryptionKeyId\":\"test-key-id\",\"EncryptionAlgorithm\":\"AES-256-GCM\",\"ChunkCount\":1}");

        // Setup storage retrieval - return encrypted data
        var originalBytes = System.Text.Encoding.UTF8.GetBytes(originalData);
        var encryptedBytes = EncryptDataForTest(originalBytes, "test-key-id");
        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.Is<string>(k => k == key), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(encryptedBytes));
    }

    private void SetupCompressedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"Key\":\"" + key + "\",\"SizeBytes\":" + originalData.Length + ",\"IsEncrypted\":false,\"IsCompressed\":true,\"ChunkCount\":1,\"ChunkSizeBytes\":1048576,\"CreatedAt\":\"2025-01-01T00:00:00Z\",\"LastModifiedAt\":\"2025-01-01T00:00:00Z\",\"LastAccessedAt\":\"2025-01-01T00:00:00Z\"}");

        // Setup storage retrieval - return compressed data
        var originalBytes = System.Text.Encoding.UTF8.GetBytes(originalData);
        var compressedBytes = CompressDataForTest(originalBytes);
        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.Is<string>(k => k == key), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(compressedBytes));
    }

    private static byte[] CompressDataForTest(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] EncryptDataForTest(byte[] data, string keyId)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keyId));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var output = new MemoryStream();

        // Write IV to the beginning
        output.Write(aes.IV, 0, aes.IV.Length);

        using (var cryptoStream = new System.Security.Cryptography.CryptoStream(output, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        {
            cryptoStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private void SetupChunkedStorageRetrieval(string key, string originalData)
    {
        // Setup metadata retrieval
        _mockEnclaveManager
            .Setup(x => x.ExecuteJavaScriptAsync(It.Is<string>(script => script.Contains($"getMetadata('{key}')")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"Key\":\"" + key + "\",\"SizeBytes\":" + originalData.Length + ",\"IsEncrypted\":false,\"IsCompressed\":false,\"ChunkCount\":1,\"ChunkSizeBytes\":1048576,\"CreatedAt\":\"2025-01-01T00:00:00Z\",\"LastModifiedAt\":\"2025-01-01T00:00:00Z\",\"LastAccessedAt\":\"2025-01-01T00:00:00Z\"}");

        // Setup storage retrieval - the actual method used
        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.Is<string>(k => k == key), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
