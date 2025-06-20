using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.SmartContracts.Tests;

/// <summary>
/// Unit tests for the SmartContractsService.
/// </summary>
public class SmartContractsServiceTests : IDisposable
{
    private readonly Mock<ILogger<SmartContractsService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<ISmartContractManager> _mockNeoN3Manager;
    private readonly Mock<ISmartContractManager> _mockNeoXManager;
    private readonly SmartContractsService _service;

    public SmartContractsServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmartContractsService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockNeoN3Manager = new Mock<ISmartContractManager>();
        _mockNeoXManager = new Mock<ISmartContractManager>();

        // Setup blockchain type for managers
        _mockNeoN3Manager.Setup(x => x.BlockchainType).Returns(BlockchainType.NeoN3);
        _mockNeoXManager.Setup(x => x.BlockchainType).Returns(BlockchainType.NeoX);

        _service = new SmartContractsService(
            _mockLogger.Object,
            _mockEnclaveManager.Object,
            new[] { _mockNeoN3Manager.Object, _mockNeoXManager.Object });
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue_WhenAllManagersInitializeSuccessfully()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeTrue();
        _mockNeoN3Manager.Verify(x => x.InitializeAsync(), Times.Once);
        _mockNeoXManager.Verify(x => x.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnFalse_WhenEnclaveNotInitialized()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(false);

        // Act
        var result = await _service.InitializeAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeployContractAsync_ShouldCallCorrectManager_ForNeoN3()
    {
        // Arrange
        var contractCode = new byte[] { 0x01, 0x02, 0x03 };
        var deploymentOptions = new ContractDeploymentOptions 
        { 
            ContractName = "TestContract",
            Description = "Test contract"
        };
        var expectedResult = new ContractDeploymentResult
        {
            Success = true,
            ContractHash = "0x1234567890abcdef",
            TransactionHash = "0xabcdef1234567890"
        };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoN3Manager
            .Setup(x => x.DeployContractAsync(contractCode, deploymentOptions))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeployContractAsync(
            contractCode, 
            deploymentOptions, 
            BlockchainType.NeoN3);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNeoN3Manager.Verify(
            x => x.DeployContractAsync(contractCode, deploymentOptions), 
            Times.Once);
        _mockNeoXManager.Verify(
            x => x.DeployContractAsync(It.IsAny<byte[]>(), It.IsAny<ContractDeploymentOptions>()), 
            Times.Never);
    }

    [Fact]
    public async Task DeployContractAsync_ShouldCallCorrectManager_ForNeoX()
    {
        // Arrange
        var contractCode = new byte[] { 0x01, 0x02, 0x03 };
        var deploymentOptions = new ContractDeploymentOptions 
        { 
            ContractName = "TestContract",
            Description = "Test contract"
        };
        var expectedResult = new ContractDeploymentResult
        {
            Success = true,
            ContractHash = "0x1234567890abcdef",
            TransactionHash = "0xabcdef1234567890"
        };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager
            .Setup(x => x.DeployContractAsync(contractCode, deploymentOptions))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeployContractAsync(
            contractCode, 
            deploymentOptions, 
            BlockchainType.NeoX);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNeoXManager.Verify(
            x => x.DeployContractAsync(contractCode, deploymentOptions), 
            Times.Once);
        _mockNeoN3Manager.Verify(
            x => x.DeployContractAsync(It.IsAny<byte[]>(), It.IsAny<ContractDeploymentOptions>()), 
            Times.Never);
    }

    [Fact]
    public async Task InvokeContractAsync_ShouldReturnResult_WhenContractExists()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var methodName = "testMethod";
        var parameters = new object[] { "param1", 42 };
        var expectedResult = new ContractInvocationResult
        {
            Success = true,
            Result = "success",
            TransactionHash = "0xabcdef1234567890"
        };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoN3Manager
            .Setup(x => x.InvokeContractAsync(contractHash, methodName, parameters))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.InvokeContractAsync(
            contractHash, 
            methodName, 
            parameters, 
            BlockchainType.NeoN3);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        _mockNeoN3Manager.Verify(
            x => x.InvokeContractAsync(contractHash, methodName, parameters), 
            Times.Once);
    }

    [Fact]
    public async Task CallContractAsync_ShouldReturnResult_WhenMethodExists()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var methodName = "testMethod";
        var parameters = new object[] { "param1", 42 };
        var expectedResult = "method result";

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoN3Manager
            .Setup(x => x.CallContractAsync(contractHash, methodName, parameters))
            .ReturnsAsync(expectedResult);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.CallContractAsync(
            contractHash, 
            methodName, 
            parameters, 
            BlockchainType.NeoN3);

        // Assert
        result.Should().Be(expectedResult);
        _mockNeoN3Manager.Verify(
            x => x.CallContractAsync(contractHash, methodName, parameters), 
            Times.Once);
    }

    [Fact]
    public async Task GetContractEventsAsync_ShouldReturnEvents_WhenEventsExist()
    {
        // Arrange
        var contractHash = "0x1234567890abcdef";
        var fromBlock = 100u;
        var toBlock = 200u;
        var expectedEvents = new List<ContractEvent>
        {
            new() { EventName = "Transfer", BlockNumber = 150 },
            new() { EventName = "Mint", BlockNumber = 175 }
        };

        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoN3Manager
            .Setup(x => x.GetContractEventsAsync(contractHash, fromBlock, toBlock))
            .ReturnsAsync(expectedEvents);

        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetContractEventsAsync(
            contractHash, 
            fromBlock, 
            toBlock, 
            BlockchainType.NeoN3);

        // Assert
        result.Should().BeEquivalentTo(expectedEvents);
        _mockNeoN3Manager.Verify(
            x => x.GetContractEventsAsync(contractHash, fromBlock, toBlock), 
            Times.Once);
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetContractManagerAsync_ShouldReturnCorrectManager_ForBlockchainType(BlockchainType blockchainType)
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();

        // Act
        var result = await _service.GetContractManagerAsync(blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.BlockchainType.Should().Be(blockchainType);
    }

    [Fact]
    public async Task GetContractManagerAsync_ShouldThrowException_ForUnsupportedBlockchain()
    {
        // Arrange
        var unsupportedBlockchain = (BlockchainType)999;
        
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => _service.GetContractManagerAsync(unsupportedBlockchain));
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics_WhenServiceRunning()
    {
        // Arrange
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Perform some operations to generate metrics
        await _service.GetContractManagerAsync(BlockchainType.NeoN3);

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
        _mockEnclaveManager.Setup(x => x.IsEnclaveInitialized()).Returns(true);
        _mockNeoN3Manager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockNeoXManager.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        
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
        _service.Name.Should().Be("Smart Contracts");
        _service.Description.Should().Contain("Smart contract");
        _service.Version.Should().NotBeNullOrEmpty();
        _service.Capabilities.Should().Contain(typeof(ISmartContractsService));
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}