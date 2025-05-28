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
/// Unit tests for ComputeService computation management functionality.
/// </summary>
public class ComputationManagementTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputationManagementTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        // Setup enclave manager
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
    public async Task ListComputationsAsync_ValidParameters_ReturnsComputationList()
    {
        // Arrange
        var mockComputations = new List<ComputationMetadata>
        {
            new ComputationMetadata
            {
                ComputationId = "computation-1",
                ComputationType = "JavaScript",
                Description = "Test computation 1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExecutionCount = 5,
                AverageExecutionTimeMs = 100.5
            },
            new ComputationMetadata
            {
                ComputationId = "computation-2",
                ComputationType = "WebAssembly",
                Description = "Test computation 2",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExecutionCount = 10,
                AverageExecutionTimeMs = 250.0
            }
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputations));

        // Act
        var result = await _computeService.ListComputationsAsync(0, 10, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().ComputationId.Should().Be("computation-1");
        result.Last().ComputationId.Should().Be("computation-2");
    }

    [Fact]
    public async Task ListComputationsAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.ListComputationsAsync(0, 10, (BlockchainType)999));
    }

    [Fact]
    public async Task ListComputationsAsync_ServiceNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        await _computeService.StopAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ListComputationsAsync(0, 10, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetComputationMetadataAsync_ExistingComputation_ReturnsMetadata()
    {
        // Arrange
        var computationId = "test-computation";
        var mockMetadata = new ComputationMetadata
        {
            ComputationId = computationId,
            ComputationType = "JavaScript",
            Description = "Test computation",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExecutionCount = 3,
            AverageExecutionTimeMs = 150.0,
            LastUsedAt = DateTime.UtcNow.AddHours(-2)
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockMetadata));

        // Act
        var result = await _computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.ComputationId.Should().Be(computationId);
        result.ComputationType.Should().Be("JavaScript");
        result.Description.Should().Be("Test computation");
        result.ExecutionCount.Should().Be(3);
        result.AverageExecutionTimeMs.Should().Be(150.0);
    }

    [Fact]
    public async Task GetComputationMetadataAsync_NonExistentComputation_ThrowsInvalidOperationException()
    {
        // Arrange
        var computationId = "non-existent-computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetComputationMetadataAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var computationId = "test-computation";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.GetComputationMetadataAsync(computationId, (BlockchainType)999));
    }

    [Fact]
    public async Task VerifyComputationResultAsync_ValidResult_ReturnsTrue()
    {
        // Arrange
        var result = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = "test-computation",
            ResultData = "42",
            Parameters = new Dictionary<string, string> { { "input", "21" } },
            Proof = "valid-signature",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var isValid = await _computeService.VerifyComputationResultAsync(result, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyComputationResultAsync_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = "test-computation",
            ResultData = "42",
            Parameters = new Dictionary<string, string> { { "input", "21" } },
            Proof = "invalid-signature",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act
        var isValid = await _computeService.VerifyComputationResultAsync(result, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyComputationResultAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var result = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = "test-computation",
            ResultData = "42",
            Parameters = new Dictionary<string, string> { { "input", "21" } },
            Proof = "valid-signature",
            BlockchainType = BlockchainType.NeoN3
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.VerifyComputationResultAsync(result, (BlockchainType)999));
    }

    [Fact]
    public async Task RegisterComputationAsync_DuplicateComputation_ThrowsArgumentException()
    {
        // Arrange
        var computationId = "duplicate-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        // Setup first registration to succeed
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Register the computation first time
        await _computeService.RegisterComputationAsync(
            computationId, computationCode, computationType, description, BlockchainType.NeoN3);

        // Act & Assert - Try to register the same computation again
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.RegisterComputationAsync(
                computationId, computationCode, computationType, description, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterComputationAsync_EnclaveFailure_ReturnsFalse()
    {
        // Arrange
        var computationId = "failing-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("false");

        // Act
        var result = await _computeService.RegisterComputationAsync(
            computationId, computationCode, computationType, description, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnregisterComputationAsync_EnclaveFailure_ReturnsFalse()
    {
        // Arrange
        var computationId = "test-computation";

        // Setup computation registration and unregistration
        _mockEnclaveManager.SetupSequence(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true")   // For registration
            .ReturnsAsync("false"); // For unregistration (fail)

        // Register the computation first
        await _computeService.RegisterComputationAsync(
            computationId, "function compute(input) { return input * 2; }", "JavaScript", "Test", BlockchainType.NeoN3);

        // Act
        var result = await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListComputationsAsync_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var mockComputations = new List<ComputationMetadata>
        {
            new ComputationMetadata { ComputationId = "computation-3", ComputationType = "JavaScript" },
            new ComputationMetadata { ComputationId = "computation-4", ComputationType = "WebAssembly" }
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputations));

        // Act
        var result = await _computeService.ListComputationsAsync(5, 2, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
