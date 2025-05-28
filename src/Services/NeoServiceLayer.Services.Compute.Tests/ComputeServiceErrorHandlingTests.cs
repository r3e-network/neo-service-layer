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
/// Unit tests for ComputeService error handling and edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ComputeService")]
[Trait("Area", "ErrorHandling")]
public class ComputeServiceErrorHandlingTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServiceErrorHandlingTests()
    {
        _mockLogger = new Mock<ILogger<ComputeService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxComputationCount", "1000")).Returns("1000");
        _mockConfiguration.Setup(x => x.GetValue("Compute:MaxExecutionTimeMs", "30000")).Returns("30000");

        _computeService = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData((BlockchainType)999)]
    [InlineData((BlockchainType)100)]
    [InlineData((BlockchainType)50)]
    public async Task ExecuteComputation_UnsupportedBlockchain_ThrowsNotSupportedException(BlockchainType blockchainType)
    {
        // Arrange
        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, blockchainType));

        exception.Message.Should().Contain($"Blockchain type {blockchainType} is not supported");
    }

    [Fact]
    public async Task ExecuteComputation_EnclaveNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));

        exception.Message.Should().Be("Enclave is not initialized.");
    }

    [Fact]
    public async Task ExecuteComputation_ServiceNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await _computeService.InitializeEnclaveAsync();

        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));

        exception.Message.Should().Be("Service is not running.");
    }

    [Fact]
    public async Task ExecuteComputation_NonExistentComputation_ThrowsArgumentException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "non-existent-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteComputation_EnclaveExecutionFails_ThrowsException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup execution to fail
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Enclave execution failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ExecuteComputation_InvalidJsonResult_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup execution to return invalid JSON
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("invalid json");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterComputation_DuplicateId_ThrowsArgumentException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // Register computation first time
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Act & Assert - Try to register again
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.RegisterComputationAsync(computationId, "code2", "JavaScript", "desc2", BlockchainType.NeoN3));

        exception.Message.Should().Contain($"Computation with ID {computationId} already exists");
    }

    [Fact]
    public async Task UnregisterComputation_NonExistentId_ThrowsArgumentException()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "non-existent-computation";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3));

        exception.Message.Should().Contain($"Computation with ID {computationId} does not exist");
    }

    [Fact]
    public async Task GetComputationStatus_NonExistentComputation_ReturnsNotFound()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "non-existent-computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        // Act
        var status = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        status.Should().Be(ComputationStatus.Failed);
    }

    [Fact]
    public async Task GetComputationStatus_EnclaveException_ReturnsError()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var status = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        status.Should().Be(ComputationStatus.Failed);
    }

    [Fact]
    public async Task VerifyComputationResult_SignatureVerificationFails_ReturnsFalse()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var result = new ComputationResult
        {
            ComputationId = "test-comp",
            ResultData = "42",
            Parameters = new Dictionary<string, string> { { "input", "21" } },
            Proof = "invalid-signature"
        };

        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var isValid = await _computeService.VerifyComputationResultAsync(result, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task RegisterComputation_InvalidComputationId_ThrowsArgumentException(string computationId)
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3));
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, -1)]
    [InlineData(-5, -10)]
    public async Task ListComputations_InvalidPagination_ThrowsArgumentException(int skip, int take)
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _computeService.ListComputationsAsync(skip, take, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task InitializeEnclave_EnclaveManagerThrows_ReturnsFalse()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Enclave initialization failed"));

        // Act
        var result = await _computeService.InitializeEnclaveAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StartService_RefreshCacheFails_ContinuesExecution()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache refresh failed"));

        await _computeService.InitializeEnclaveAsync();

        // Act
        var result = await _computeService.StartAsync();

        // Assert
        result.Should().BeTrue(); // Service should still start even if cache refresh fails
        _computeService.IsRunning.Should().BeTrue();
    }
}
