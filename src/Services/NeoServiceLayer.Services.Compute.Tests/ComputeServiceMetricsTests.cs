using System.Reflection;
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
/// Unit tests for ComputeService metrics and statistics functionality.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "ComputeService")]
[Trait("Area", "Metrics")]
public class ComputeServiceMetricsTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServiceMetricsTests()
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
    public async Task ExecuteComputation_UpdatesRequestCount()
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

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");

        // Act
        await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Assert
        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        finalRequestCount.Should().Be(initialRequestCount + 1);
    }

    [Fact]
    public async Task ExecuteComputation_UpdatesSuccessCount()
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

        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Assert
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task ExecuteComputation_WithException_UpdatesFailureCount()
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

        // Setup execution to fail
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Execution failed"));

        var initialFailureCount = GetPrivateField<int>(_computeService, "_failureCount");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3));

        var finalFailureCount = GetPrivateField<int>(_computeService, "_failureCount");
        finalFailureCount.Should().Be(initialFailureCount + 1);
    }

    [Fact]
    public async Task ExecuteComputation_UpdatesLastRequestTime()
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

        var beforeExecution = DateTime.UtcNow;

        // Act
        await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Assert
        var lastRequestTime = GetPrivateField<DateTime>(_computeService, "_lastRequestTime");
        lastRequestTime.Should().BeOnOrAfter(beforeExecution);
        lastRequestTime.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetComputationStatus_UpdatesRequestAndSuccessCount()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        var status = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        status.Should().Be(ComputationStatus.Completed);

        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        finalRequestCount.Should().Be(initialRequestCount + 1);
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task RegisterComputation_UpdatesRequestAndSuccessCount()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        await _computeService.RegisterComputationAsync("test-comp", "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Assert
        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        finalRequestCount.Should().Be(initialRequestCount + 1);
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task UnregisterComputation_UpdatesRequestAndSuccessCount()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "test-computation";

        // Register computation first
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");
        await _computeService.RegisterComputationAsync(computationId, "code", "JavaScript", "desc", BlockchainType.NeoN3);

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);

        // Assert
        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        finalRequestCount.Should().Be(initialRequestCount + 1);
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task ListComputations_UpdatesRequestAndSuccessCount()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var mockComputations = new List<ComputationMetadata>
        {
            new() { ComputationId = "comp1", ComputationType = "JavaScript", Description = "Computation 1" }
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputations));

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        await _computeService.ListComputationsAsync(0, 10, BlockchainType.NeoN3);

        // Assert
        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        finalRequestCount.Should().Be(initialRequestCount + 1);
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task VerifyComputationResult_UpdatesRequestAndSuccessCount()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var result = new ComputationResult
        {
            ComputationId = "test-comp",
            ResultData = "42",
            Parameters = new Dictionary<string, string> { { "input", "21" } },
            Proof = "test-proof"
        };

        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var initialRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var initialSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        // Act
        await _computeService.VerifyComputationResultAsync(result, BlockchainType.NeoN3);

        // Assert
        var finalRequestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var finalSuccessCount = GetPrivateField<int>(_computeService, "_successCount");

        finalRequestCount.Should().Be(initialRequestCount + 1);
        finalSuccessCount.Should().Be(initialSuccessCount + 1);
    }

    [Fact]
    public async Task MultipleOperations_CalculatesCorrectSuccessRate()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        // Setup successful operations
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Perform 3 successful operations
        await _computeService.RegisterComputationAsync("comp1", "code", "JavaScript", "desc", BlockchainType.NeoN3);
        await _computeService.RegisterComputationAsync("comp2", "code", "JavaScript", "desc", BlockchainType.NeoN3);
        await _computeService.RegisterComputationAsync("comp3", "code", "JavaScript", "desc", BlockchainType.NeoN3);

        // Setup one failing operation
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed"));

        // Perform 1 failing operation
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.RegisterComputationAsync("comp4", "code", "JavaScript", "desc", BlockchainType.NeoN3));

        // Act - Trigger metrics update
        await InvokePrivateMethod(_computeService, "OnUpdateMetricsAsync");

        // Assert
        var requestCount = GetPrivateField<int>(_computeService, "_requestCount");
        var successCount = GetPrivateField<int>(_computeService, "_successCount");
        var failureCount = GetPrivateField<int>(_computeService, "_failureCount");

        requestCount.Should().Be(4);
        successCount.Should().Be(3);
        failureCount.Should().Be(1);
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
    }

    private async Task InvokePrivateMethod(object obj, string methodName)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method!.Invoke(obj, null);
        if (result is Task task)
        {
            await task;
        }
    }
}
