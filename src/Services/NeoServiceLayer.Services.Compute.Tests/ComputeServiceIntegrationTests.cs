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
/// Integration tests for ComputeService complete workflows and scenarios.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "ComputeService")]
[Trait("Area", "Integration")]
public class ComputeServiceIntegrationTests
{
    private readonly Mock<ILogger<ComputeService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ComputeService _computeService;

    public ComputeServiceIntegrationTests()
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
    public async Task CompleteComputationWorkflow_RegisterExecuteVerify_Success()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "fibonacci-calculator";
        var computationCode = "function fibonacci(n) { return n <= 1 ? n : fibonacci(n-1) + fibonacci(n-2); }";
        var parameters = new Dictionary<string, string> { { "n", "10" } };

        // Setup registration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Setup execution
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "55" // 10th Fibonacci number
        };

        _mockEnclaveManager.SetupSequence(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true") // Registration
            .ReturnsAsync(JsonSerializer.Serialize(mockResult)); // Execution

        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("fibonacci-signature");
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        // Step 1: Register computation
        var registrationResult = await _computeService.RegisterComputationAsync(
            computationId, computationCode, "JavaScript", "Fibonacci calculator", BlockchainType.NeoN3);

        // Step 2: Execute computation
        var executionResult = await _computeService.ExecuteComputationAsync(computationId, parameters, BlockchainType.NeoN3);

        // Step 3: Verify result
        var verificationResult = await _computeService.VerifyComputationResultAsync(executionResult, BlockchainType.NeoN3);

        // Assert
        registrationResult.Should().BeTrue();
        executionResult.Should().NotBeNull();
        executionResult.ComputationId.Should().Be(computationId);
        executionResult.ResultData.Should().Be("55");
        executionResult.Proof.Should().Be("fibonacci-signature");
        verificationResult.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleComputationsWorkflow_RegisterListExecute_Success()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computations = new[]
        {
            new { Id = "add", Code = "function add(a, b) { return a + b; }", Type = "JavaScript", Desc = "Addition" },
            new { Id = "multiply", Code = "function multiply(a, b) { return a * b; }", Type = "JavaScript", Desc = "Multiplication" },
            new { Id = "power", Code = "function power(base, exp) { return Math.pow(base, exp); }", Type = "JavaScript", Desc = "Power" }
        };

        // Setup registration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        // Step 1: Register multiple computations
        foreach (var comp in computations)
        {
            var result = await _computeService.RegisterComputationAsync(comp.Id, comp.Code, comp.Type, comp.Desc, BlockchainType.NeoN3);
            result.Should().BeTrue();
        }

        // Step 2: List computations
        var mockComputationList = computations.Select(c => new ComputationMetadata
        {
            ComputationId = c.Id,
            ComputationType = c.Type,
            Description = c.Desc,
            CreatedAt = DateTime.UtcNow,
            ExecutionCount = 0,
            AverageExecutionTimeMs = 0
        }).ToList();

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputationList));

        var listedComputations = await _computeService.ListComputationsAsync(0, 10, BlockchainType.NeoN3);

        // Step 3: Execute each computation
        var executionResults = new List<ComputationResult>();
        foreach (var comp in computations)
        {
            var mockResult = new ComputationResult
            {
                ResultId = Guid.NewGuid().ToString(),
                ComputationId = comp.Id,
                ResultData = "42"
            };

            _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(JsonSerializer.Serialize(mockResult));
            _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync($"{comp.Id}-signature");

            var parameters = new Dictionary<string, string> { { "a", "6" }, { "b", "7" } };
            var result = await _computeService.ExecuteComputationAsync(comp.Id, parameters, BlockchainType.NeoN3);
            executionResults.Add(result);
        }

        // Assert
        listedComputations.Should().HaveCount(3);
        listedComputations.Select(c => c.ComputationId).Should().BeEquivalentTo(computations.Select(c => c.Id));
        executionResults.Should().HaveCount(3);
        executionResults.Should().AllSatisfy(r => r.ResultData.Should().Be("42"));
    }

    [Fact]
    public async Task ComputationLifecycleWorkflow_RegisterExecuteUnregister_Success()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "temp-computation";

        // Setup registration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        // Step 1: Register computation
        var registrationResult = await _computeService.RegisterComputationAsync(
            computationId, "function temp() { return 'temp'; }", "JavaScript", "Temporary computation", BlockchainType.NeoN3);

        // Step 2: Check status
        var statusAfterRegistration = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Step 3: Execute computation
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "temp"
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("temp-signature");

        var executionResult = await _computeService.ExecuteComputationAsync(
            computationId, new Dictionary<string, string>(), BlockchainType.NeoN3);

        // Step 4: Unregister computation
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        var unregistrationResult = await _computeService.UnregisterComputationAsync(computationId, BlockchainType.NeoN3);

        // Step 5: Check status after unregistration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to deserialize computation metadata."));

        var statusAfterUnregistration = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        registrationResult.Should().BeTrue();
        statusAfterRegistration.Should().Be(ComputationStatus.Completed);
        executionResult.Should().NotBeNull();
        executionResult.ResultData.Should().Be("temp");
        unregistrationResult.Should().BeTrue();
        statusAfterUnregistration.Should().Be(ComputationStatus.Failed);
    }

    [Fact]
    public async Task ServiceRestartWorkflow_MaintainsState()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "persistent-computation";

        // Setup registration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Register computation
        await _computeService.RegisterComputationAsync(
            computationId, "function persist() { return 'persistent'; }", "JavaScript", "Persistent computation", BlockchainType.NeoN3);

        // Act
        // Step 1: Stop service
        await _computeService.StopAsync();
        _computeService.IsRunning.Should().BeFalse();

        // Step 2: Start service again
        var mockComputationList = new List<ComputationMetadata>
        {
            new()
            {
                ComputationId = computationId,
                ComputationType = "JavaScript",
                Description = "Persistent computation",
                CreatedAt = DateTime.UtcNow,
                ExecutionCount = 0,
                AverageExecutionTimeMs = 0
            }
        };

        var mockMetadata = new ComputationMetadata
        {
            ComputationId = computationId,
            ComputationType = "JavaScript",
            Description = "Persistent computation",
            CreatedAt = DateTime.UtcNow,
            ExecutionCount = 0,
            AverageExecutionTimeMs = 0
        };

        // Setup specific mocks for different calls
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync($"listComputations(0, {int.MaxValue})", It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockComputationList));
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync($"getComputationMetadata('{computationId}')", It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockMetadata));

        await _computeService.StartAsync();
        _computeService.IsRunning.Should().BeTrue();

        // Step 3: Verify computation is still available
        var status = await _computeService.GetComputationStatusAsync(computationId, BlockchainType.NeoN3);

        // Assert
        status.Should().Be(ComputationStatus.Completed);
    }

    [Fact]
    public async Task ErrorRecoveryWorkflow_HandlesFailuresGracefully()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "error-prone-computation";

        // Setup registration to succeed
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        await _computeService.RegisterComputationAsync(
            computationId, "function errorProne() { throw new Error('Computation failed'); }", "JavaScript", "Error-prone computation", BlockchainType.NeoN3);

        // Act
        // Step 1: Attempt execution that fails
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Computation execution failed"));

        var executionException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _computeService.ExecuteComputationAsync(computationId, new Dictionary<string, string>(), BlockchainType.NeoN3));

        // Step 2: Verify service is still operational
        var healthAfterError = await _computeService.GetHealthAsync();

        // Step 3: Attempt successful execution
        var mockResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "recovered"
        };

        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockResult));
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("recovery-signature");

        var recoveryResult = await _computeService.ExecuteComputationAsync(
            computationId, new Dictionary<string, string>(), BlockchainType.NeoN3);

        // Assert
        executionException.Message.Should().Contain("Computation execution failed");
        healthAfterError.Should().Be(ServiceHealth.Healthy);
        recoveryResult.Should().NotBeNull();
        recoveryResult.ResultData.Should().Be("recovered");
    }

    [Fact]
    public async Task CrossBlockchainWorkflow_SupportsBothNeoN3AndNeoX()
    {
        // Arrange
        await _computeService.InitializeEnclaveAsync();
        await _computeService.StartAsync();

        var computationId = "cross-chain-computation";

        // Setup registration
        _mockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("true");

        // Act
        // Step 1: Register computation for Neo N3
        var neoN3Registration = await _computeService.RegisterComputationAsync(
            computationId, "function crossChain() { return 'neo-n3'; }", "JavaScript", "Cross-chain computation", BlockchainType.NeoN3);

        // Step 2: Register same computation for NeoX
        var neoXRegistration = await _computeService.RegisterComputationAsync(
            computationId + "-x", "function crossChain() { return 'neo-x'; }", "JavaScript", "Cross-chain computation", BlockchainType.NeoX);

        // Step 3: Execute on both blockchains
        var mockN3Result = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId,
            ResultData = "neo-n3-result"
        };

        var mockXResult = new ComputationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ComputationId = computationId + "-x",
            ResultData = "neo-x-result"
        };

        _mockEnclaveManager.SetupSequence(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(mockN3Result))
            .ReturnsAsync(JsonSerializer.Serialize(mockXResult));

        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("cross-chain-signature");

        var n3ExecutionResult = await _computeService.ExecuteComputationAsync(
            computationId, new Dictionary<string, string>(), BlockchainType.NeoN3);

        var xExecutionResult = await _computeService.ExecuteComputationAsync(
            computationId + "-x", new Dictionary<string, string>(), BlockchainType.NeoX);

        // Assert
        neoN3Registration.Should().BeTrue();
        neoXRegistration.Should().BeTrue();
        n3ExecutionResult.Should().NotBeNull();
        n3ExecutionResult.BlockchainType.Should().Be(BlockchainType.NeoN3);
        n3ExecutionResult.ResultData.Should().Be("neo-n3-result");
        xExecutionResult.Should().NotBeNull();
        xExecutionResult.BlockchainType.Should().Be(BlockchainType.NeoX);
        xExecutionResult.ResultData.Should().Be("neo-x-result");
    }
}
