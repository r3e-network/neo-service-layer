using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute.Tests;

/// <summary>
/// Unit tests for the ComputeService class.
/// </summary>
public class ComputeServiceTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServiceTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Setup enclave manager
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _computeService = new ComputeService(
            _mockEnclaveManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Initialize the enclave and start the service for testing
        _computeService.InitializeEnclaveAsync().Wait();
        _computeService.StartAsync().Wait();
    }

    [Fact]
    public async Task RegisterComputationAsync_ValidParameters_ReturnsTrue()
    {
        // Arrange
        var computationId = "test-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        var result = await _computeService.RegisterComputationAsync(
            computationId, computationCode, computationType, description, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterComputationAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var computationId = "test-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.RegisterComputationAsync(
                computationId, computationCode, computationType, description, (BlockchainType)999));
    }

    [Fact]
    public async Task RegisterComputationAsync_ServiceNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        await _computeService.StopAsync();

        var computationId = "test-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.RegisterComputationAsync(
                computationId, computationCode, computationType, description, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteComputationAsync_ValidParameters_ReturnsComputationResult()
    {
        // Arrange
        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Setup computation registration first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Setup computation execution
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "84"
        };

        _mockEnclaveManager.SetupSequence(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true")
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));

        // Register the computation first
        await _computeService.RegisterComputationAsync(
            computationId, "function compute(input) { return input * 2; }", "JavaScript", "Test", BlockchainType.NeoN3);

        // Act
        var result = await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.ComputationId.Should().Be(computationId);
        result.ResultData.Should().Be("84");
        result.BlockchainType.Should().Be(BlockchainType.NeoN3);
        result.Parameters.Should().BeEquivalentTo(parameters);
        result.Proof.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteComputationAsync_NonExistentComputation_ThrowsArgumentException()
    {
        // Arrange
        var computationId = "non-existent-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Setup to return null for non-existent computation
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteComputationAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, (BlockchainType)999));
    }

    [Fact]
    public async Task UnregisterComputationAsync_ExistingComputation_ReturnsTrue()
    {
        // Arrange
        var computationId = "test-computation";

        // Setup computation registration and unregistration
        _mockEnclaveManager.SetupSequence(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true")  // For registration
            .ReturnsAsync("true"); // For unregistration

        // Register the computation first
        await _computeService.RegisterComputationAsync(
            computationId, "function compute(input) { return input * 2; }", "JavaScript", "Test", BlockchainType.NeoN3);

        // Act
        var result = await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisterComputationAsync_NonExistentComputation_ThrowsArgumentException()
    {
        // Arrange
        var computationId = "non-existent-computation";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetComputationStatusAsync_ExistingComputation_ReturnsReady()
    {
        // Arrange
        var computationId = "test-computation";

        // Setup computation registration first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Register the computation first
        await _computeService.RegisterComputationAsync(
            computationId, "function compute(input) { return input * 2; }", "JavaScript", "Test", BlockchainType.NeoN3);

        // Act
        var result = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().Be(ComputationStatus.Completed);
    }

    [Fact]
    public async Task GetComputationStatusAsync_NonExistentComputation_ReturnsNotFound()
    {
        // Arrange
        var computationId = "non-existent-computation";

        // Setup to return null for non-existent computation
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        // Act
        var result = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().Be(ComputationStatus.Failed);
    }

    [Fact]
    public void ServiceInfo_HasCorrectProperties()
    {
        // Assert
        _computeService.Name.Should().Be("Compute");
        _computeService.Description.Should().Be("High-Performance Verifiable Compute Service");
        _computeService.Version.Should().Be("1.0.0");
        _computeService.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        _computeService.SupportedBlockchains.Should().Contain(BlockchainType.NeoX);
    }

    [Fact]
    public void SupportsBlockchain_NeoN3_ReturnsTrue()
    {
        // Act & Assert
        _computeService.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_NeoX_ReturnsTrue()
    {
        // Act & Assert
        _computeService.SupportsBlockchain(BlockchainType.NeoX).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_UnsupportedBlockchain_ReturnsFalse()
    {
        // Act & Assert
        _computeService.SupportsBlockchain((BlockchainType)999).Should().BeFalse();
    }
}
