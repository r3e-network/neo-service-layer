using System.Diagnostics;
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
/// Unit tests for ComputeService performance and concurrency.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Component", "ComputeService")]
[Trait("Area", "Performance")]
public class ComputeServicePerformanceTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServicePerformanceTests()
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
    public async Task RegisterMultipleComputations_Concurrently_CompletesWithinReasonableTime()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        const int computationCount = 100;
        var tasks = new List<Task<bool>>();
        var stopwatch = Stopwatch.StartNew();

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        for (int i = 0; i < computationCount; i++)
        {
            var computationId = $"computation-{i}";
            tasks.Add(_computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task ExecuteMultipleComputations_Concurrently_CompletesWithinReasonableTime()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        const int executionCount = 50;
        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup execution
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "42"
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");

        var tasks = new List<Task<ComputationResult>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < executionCount; i++)
        {
            var parameters = new Dictionary<string, string> { { "input", i.ToString() } };
            tasks.Add(_computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(executionCount);
        results.Should().AllSatisfy(result => result.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    [Fact]
    public async Task ConcurrentCacheOperations_ThreadSafe()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        const int operationCount = 100;
        var tasks = new List<Task>();

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act - Mix of register and unregister operations
        for (int i = 0; i < operationCount; i++)
        {
            var computationId = $"computation-{i}";

            // Register
            tasks.Add(Task.Run(async () =>
            {
                await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);
            }));

            // Unregister (some will fail, which is expected)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);
                }
                catch (ArgumentException)
                {
                    // Expected for non-existent computations
                }
            }));
        }

        // Assert - No deadlocks or exceptions
        await Task.WhenAll(tasks);
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact]
    public async Task HighVolumeMetadataRetrieval_PerformsWell()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        const int retrievalCount = 200;
        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        var tasks = new List<Task<ComputationMetadata>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < retrievalCount; i++)
        {
            tasks.Add(_computeService.GetComputationMetadataAsync(computationId, BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(retrievalCount);
        results.Should().AllSatisfy(result => result.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds (cache should help)
    }

    [Fact]
    public async Task ServiceInitialization_CompletesQuickly()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var initResult = await _computeService.InitializeEnclaveAsync();
        var startResult = await _computeService.StartAsync();

        stopwatch.Stop();

        // Assert
        initResult.Should().BeTrue();
        startResult.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task ServiceShutdown_CompletesQuickly()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var stopResult = await _computeService.StopAsync();

        stopwatch.Stop();

        // Assert
        stopResult.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete within 0.5 seconds
    }

    [Fact]
    public async Task LargeParameterSets_HandledEfficiently()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Create large parameter set
        var parameters = new Dictionary<string, string>();
        for (int i = 0; i < 1000; i++)
        {
            parameters[$"param{i}"] = $"value{i}";
        }

        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "result"
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Parameters.Should().HaveCount(1000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should handle large parameter sets efficiently
    }

    [Fact]
    public async Task MemoryUsage_RemainsStable()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var initialMemory = GC.GetTotalMemory(true);

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act - Perform many operations
        for (int i = 0; i < 1000; i++)
        {
            var computationId = $"computation-{i}";
            await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

            if (i % 2 == 0) // Unregister every other computation
            {
                await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);
            }
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // Should not increase by more than 50MB
    }

    [Fact]
    public async Task ConcurrentStatusChecks_PerformWell()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        const int statusCheckCount = 100;
        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        var tasks = new List<Task<ComputationStatus>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < statusCheckCount; i++)
        {
            tasks.Add(_computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(statusCheckCount);
        results.Should().AllSatisfy(status => status.Should().Be(ComputationStatus.Completed));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete within 3 seconds
    }
}
