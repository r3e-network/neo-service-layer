using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SmartContracts.NeoN3;
using NeoServiceLayer.Services.SmartContracts.NeoX;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.SmartContracts.Tests;

/// <summary>
/// Unit tests for the SmartContractsService.
/// </summary>
public class SmartContractsServiceTests : IDisposable
{
    private readonly Mock<ILogger<SmartContractsService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<NeoN3SmartContractManager> _mockNeoN3Manager;
    private readonly Mock<NeoXSmartContractManager> _mockNeoXManager;
    private readonly SmartContractsService _service;

    public SmartContractsServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmartContractsService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockNeoN3Manager = new Mock<NeoN3SmartContractManager>();
        _mockNeoXManager = new Mock<NeoXSmartContractManager>();

        // Setup blockchain type for managers
        _mockNeoN3Manager.Setup(x => x.BlockchainType).Returns(BlockchainType.NeoN3);
        _mockNeoXManager.Setup(x => x.BlockchainType).Returns(BlockchainType.NeoX);

        _service = new SmartContractsService(
            _mockConfiguration.Object,
            _mockEnclaveManager.Object,
            _mockNeoN3Manager.Object,
            _mockNeoXManager.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenAllManagersInitializeSuccessfully()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenEnclaveNotInitialized()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(false);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeployContractAsync_ShouldDeployContract_ForNeoN3()
    {
        // Arrange
        var contractCode = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new ContractDeploymentResult
        {
            TransactionHash = "0xabcdef1234567890",
            ContractHash = "0x1234567890abcdef",
            GasConsumed = 100
        };

        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockNeoN3Manager
            .Setup(x => x.DeployContractAsync(It.IsAny<ContractDeploymentRequest>()))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeployContractAsync(
            BlockchainType.NeoN3,
            contractCode);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task InvokeContractAsync_ShouldInvokeContract_ForNeoN3()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var methodName = "testMethod";
        var parameters = new object[] { "param1", 42 };
        var expectedResult = new ContractInvocationResult
        {
            TransactionHash = "0xabcdef1234567890",
            Result = "success",
            GasConsumed = 50
        };

        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockNeoN3Manager
            .Setup(x => x.InvokeContractAsync(It.IsAny<ContractInvocationRequest>()))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.InvokeContractAsync(
            BlockchainType.NeoN3,
            contractHash,
            methodName,
            parameters);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task CallContractAsync_ShouldCallContract_ForNeoX()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var methodName = "testMethod";
        var parameters = new object[] { "param1", 42 };
        var expectedResult = "method result";

        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockNeoXManager
            .Setup(x => x.CallContractAsync(It.IsAny<ContractCallRequest>()))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.CallContractAsync(
            BlockchainType.NeoX,
            contractHash,
            methodName,
            parameters);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task GetContractEventsAsync_ShouldReturnEvents()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var expectedEvents = new List<Core.SmartContracts.ContractEvent>
        {
            new() { EventName = "Transfer", BlockNumber = 150, Data = new Dictionary<string, object>() },
            new() { EventName = "Mint", BlockNumber = 175, Data = new Dictionary<string, object>() }
        };

        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockNeoN3Manager
            .Setup(x => x.GetContractEventsAsync(It.IsAny<ContractEventQuery>()))
            .ReturnsAsync(expectedEvents);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetContractEventsAsync(
            BlockchainType.NeoN3,
            contractHash,
            "Transfer",
            100,
            200);

        // Assert
        result.Should().BeEquivalentTo(expectedEvents);
    }

    [Fact]
    public async Task EstimateGasAsync_ShouldReturnGasEstimate()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var methodName = "testMethod";
        var expectedGas = 1000L;

        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _mockNeoN3Manager
            .Setup(x => x.EstimateGasAsync(It.IsAny<ContractInvocationRequest>()))
            .ReturnsAsync(expectedGas);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.EstimateGasAsync(
            BlockchainType.NeoN3,
            contractHash,
            methodName);

        // Assert
        result.Should().Be(expectedGas);
    }

    [Fact]
    public void GetManager_ShouldReturnCorrectManager()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        _service.InitializeAsync().Wait();

        // Act
        var neoN3Manager = _service.GetManager(BlockchainType.NeoN3);
        var neoXManager = _service.GetManager(BlockchainType.NeoX);

        // Assert
        neoN3Manager.Should().Be(_mockNeoN3Manager.Object);
        neoXManager.Should().Be(_mockNeoXManager.Object);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.ByBlockchain.Should().ContainKey(BlockchainType.NeoN3);
        result.ByBlockchain.Should().ContainKey(BlockchainType.NeoX);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics_WhenServiceRunning()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("TotalDeploymentRequests");
        result.Should().ContainKey("TotalInvocationRequests");
        result.Should().ContainKey("SupportedBlockchains");
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealthy_WhenServiceRunning()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetHealthAsync();

        // Assert
        result.Should().Be(ServiceHealth.Healthy);
    }

    [Fact]
    public void ServiceProperties_ShouldHaveCorrectValues()
    {
        // Assert
        _service.Name.Should().Be("SmartContracts");
        _service.Description.Should().Contain("Smart contract");
        _service.Version.Should().NotBeNullOrEmpty();
        _service.Capabilities.Should().Contain(typeof(ISmartContractsService));
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}