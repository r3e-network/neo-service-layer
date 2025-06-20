using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.SecretsManagement;
using NeoServiceLayer.Services.SecretsManagement.ExternalProviders;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.SecretsManagement.Tests;

/// <summary>
/// Unit tests for the SecretsManagementService.
/// </summary>
public class SecretsManagementServiceTests : IDisposable
{
    private readonly Mock<ILogger<SecretsManagementService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IExternalSecretProvider> _mockExternalProvider;
    private readonly SecretsManagementService _service;

    public SecretsManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecretsManagementService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockExternalProvider = new Mock<IExternalSecretProvider>();

        _service = new SecretsManagementService(
            _mockLogger.Object,
            _mockEnclaveManager.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenEnclaveIsAvailable()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenEnclaveIsNotAvailable()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(false);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldReturnTrue_WhenServiceInitialized()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();

        // Act
        var result = await _service.StartAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task StoreSecretAsync_ShouldStoreSecret_WhenValidInput()
    {
        // Arrange
        var secretId = "test-secret";
        var secretValue = "test-value";
        var secretMetadata = new Dictionary<string, string> { ["type"] = "api-key" };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockEnclaveManager
            .Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.StoreSecretAsync(secretId, secretValue, secretMetadata);

        // Assert
        result.Should().BeTrue();
        _mockEnclaveManager.Verify(
            x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>()), 
            Times.Once);
    }

    [Fact]
    public async Task RetrieveSecretAsync_ShouldReturnSecret_WhenSecretExists()
    {
        // Arrange
        var secretId = "test-secret";
        var expectedSecret = "test-value";
        var encryptedData = System.Text.Encoding.UTF8.GetBytes(expectedSecret);

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockEnclaveManager
            .Setup(x => x.RetrieveDataAsync(It.IsAny<string>()))
            .ReturnsAsync(encryptedData);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.RetrieveSecretAsync(secretId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RetrieveSecretAsync_ShouldReturnFailure_WhenSecretNotFound()
    {
        // Arrange
        var secretId = "non-existent-secret";

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockEnclaveManager
            .Setup(x => x.RetrieveDataAsync(It.IsAny<string>()))
            .ReturnsAsync((byte[])null!);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.RetrieveSecretAsync(secretId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSecretAsync_ShouldReturnTrue_WhenSecretDeleted()
    {
        // Arrange
        var secretId = "test-secret";

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockEnclaveManager
            .Setup(x => x.DeleteDataAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeleteSecretAsync(secretId);

        // Assert
        result.Should().BeTrue();
        _mockEnclaveManager.Verify(
            x => x.DeleteDataAsync(secretId), 
            Times.Once);
    }

    [Fact]
    public async Task ListSecretsAsync_ShouldReturnSecretsList_WhenSecretsExist()
    {
        // Arrange
        var expectedSecrets = new List<string> { "secret1", "secret2", "secret3" };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockEnclaveManager
            .Setup(x => x.ListStoredKeysAsync())
            .ReturnsAsync(expectedSecrets);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.ListSecretsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(expectedSecrets.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StoreSecretAsync_ShouldThrowException_WhenSecretIdInvalid(string invalidSecretId)
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.StoreSecretAsync(invalidSecretId, "value", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StoreSecretAsync_ShouldThrowException_WhenSecretValueInvalid(string invalidSecretValue)
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.StoreSecretAsync("valid-id", invalidSecretValue, null));
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthy_WhenServiceRunning()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        result.Should().Be(ServiceHealth.Healthy);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnUnhealthy_WhenServiceNotRunning()
    {
        // Arrange
        // Service not started

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        result.Should().Be(ServiceHealth.NotRunning);
    }

    [Fact]
    public void ServiceProperties_ShouldHaveCorrectValues()
    {
        // Assert
        _service.Name.Should().Be("Secrets Management");
        _service.Description.Should().Contain("secure storage");
        _service.Version.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}