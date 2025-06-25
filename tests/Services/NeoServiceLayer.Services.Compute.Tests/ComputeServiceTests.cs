using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Services.Compute.Tests;

public class ComputeServiceTests : IDisposable
{
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<ILogger<ComputeService>> _loggerMock;
    private readonly ComputeService _service;

    public ComputeServiceTests()
    {
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _loggerMock = new Mock<ILogger<ComputeService>>();

        _configurationMock
            .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);

        _enclaveManagerMock
            .Setup(e => e.InitializeEnclaveAsync())
            .ReturnsAsync(true);
        _enclaveManagerMock
            .Setup(e => e.InitializeAsync(null, default))
            .Returns(Task.CompletedTask);

        _enclaveManagerMock
            .Setup(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string script, CancellationToken token) =>
            {
                if (script.Contains("registerComputation"))
                {
                    return "true";
                }
                else if (script.Contains("unregisterComputation"))
                {
                    return "true";
                }
                else if (script.Contains("listAllComputations"))
                {
                    // Return empty list for startup calls
                    return JsonSerializer.Serialize(new List<ComputationMetadata>());
                }
                else if (script.Contains("listComputations"))
                {
                    // For listComputations(skip, take) calls, return a single test computation
                    // This simulates the enclave returning computations that were previously registered
                    var testComputation = new ComputationMetadata
                    {
                        ComputationId = "dummy-computation-for-list-test",
                        ComputationType = "JavaScript",
                        Description = "Test computation",
                        CreatedAt = DateTime.UtcNow,
                        ExecutionCount = 0,
                        AverageExecutionTimeMs = 0,
                        ComputationCode = "function compute(input) { return input * 2; }"
                    };
                    return JsonSerializer.Serialize(new List<ComputationMetadata> { testComputation });
                }
                else if (script.Contains("getComputationMetadata"))
                {
                    // Extract the computation ID from script for getComputationMetadata(id)
                    var startIndex = script.IndexOf("('") + 2;
                    var endIndex = script.IndexOf("')", startIndex);
                    var computationId = script.Substring(startIndex, endIndex - startIndex);

                    return JsonSerializer.Serialize(new ComputationMetadata
                    {
                        ComputationId = computationId,
                        ComputationType = "JavaScript",
                        Description = "Test computation",
                        CreatedAt = DateTime.UtcNow,
                        ExecutionCount = 10,
                        AverageExecutionTimeMs = 50,
                        ComputationCode = "function compute(input) { return input * 2; }"
                    });
                }
                else if (script.Contains("executeComputation"))
                {
                    return JsonSerializer.Serialize(new { success = true, result = "42" });
                }
                else if (script.Contains("verifyComputationResult"))
                {
                    return "true";
                }
                else if (script.Contains("updateComputationStats"))
                {
                    return "true";
                }
                else if (script.Contains("healthCheck"))
                {
                    return "true";
                }

                return "true"; // Default to true for any unmatched script
            });

        _enclaveManagerMock
            .Setup(e => e.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0123456789abcdef");

        _enclaveManagerMock
            .Setup(e => e.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _service = new ComputeService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        // Clean up resources if needed
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeEnclave()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        _enclaveManagerMock.Verify(e => e.InitializeAsync(null, default), Times.Once);
        Assert.True(_service.IsEnclaveInitialized);
    }

    [Fact]
    public async Task StartAsync_ShouldStartService()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldStopService()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task RegisterComputationAsync_ShouldRegisterComputation()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var computationId = $"test-computation-{Guid.NewGuid():N}";

        // Act
        var result = await _service.RegisterComputationAsync(
            computationId,
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("registerComputation")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnregisterComputationAsync_ShouldUnregisterComputation()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var computationId = $"test-computation-{Guid.NewGuid():N}";

        await _service.RegisterComputationAsync(
            computationId,
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.UnregisterComputationAsync(
            computationId,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        // Only verify the unregister call since that's what this test is about
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("unregisterComputation")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListComputationsAsync_ShouldListComputations()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var computationId = $"test-computation-{Guid.NewGuid():N}";

        // First register a computation
        await _service.RegisterComputationAsync(
            computationId,
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.ListComputationsAsync(
            0,
            10,
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("dummy-computation-for-list-test", result.First().ComputationId);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("listComputations")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComputationMetadataAsync_ShouldGetComputationMetadata()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var computationId = $"test-computation-{Guid.NewGuid():N}";

        // First register the computation
        await _service.RegisterComputationAsync(
            computationId,
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.GetComputationMetadataAsync(
            computationId,
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal(computationId, result.ComputationId);
        Assert.Equal("JavaScript", result.ComputationType);
        Assert.Equal("Test computation", result.Description);
        Assert.Equal(0, result.ExecutionCount); // Newly registered computation starts with 0
        Assert.Equal(0, result.AverageExecutionTimeMs); // Newly registered computation starts with 0
        // Note: Since the computation is found in cache, getComputationMetadata is not called on enclave
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("getComputationMetadata")), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteComputationAsync_ShouldExecuteComputation()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var computationId = $"test-computation-{Guid.NewGuid():N}";

        // First register the computation
        await _service.RegisterComputationAsync(
            computationId,
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        var parameters = new Dictionary<string, string>
        {
            { "input", "21" }
        };

        // Act
        var result = await _service.ExecuteComputationAsync(
            computationId,
            parameters,
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal(computationId, result.ComputationId);
        Assert.NotEmpty(result.ResultId); // ResultId is generated as GUID
        Assert.Equal("42", result.ResultData);
        Assert.Equal(BlockchainType.NeoN3, result.BlockchainType);
        Assert.Equal("0123456789abcdef", result.Proof);
        Assert.Equal("21", result.Parameters["input"]);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("executeComputation")), It.IsAny<CancellationToken>()), Times.Once);
        _enclaveManagerMock.Verify(e => e.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetComputationStatusAsync_ShouldGetComputationStatus()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetComputationStatusAsync(
            "test-computation",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal(ComputationStatus.Completed, result);
        // GetComputationStatusAsync may not call ExecuteJavaScriptAsync if computation is in cache
    }

    [Fact]
    public async Task VerifyComputationResultAsync_ShouldVerifyComputationResult()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var result = new ComputationResult
        {
            ComputationId = "test-computation",
            ResultId = "test-result",
            ResultData = "42",
            ExecutionTimeMs = 50,
            Timestamp = DateTime.UtcNow,
            BlockchainType = BlockchainType.NeoN3,
            Proof = "0123456789abcdef",
            Parameters = new Dictionary<string, string>
            {
                { "input", "21" }
            }
        };

        // Act
        bool isValid;
        try
        {
            isValid = await _service.VerifyComputationResultAsync(
                result,
                BlockchainType.NeoN3);
        }
        catch (Exception ex)
        {
            throw new Exception($"Verification failed with exception: {ex.Message}", ex);
        }

        // Assert
        Assert.True(isValid);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.Is<string>(s => s.Contains("verifyComputationResult")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
