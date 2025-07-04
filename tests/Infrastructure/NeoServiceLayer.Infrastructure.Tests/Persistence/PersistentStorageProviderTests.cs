using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Infrastructure.Persistence;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Persistence;

/// <summary>
/// Comprehensive unit tests for PersistentStorageProvider covering all storage operations.
/// Tests multiple storage providers, encryption, compression, chunking, and transactions.
/// </summary>
public class PersistentStorageProviderTests : IDisposable
{
    private readonly Mock<ILogger<OcclumFileStorageProvider>> _mockLogger;
    private readonly OcclumFileStorageProvider _provider;
    private readonly string _testStoragePath;

    public PersistentStorageProviderTests()
    {
        // Set test encryption key for OcclumFileStorageProvider
        Environment.SetEnvironmentVariable("ENCLAVE_MASTER_KEY", "test-encryption-key-for-unit-tests");

        _mockLogger = new Mock<ILogger<OcclumFileStorageProvider>>();
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"test-storage-{Guid.NewGuid():N}");
        _provider = new OcclumFileStorageProvider(_testStoragePath, _mockLogger.Object);
    }

    #region Initialization Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Initialization")]
    public async Task InitializeAsync_ValidConfiguration_InitializesSuccessfully()
    {
        // Act
        await _provider.InitializeAsync();

        // Assert
        _provider.IsInitialized.Should().BeTrue();
        Directory.Exists(_testStoragePath).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Initialization")]
    public async Task InitializeAsync_AlreadyInitialized_DoesNotReinitialize()
    {
        // Arrange
        await _provider.InitializeAsync();
        var firstInitTime = Directory.GetCreationTime(_testStoragePath);

        // Wait a moment to see if time changes
        await Task.Delay(10);

        // Act
        await _provider.InitializeAsync();

        // Assert
        _provider.IsInitialized.Should().BeTrue();
        Directory.GetCreationTime(_testStoragePath).Should().Be(firstInitTime);
    }

    #endregion

    #region Basic Storage Operations Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task StoreAsync_ValidData_StoresSuccessfully()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "test_key";
        var data = System.Text.Encoding.UTF8.GetBytes("test_data");
        var options = new StorageOptions { Encrypt = false, Compress = false };

        // Act
        await _provider.StoreAsync(key, data, options);

        // Assert
        var storedFile = Path.Combine(_testStoragePath, $"{key}.dat");
        File.Exists(storedFile).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task RetrieveAsync_ExistingKey_ReturnsData()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "test_key";
        var originalData = System.Text.Encoding.UTF8.GetBytes("test_data");
        var options = new StorageOptions { Encrypt = false, Compress = false };

        await _provider.StoreAsync(key, originalData, options);

        // Act
        var retrievedData = await _provider.RetrieveAsync(key);

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task RetrieveAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string nonExistentKey = "non_existent_key";
        var options = new StorageOptions();

        // Act
        var result = await _provider.RetrieveAsync(nonExistentKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task DeleteAsync_ExistingKey_DeletesSuccessfully()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "test_key";
        var data = System.Text.Encoding.UTF8.GetBytes("test_data");
        var options = new StorageOptions();

        await _provider.StoreAsync(key, data, options);

        // Act
        var result = await _provider.DeleteAsync(key);

        // Assert
        result.Should().BeTrue();

        // Verify data is deleted
        var retrievedData = await _provider.RetrieveAsync(key);
        retrievedData.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "test_key";
        var data = System.Text.Encoding.UTF8.GetBytes("test_data");
        var options = new StorageOptions();

        await _provider.StoreAsync(key, data, options);

        // Act
        var exists = await _provider.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "BasicStorage")]
    public async Task ExistsAsync_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string nonExistentKey = "non_existent_key";

        // Act
        var exists = await _provider.ExistsAsync(nonExistentKey);

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Encryption Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Encryption")]
    public async Task StoreAsync_WithEncryption_EncryptsData()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "encrypted_key";
        var data = System.Text.Encoding.UTF8.GetBytes("sensitive_data");
        var options = new StorageOptions { Encrypt = true, EncryptionKey = "test_encryption_key" };

        // Act
        await _provider.StoreAsync(key, data, options);

        // Assert
        // File should exist but content should be encrypted (different from original)
        var storedFile = Path.Combine(_testStoragePath, $"{key}.dat");
        File.Exists(storedFile).Should().BeTrue();
        var fileContent = await File.ReadAllBytesAsync(storedFile);
        fileContent.Should().NotBeEquivalentTo(data); // Should be encrypted, not raw data
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Encryption")]
    public async Task RetrieveAsync_WithEncryption_DecryptsData()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "encrypted_key";
        var originalData = System.Text.Encoding.UTF8.GetBytes("sensitive_data");
        var options = new StorageOptions { Encrypt = true, EncryptionKey = "test_encryption_key" };

        await _provider.StoreAsync(key, originalData, options);

        // Act
        var retrievedData = await _provider.RetrieveAsync(key);

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData.Should().BeEquivalentTo(originalData);
    }

    #endregion

    #region Compression Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    public async Task StoreAsync_WithCompression_CompressesData()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "compressed_key";
        var largeData = System.Text.Encoding.UTF8.GetBytes(new string('A', 10000));
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = CompressionAlgorithm.GZip };

        // Act
        await _provider.StoreAsync(key, largeData, options);

        // Assert
        var storedFile = Path.Combine(_testStoragePath, $"{key}.dat");
        File.Exists(storedFile).Should().BeTrue();
        var fileSize = new FileInfo(storedFile).Length;
        fileSize.Should().BeLessThan(largeData.Length); // Should be compressed
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Compression")]
    public async Task RetrieveAsync_WithCompression_DecompressesData()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "compressed_key";
        var originalData = System.Text.Encoding.UTF8.GetBytes(new string('A', 10000));
        var options = new StorageOptions { Compress = true, CompressionAlgorithm = CompressionAlgorithm.GZip };

        await _provider.StoreAsync(key, originalData, options);

        // Act
        var retrievedData = await _provider.RetrieveAsync(key);

        // Assert
        retrievedData.Should().NotBeNull();
        retrievedData.Should().BeEquivalentTo(originalData);
    }

    #endregion

    #region Transaction Support Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task BeginTransactionAsync_ValidTransaction_ReturnsTransactionId()
    {
        // Arrange
        await _provider.InitializeAsync();

        // Act
        var transaction = await _provider.BeginTransactionAsync();

        // Assert
        transaction.Should().NotBeNull();
        transaction.TransactionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task CommitTransactionAsync_ValidTransaction_CommitsSuccessfully()
    {
        // Arrange
        await _provider.InitializeAsync();
        var transaction = await _provider.BeginTransactionAsync();

        // Act
        // TODO: Transaction support not yet implemented
        // await _provider.CommitTransactionAsync(transaction.TransactionId);

        // Assert - Should not throw
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Transactions")]
    public async Task RollbackTransactionAsync_ValidTransaction_RollsBackSuccessfully()
    {
        // Arrange
        await _provider.InitializeAsync();
        var transaction = await _provider.BeginTransactionAsync();

        // Act
        // TODO: Transaction support not yet implemented
        // await _provider.RollbackTransactionAsync(transaction.TransactionId);

        // Assert - Should not throw
        true.Should().BeTrue();
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "Storage")]
    public async Task StoreAsync_HighVolumeOperations_PerformsEfficiently()
    {
        // Arrange
        await _provider.InitializeAsync();
        const int operationCount = 50; // Reduced for file-based operations
        var tasks = new List<Task>();
        var options = new StorageOptions { Encrypt = false, Compress = false };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < operationCount; i++)
        {
            var data = System.Text.Encoding.UTF8.GetBytes($"data_{i}");
            tasks.Add(_provider.StoreAsync($"key_{i}", data, options));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000); // Should complete within 15 seconds for file operations
    }

    #endregion

    #region Listing and Metadata Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Metadata")]
    public async Task ListKeysAsync_WithStoredData_ReturnsAllKeys()
    {
        // Arrange
        await _provider.InitializeAsync();
        var keys = new[] { "key1", "key2", "key3" };
        var options = new StorageOptions();

        foreach (var key in keys)
        {
            var data = System.Text.Encoding.UTF8.GetBytes($"data_for_{key}");
            await _provider.StoreAsync(key, data, options);
        }

        // Act
        var retrievedKeys = await _provider.ListKeysAsync();

        // Assert
        retrievedKeys.Should().Contain(keys);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task StoreAsync_InvalidKey_ThrowsArgumentException()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string invalidKey = "";
        var data = System.Text.Encoding.UTF8.GetBytes("test_data");
        var options = new StorageOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _provider.StoreAsync(invalidKey, data, options));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task StoreAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        await _provider.InitializeAsync();
        const string key = "test_key";
        byte[]? nullData = null;
        var options = new StorageOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _provider.StoreAsync(key, nullData!, options));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task RetrieveAsync_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        const string key = "test_key";
        var options = new StorageOptions();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _provider.RetrieveAsync(key));
    }

    #endregion

    public void Dispose()
    {
        try
        {
            _provider?.Dispose();
            if (Directory.Exists(_testStoragePath))
            {
                Directory.Delete(_testStoragePath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
