using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Services.KeyManagement.Tests;

public class KeyManagementServiceTests
{
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<ILogger<KeyManagementService>> _loggerMock;
    private readonly KeyManagementService _service;

    public KeyManagementServiceTests()
    {
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _loggerMock = new Mock<ILogger<KeyManagementService>>();

        _configurationMock
            .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);

        _enclaveManagerMock
            .Setup(e => e.InitializeEnclaveAsync())
            .ReturnsAsync(true);

        _enclaveManagerMock
            .Setup(e => e.KmsGenerateKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync((string keyId, string keyType, string keyUsage, bool exportable, string description) =>
                JsonSerializer.Serialize(new KeyMetadata
                {
                    KeyId = keyId,
                    KeyType = keyType,
                    KeyUsage = keyUsage,
                    Exportable = exportable,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    PublicKeyHex = "0x0123456789abcdef"
                }));

        _enclaveManagerMock
            .Setup(e => e.KmsGetKeyMetadataAsync(It.IsAny<string>()))
            .ReturnsAsync((string keyId) =>
                JsonSerializer.Serialize(new KeyMetadata
                {
                    KeyId = keyId,
                    KeyType = "Secp256k1",
                    KeyUsage = keyId == "encrypt-test-key" ? "encryption,decryption" : "signing,verification",
                    Exportable = false,
                    Description = "Test key",
                    CreatedAt = DateTime.UtcNow,
                    PublicKeyHex = "0x0123456789abcdef"
                }));

        _enclaveManagerMock
            .Setup(e => e.KmsListKeysAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(JsonSerializer.Serialize(new List<KeyMetadata>
            {
                new KeyMetadata
                {
                    KeyId = "test-key",
                    KeyType = "Secp256k1",
                    KeyUsage = "signing,verification",
                    Exportable = false,
                    Description = "Test key",
                    CreatedAt = DateTime.UtcNow,
                    PublicKeyHex = "0x0123456789abcdef"
                }
            }));

        _enclaveManagerMock
            .Setup(e => e.KmsSignDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0123456789abcdef");

        _enclaveManagerMock
            .Setup(e => e.KmsVerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _enclaveManagerMock
            .Setup(e => e.KmsEncryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0123456789abcdef");

        _enclaveManagerMock
            .Setup(e => e.KmsDecryptDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0123456789abcdef");

        _enclaveManagerMock
            .Setup(e => e.KmsDeleteKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(true);


        _service = new KeyManagementService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeEnclave()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        _enclaveManagerMock.Verify(e => e.InitializeEnclaveAsync(), Times.Once);
        Assert.True(_service.IsEnclaveInitialized);
    }

    [Fact]
    public async Task StartAsync_ShouldStartService()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldStopService()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task CreateKeyAsync_ShouldGenerateKey()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.CreateKeyAsync(
            "test-key",
            "Secp256k1",
            "signing,verification",
            false,
            "Test key",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("test-key", result.KeyId);
        Assert.Equal("Secp256k1", result.KeyType);
        Assert.Equal("signing,verification", result.KeyUsage);
        Assert.False(result.Exportable);
        Assert.Equal("Test key", result.Description);
        Assert.Equal("0x0123456789abcdef", result.PublicKeyHex);
        _enclaveManagerMock.Verify(e => e.KmsGenerateKeyAsync(
            "test-key",
            "Secp256k1",
            "signing,verification",
            false,
            "Test key"), Times.Once);
    }

    [Fact]
    public async Task GetKeyMetadataAsync_ShouldGetKeyMetadata()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetKeyMetadataAsync(
            "test-key",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("test-key", result.KeyId);
        Assert.Equal("Secp256k1", result.KeyType);
        Assert.Equal("signing,verification", result.KeyUsage);
        Assert.False(result.Exportable);
        Assert.Equal("Test key", result.Description);
        Assert.Equal("0x0123456789abcdef", result.PublicKeyHex);
        _enclaveManagerMock.Verify(e => e.KmsGetKeyMetadataAsync("test-key"), Times.Once);
    }

    [Fact]
    public async Task ListKeysAsync_ShouldListKeys()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.ListKeysAsync(
            0,
            10,
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("test-key", result.First().KeyId);
        _enclaveManagerMock.Verify(e => e.KmsListKeysAsync(0, 10), Times.Once);
    }

    [Fact]
    public async Task SignDataAsync_ShouldSignData()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Create the key first
        await _service.CreateKeyAsync(
            "test-key",
            "Secp256k1",
            "signing,verification",
            false,
            "Test key",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.SignDataAsync(
            "test-key",
            "0123456789abcdef",
            "ECDSA",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("0123456789abcdef", result);
        _enclaveManagerMock.Verify(e => e.KmsSignDataAsync(
            "test-key",
            "0123456789abcdef",
            "ECDSA"), Times.Once);
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldVerifySignature()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.VerifySignatureAsync(
            "test-key",
            "0123456789abcdef",
            "0123456789abcdef",
            "ECDSA",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.KmsVerifySignatureAsync(
            "test-key",
            "0123456789abcdef",
            "0123456789abcdef",
            "ECDSA"), Times.Once);
    }

    [Fact]
    public async Task EncryptDataAsync_ShouldEncryptData()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Create the key first
        await _service.CreateKeyAsync(
            "encrypt-test-key",
            "Secp256k1",
            "encryption,decryption",
            false,
            "Test key",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.EncryptDataAsync(
            "encrypt-test-key",
            "0123456789abcdef",
            "ECIES",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("0123456789abcdef", result);
        _enclaveManagerMock.Verify(e => e.KmsEncryptDataAsync(
            "encrypt-test-key",
            "0123456789abcdef",
            "ECIES"), Times.Once);
    }

    [Fact]
    public async Task DecryptDataAsync_ShouldDecryptData()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Create the key first
        await _service.CreateKeyAsync(
            "encrypt-test-key",
            "Secp256k1",
            "encryption,decryption",
            false,
            "Test key",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.DecryptDataAsync(
            "encrypt-test-key",
            "0123456789abcdef",
            "ECIES",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("0123456789abcdef", result);
        _enclaveManagerMock.Verify(e => e.KmsDecryptDataAsync(
            "encrypt-test-key",
            "0123456789abcdef",
            "ECIES"), Times.Once);
    }

    [Fact]
    public async Task DeleteKeyAsync_ShouldDeleteKey()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeleteKeyAsync(
            "test-key",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.KmsDeleteKeyAsync("test-key"), Times.Once);
    }
}
