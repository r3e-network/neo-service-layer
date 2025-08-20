using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Shared.Configuration;
using Xunit;

namespace NeoServiceLayer.Services.KeyManagement.Tests;

public class KeyManagementServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<KeyManagementService>> _mockLogger;
    private readonly Mock<IPersistentStorageProvider> _mockStorageProvider;
    private readonly Mock<IServiceConfiguration> _mockConfig;
    private readonly Mock<IHealthCheckService> _mockHealthCheck;
    private readonly Mock<ITelemetryCollector> _mockTelemetry;
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainFactory;
    private readonly Mock<ISecretsManager> _mockSecretsManager;
    private readonly KeyManagementService _keyManagementService;

    public KeyManagementServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<KeyManagementService>>();
        _mockStorageProvider = new Mock<IPersistentStorageProvider>();
        _mockConfig = new Mock<IServiceConfiguration>();
        _mockHealthCheck = new Mock<IHealthCheckService>();
        _mockTelemetry = new Mock<ITelemetryCollector>();
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockBlockchainFactory = new Mock<IBlockchainClientFactory>();
        _mockSecretsManager = new Mock<ISecretsManager>();

        _mockConfig.Setup(x => x.GetSetting("KeyManagement:DefaultKeySize", "2048"))
               .Returns("2048");
        _mockConfig.Setup(x => x.GetSetting("KeyManagement:EnableHSM", "false"))
               .Returns("false");

        _keyManagementService = new KeyManagementService(
            _mockLogger.Object,
            _mockStorageProvider.Object,
            _mockConfig.Object,
            _mockHealthCheck.Object,
            _mockTelemetry.Object,
            _mockHttpClient.Object,
            _mockBlockchainFactory.Object,
            _mockSecretsManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_InitializesSuccessfully()
    {
        var result = await _keyManagementService.InitializeAsync();

        result.Should().BeTrue();
        _keyManagementService.Name.Should().Be("KeyManagementService");
        _keyManagementService.ServiceType.Should().Be("KeyManagementService");
    }

    [Fact]
    public async Task GenerateKeyPairAsync_WithValidParameters_GeneratesKeyPair()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var keyType = "RSA";
        var keySize = 2048;

        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _keyManagementService.GenerateKeyPairAsync(keyId, keyType, keySize);

        // Assert
        result.Should().NotBeNull();
        result.KeyId.Should().Be(keyId);
        result.KeyType.Should().Be(keyType);
        result.KeySize.Should().Be(keySize);
        result.Status.Should().Be("Generated");

        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateKeyPairAsync_WithInvalidKeyId_ThrowsArgumentException()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = ""; // Invalid empty ID
        var keyType = "RSA";
        var keySize = 2048;

        // Act & Assert
        Func<Task> act = async () => await _keyManagementService.GenerateKeyPairAsync(keyId, keyType, keySize);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetPublicKeyAsync_WithExistingKey_ReturnsPublicKey()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var expectedPublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQE...";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/public/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/public/{keyId}"))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(expectedPublicKey));

        // Act
        var result = await _keyManagementService.GetPublicKeyAsync(keyId);

        // Assert
        result.Should().Be(expectedPublicKey);
    }

    [Fact]
    public async Task GetPublicKeyAsync_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "non-existent-key";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/public/{keyId}"))
            .ReturnsAsync(false);

        // Act
        var result = await _keyManagementService.GetPublicKeyAsync(keyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SignDataAsync_WithValidKeyAndData_ReturnsSignature()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var data = System.Text.Encoding.UTF8.GetBytes("data to sign");
        var algorithm = "SHA256withRSA";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/private/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/private/{keyId}"))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4, 5 }); // Mock private key

        // Act
        var result = await _keyManagementService.SignDataAsync(keyId, data, algorithm);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        result.Should().NotBeEquivalentTo(data);
    }

    [Fact]
    public async Task VerifySignatureAsync_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var data = System.Text.Encoding.UTF8.GetBytes("data to verify");
        var signature = new byte[] { 1, 2, 3, 4, 5 };
        var algorithm = "SHA256withRSA";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/public/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/public/{keyId}"))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("public-key-data"));

        // Act
        var result = await _keyManagementService.VerifySignatureAsync(keyId, data, signature, algorithm);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptDataAsync_WithValidKeyAndData_ReturnsEncryptedData()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var plaintext = System.Text.Encoding.UTF8.GetBytes("data to encrypt");
        var algorithm = "RSA/ECB/PKCS1Padding";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/public/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/public/{keyId}"))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("public-key-data"));

        // Act
        var result = await _keyManagementService.EncryptDataAsync(keyId, plaintext, algorithm);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        result.Should().NotBeEquivalentTo(plaintext);
    }

    [Fact]
    public async Task DecryptDataAsync_WithValidKeyAndCiphertext_ReturnsPlaintext()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var ciphertext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var algorithm = "RSA/ECB/PKCS1Padding";
        var expectedPlaintext = System.Text.Encoding.UTF8.GetBytes("decrypted data");

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/private/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/private/{keyId}"))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4, 5 }); // Mock private key

        // Act
        var result = await _keyManagementService.DecryptDataAsync(keyId, ciphertext, algorithm);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteKeyPairAsync_WithExistingKey_DeletesSuccessfully()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/private/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/public/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync($"keys/private/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.DeleteAsync($"keys/public/{keyId}"))
            .ReturnsAsync(true);

        // Act
        var result = await _keyManagementService.DeleteKeyPairAsync(keyId);

        // Assert
        result.Should().BeTrue();

        _mockStorageProvider.Verify(x => x.DeleteAsync($"keys/private/{keyId}"), Times.Once);
        _mockStorageProvider.Verify(x => x.DeleteAsync($"keys/public/{keyId}"), Times.Once);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsKeyList()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyIds = new[] { "key-001", "key-002", "key-003" };
        var keyPaths = keyIds.Select(id => $"keys/public/{id}").ToArray();

        _mockStorageProvider.Setup(x => x.ListKeysAsync("keys/public/", It.IsAny<string>()))
            .ReturnsAsync(keyPaths);

        // Act
        var result = await _keyManagementService.ListKeysAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(3);
        result.Should().Contain("key-001");
        result.Should().Contain("key-002");
        result.Should().Contain("key-003");
    }

    [Fact]
    public async Task GetKeyInfoAsync_WithExistingKey_ReturnsKeyInfo()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";
        var keyMetadata = new Dictionary<string, object>
        {
            ["KeyType"] = "RSA",
            ["KeySize"] = 2048,
            ["CreatedAt"] = DateTime.UtcNow.ToString(),
            ["Algorithm"] = "RSA"
        };

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/metadata/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.GetAsync($"keys/metadata/{keyId}"))
            .ReturnsAsync(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(keyMetadata));

        // Act
        var result = await _keyManagementService.GetKeyInfoAsync(keyId);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("KeyType");
        result.Should().ContainKey("KeySize");
        result.Should().ContainKey("CreatedAt");
        result["KeyType"].Should().Be("RSA");
        result["KeySize"].Should().Be(2048);
    }

    [Fact]
    public async Task RotateKeyAsync_WithExistingKey_RotatesSuccessfully()
    {
        // Arrange
        await _keyManagementService.InitializeAsync();
        await _keyManagementService.StartAsync();

        var keyId = "test-key-001";

        _mockStorageProvider.Setup(x => x.ExistsAsync($"keys/metadata/{keyId}"))
            .ReturnsAsync(true);
        _mockStorageProvider.Setup(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _keyManagementService.RotateKeyAsync(keyId);

        // Assert
        result.Should().BeTrue();

        // Should store new key and backup old one
        _mockStorageProvider.Verify(x => x.StoreAsync(
            It.IsAny<string>(), 
            It.IsAny<byte[]>(), 
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_DisposesResourcesProperly()
    {
        _keyManagementService.Dispose();
        _keyManagementService.Status.Should().Be("Disposed");
    }

    public void Dispose()
    {
        _keyManagementService?.Dispose();
    }
}