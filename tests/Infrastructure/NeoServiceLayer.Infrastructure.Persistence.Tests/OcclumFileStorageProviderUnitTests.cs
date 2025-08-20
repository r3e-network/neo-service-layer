using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Infrastructure.Persistence;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Persistence.Tests;

public class OcclumFileStorageProviderUnitTests : IDisposable
{
    private readonly Mock<ILogger<OcclumFileStorageProvider>> _mockLogger;
    private readonly string _tempDirectory;
    private readonly OcclumFileStorageProvider _storageProvider;

    public OcclumFileStorageProviderUnitTests()
    {
        _mockLogger = new Mock<ILogger<OcclumFileStorageProvider>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        _storageProvider = new OcclumFileStorageProvider(_mockLogger.Object, _tempDirectory);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var provider = new OcclumFileStorageProvider(_mockLogger.Object, _tempDirectory);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new OcclumFileStorageProvider(null!, _tempDirectory);
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public void Constructor_WithNullBasePath_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new OcclumFileStorageProvider(_mockLogger.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithMessage("*basePath*");
    }

    [Fact]
    public async Task StoreAsync_WithValidData_StoresSuccessfully()
    {
        // Arrange
        var key = "test/key";
        var data = Encoding.UTF8.GetBytes("test data");
        var metadata = new Dictionary<string, object>
        {
            ["contentType"] = "text/plain",
            ["createdAt"] = DateTime.UtcNow
        };

        // Act
        var result = await _storageProvider.StoreAsync(key, data, metadata);

        // Assert
        result.Should().BeTrue();

        var filePath = Path.Combine(_tempDirectory, key.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(filePath).Should().BeTrue();
        
        var storedData = await File.ReadAllBytesAsync(filePath);
        storedData.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task StoreAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");

        // Act & Assert
        Func<Task> act = async () => await _storageProvider.StoreAsync(null!, data);
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*key*");
    }

    [Fact]
    public async Task StoreAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test/key";

        // Act & Assert
        Func<Task> act = async () => await _storageProvider.StoreAsync(key, null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*data*");
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsData()
    {
        // Arrange
        var key = "test/existing-key";
        var data = Encoding.UTF8.GetBytes("test data");
        
        await _storageProvider.StoreAsync(key, data);

        // Act
        var result = await _storageProvider.GetAsync(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = "test/non-existing-key";

        // Act
        var result = await _storageProvider.GetAsync(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "test/existing-key";
        var data = Encoding.UTF8.GetBytes("test data");
        
        await _storageProvider.StoreAsync(key, data);

        // Act
        var result = await _storageProvider.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "test/non-existing-key";

        // Act
        var result = await _storageProvider.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingKey_DeletesSuccessfully()
    {
        // Arrange
        var key = "test/key-to-delete";
        var data = Encoding.UTF8.GetBytes("test data");
        
        await _storageProvider.StoreAsync(key, data);
        var existsBefore = await _storageProvider.ExistsAsync(key);

        // Act
        var result = await _storageProvider.DeleteAsync(key);

        // Assert
        existsBefore.Should().BeTrue();
        result.Should().BeTrue();
        
        var existsAfter = await _storageProvider.ExistsAsync(key);
        existsAfter.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = "test/non-existing-key";

        // Act
        var result = await _storageProvider.DeleteAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListKeysAsync_WithMatchingPrefix_ReturnsKeys()
    {
        // Arrange
        var prefix = "test/list/";
        var keys = new[] { "test/list/key1", "test/list/key2", "test/list/subdir/key3" };
        var data = Encoding.UTF8.GetBytes("test data");

        foreach (var key in keys)
        {
            await _storageProvider.StoreAsync(key, data);
        }

        // Act
        var result = await _storageProvider.ListKeysAsync(prefix);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(3);
        result.Should().Contain("test/list/key1");
        result.Should().Contain("test/list/key2");
        result.Should().Contain("test/list/subdir/key3");
    }

    [Fact]
    public async Task ListKeysAsync_WithPattern_ReturnsMatchingKeys()
    {
        // Arrange
        var prefix = "test/pattern/";
        var pattern = "*.txt";
        var keys = new[] 
        { 
            "test/pattern/file1.txt", 
            "test/pattern/file2.txt", 
            "test/pattern/document.doc",
            "test/pattern/readme.md"
        };
        var data = Encoding.UTF8.GetBytes("test data");

        foreach (var key in keys)
        {
            await _storageProvider.StoreAsync(key, data);
        }

        // Act
        var result = await _storageProvider.ListKeysAsync(prefix, pattern);

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2);
        result.Should().Contain("test/pattern/file1.txt");
        result.Should().Contain("test/pattern/file2.txt");
        result.Should().NotContain("test/pattern/document.doc");
        result.Should().NotContain("test/pattern/readme.md");
    }

    [Fact]
    public async Task GetMetadataAsync_WithStoredMetadata_ReturnsMetadata()
    {
        // Arrange
        var key = "test/metadata-key";
        var data = Encoding.UTF8.GetBytes("test data");
        var metadata = new Dictionary<string, object>
        {
            ["contentType"] = "text/plain",
            ["size"] = data.Length,
            ["createdAt"] = DateTime.UtcNow.ToString()
        };

        await _storageProvider.StoreAsync(key, data, metadata);

        // Act
        var result = await _storageProvider.GetMetadataAsync(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("contentType");
        result.Should().ContainKey("size");
        result.Should().ContainKey("createdAt");
        result["contentType"].Should().Be("text/plain");
        result["size"].Should().Be(data.Length);
    }

    [Fact]
    public async Task GetMetadataAsync_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = "test/non-existing-metadata-key";

        // Act
        var result = await _storageProvider.GetMetadataAsync(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreAsync_CreatesDirectoryStructure()
    {
        // Arrange
        var key = "deep/nested/directory/structure/file.txt";
        var data = Encoding.UTF8.GetBytes("test data");

        // Act
        var result = await _storageProvider.StoreAsync(key, data);

        // Assert
        result.Should().BeTrue();
        
        var filePath = Path.Combine(_tempDirectory, key.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(filePath).Should().BeTrue();
        
        var directoryPath = Path.GetDirectoryName(filePath);
        Directory.Exists(directoryPath).Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_OverwritesExistingFile()
    {
        // Arrange
        var key = "test/overwrite-key";
        var originalData = Encoding.UTF8.GetBytes("original data");
        var newData = Encoding.UTF8.GetBytes("new data");

        await _storageProvider.StoreAsync(key, originalData);

        // Act
        var result = await _storageProvider.StoreAsync(key, newData);

        // Assert
        result.Should().BeTrue();
        
        var retrievedData = await _storageProvider.GetAsync(key);
        retrievedData.Should().BeEquivalentTo(newData);
        retrievedData.Should().NotBeEquivalentTo(originalData);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public async Task StoreAsync_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");

        // Act & Assert
        Func<Task> act = async () => await _storageProvider.StoreAsync(key, data);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSizeAsync_WithExistingKey_ReturnsCorrectSize()
    {
        // Arrange
        var key = "test/size-key";
        var data = Encoding.UTF8.GetBytes("test data for size calculation");
        var expectedSize = data.Length;

        await _storageProvider.StoreAsync(key, data);

        // Act
        var result = await _storageProvider.GetSizeAsync(key);

        // Assert
        result.Should().Be(expectedSize);
    }

    [Fact]
    public async Task GetSizeAsync_WithNonExistingKey_ReturnsMinusOne()
    {
        // Arrange
        var key = "test/non-existing-size-key";

        // Act
        var result = await _storageProvider.GetSizeAsync(key);

        // Assert
        result.Should().Be(-1);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}