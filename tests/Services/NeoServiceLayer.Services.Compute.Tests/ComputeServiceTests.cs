using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Compute.Tests;

public class ComputeServiceTests
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
                else if (script.Contains("listComputations"))
                {
                    return JsonSerializer.Serialize(new List<ComputationMetadata>
                    {
                        new ComputationMetadata
                        {
                            ComputationId = "test-computation",
                            ComputationType = "JavaScript",
                            Description = "Test computation",
                            CreatedAt = DateTime.UtcNow,
                            ExecutionCount = 10,
                            AverageExecutionTimeMs = 50
                        }
                    });
                }
                else if (script.Contains("getComputationMetadata"))
                {
                    return JsonSerializer.Serialize(new ComputationMetadata
                    {
                        ComputationId = "test-computation",
                        ComputationType = "JavaScript",
                        Description = "Test computation",
                        CreatedAt = DateTime.UtcNow,
                        ExecutionCount = 10,
                        AverageExecutionTimeMs = 50
                    });
                }
                else if (script.Contains("executeComputation"))
                {
                    return JsonSerializer.Serialize(new ComputationResult
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
                    });
                }

                return string.Empty;
            });

        _enclaveManagerMock
            .Setup(e => e.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("0123456789abcdef");

        _enclaveManagerMock
            .Setup(e => e.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _service = new ComputeService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
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

        // Act
        var result = await _service.RegisterComputationAsync(
            "test-computation",
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnregisterComputationAsync_ShouldUnregisterComputation()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.RegisterComputationAsync(
            "test-computation",
            "function compute(input) { return input * 2; }",
            "JavaScript",
            "Test computation",
            BlockchainType.NeoN3);

        // Act
        var result = await _service.UnregisterComputationAsync(
            "test-computation",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ListComputationsAsync_ShouldListComputations()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.ListComputationsAsync(
            0,
            10,
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("test-computation", result.First().ComputationId);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComputationMetadataAsync_ShouldGetComputationMetadata()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetComputationMetadataAsync(
            "test-computation",
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("test-computation", result.ComputationId);
        Assert.Equal("JavaScript", result.ComputationType);
        Assert.Equal("Test computation", result.Description);
        Assert.Equal(10, result.ExecutionCount);
        Assert.Equal(50, result.AverageExecutionTimeMs);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteComputationAsync_ShouldExecuteComputation()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var parameters = new Dictionary<string, string>
        {
            { "input", "21" }
        };

        // Act
        var result = await _service.ExecuteComputationAsync(
            "test-computation",
            parameters,
            BlockchainType.NeoN3);

        // Assert
        Assert.Equal("test-computation", result.ComputationId);
        Assert.Equal("test-result", result.ResultId);
        Assert.Equal("42", result.ResultData);
        Assert.Equal(BlockchainType.NeoN3, result.BlockchainType);
        Assert.Equal("0123456789abcdef", result.Proof);
        Assert.Equal("21", result.Parameters["input"]);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var isValid = await _service.VerifyComputationResultAsync(
            result,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(isValid);
        _enclaveManagerMock.Verify(e => e.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
