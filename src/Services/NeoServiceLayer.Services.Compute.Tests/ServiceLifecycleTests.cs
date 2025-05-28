using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute.Tests;

/// <summary>
/// Unit tests for ComputeService lifecycle and health functionality.
/// </summary>
public class ServiceLifecycleTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;

    public ServiceLifecycleTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");
    }

    [Fact]
    public async Task InitializeEnclaveAsync_Success_ReturnsTrue()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = await computeService.InitializeEnclaveAsync();

        // Assert
        result.Should().BeTrue();
        _mockEnclaveManager.Verify(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeEnclaveAsync_Failure_ReturnsFalse()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Enclave initialization failed"));

        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = await computeService.InitializeEnclaveAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetHealthAsync_ServiceNotRunning_ReturnsUnhealthy()
    {
        // Arrange
        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var health = await computeService.GetHealthAsync();

        // Assert
        health.Should().Be(ServiceHealth.NotRunning);
    }

    [Fact]
    public void ServiceInfo_HasCorrectProperties()
    {
        // Arrange
        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Assert
        computeService.Name.Should().Be("Compute");
        computeService.Description.Should().Be("High-Performance Verifiable Compute Service");
        computeService.Version.Should().Be("1.0.0");
        computeService.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        computeService.SupportedBlockchains.Should().Contain(BlockchainType.NeoX);
    }

    [Fact]
    public void SupportsBlockchain_NeoN3_ReturnsTrue()
    {
        // Arrange
        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        computeService.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_NeoX_ReturnsTrue()
    {
        // Arrange
        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        computeService.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_UnsupportedBlockchain_ReturnsFalse()
    {
        // Arrange
        var computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        computeService.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }
}
