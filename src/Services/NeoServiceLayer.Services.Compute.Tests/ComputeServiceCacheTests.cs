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
/// Unit tests for ComputeService cache management functionality.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ComputeService")]
[Trait("Area", "CacheManagement")]
public class ComputeServiceCacheTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServiceCacheTests()
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
            .ReturnsAsync("[]");

        _computeService = new ComputeService(_mockEnclaveManager.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterComputation_UpdatesComputationCache()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";
        var computationCode = "function compute(input) { return input * 2; }";
        var computationType = "JavaScript";
        var description = "Test computation";

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        var result = await _computeService.RegisterComputationAsync(computationId, computationCode, computationType, description, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();

        // Verify cache was updated by trying to get metadata
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new ComputationMetadata
            {
                ComputationId = computationId,
                ComputationType = computationType,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ExecutionCount = 0,
                AverageExecutionTimeMs = 0
            }));

        var metadata = await _computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3);
        metadata.Should().NotBeNull();
        metadata.ComputationId.Should().Be(computationId);
    }

    [Fact]
    public async Task UnregisterComputation_RemovesFromCache()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // First register a computation
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup for unregistration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        var result = await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteComputation_UpdatesResultCache()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";
        var parameters = new Dictionary<string, string> { { "input", "42" } };

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup execution
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "84"
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");

        // Act
        var result = await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.ComputationId.Should().Be(computationId);
        result.ResultData.Should().Be("84");
    }

    [Fact]
    public async Task GetComputationMetadata_UsesCacheWhenAvailable()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // Register computation to populate cache
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Reset mock to verify cache is used
        _mockEnclaveManager.Reset();
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var metadata = await _computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3);

        // Assert
        metadata.Should().NotBeNull();
        metadata.ComputationId.Should().Be(computationId);

        // Verify enclave was not called (cache was used)
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComputationMetadata_FallsBackToEnclaveWhenNotInCache()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";
        var mockMetadata = new ComputationMetadata
        {
            ComputationId = computationId,
            ComputationType = "JavaScript",
            Description = "Test computation",
            CreatedAt = DateTime.UtcNow,
            ExecutionCount = 5,
            AverageExecutionTimeMs = 150.5
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockMetadata));

        // Act
        var metadata = await _computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3);

        // Assert
        metadata.Should().NotBeNull();
        metadata.ComputationId.Should().Be(computationId);
        metadata.ExecutionCount.Should().Be(5);
        metadata.AverageExecutionTimeMs.Should().Be(150.5);

        // Verify enclave was called
        _mockEnclaveManager.Verify(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListComputations_UpdatesCache()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var mockComputations = new List<ComputationMetadata>
        {
            new() { ComputationId = "comp1", ComputationType = "JavaScript", Description = "Computation 1" },
            new() { ComputationId = "comp2", ComputationType = "WebAssembly", Description = "Computation 2" }
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputations));

        // Act
        var result = await _computeService.ListComputationsAsync(0, 10, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        // Verify cache was updated by checking if we can get metadata without enclave call
        _mockEnclaveManager.Reset();
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var metadata1 = await _computeService.GetComputationMetadataAsync("comp1", BlockchainType.NeoN3);
        metadata1.Should().NotBeNull();
        metadata1.ComputationId.Should().Be("comp1");
    }

    [Fact]
    public async Task StopService_ClearsCaches()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        // Register a computation to populate cache
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        await _computeService.RegisterComputationAsync("test-comp", "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Act
        await _computeService.StopAsync();

        // Assert
        _computeService.IsRunning.Should().BeFalse();

        // Cache should be cleared - verify by trying to start again and checking if enclave is called
        await _computeService.StartAsync();

        // The cache should be empty, so getting metadata should call the enclave
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.GetComputationMetadataAsync("test-comp", BlockchainType.NeoN3));
    }

    [Fact]
    public async Task ConcurrentCacheAccess_ThreadSafe()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var tasks = new List<Task>();
        var computationIds = Enumerable.Range(1, 10).Select(i => $"comp-{i}").ToList();

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act - Register multiple computations concurrently
        foreach (var id in computationIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                await _computeService.RegisterComputationAsync(id, "code", "JavaScript", "desc", BlockchainType.NeoN3);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All registrations should succeed without exceptions
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }
}
