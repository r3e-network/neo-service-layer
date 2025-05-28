using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Tee.Host.Services;
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;

namespace NeoServiceLayer.Services.Oracle.Tests;

/// <summary>
/// Unit tests for Oracle Service data source management functionality.
/// </summary>
public class DataSourceManagementTests
{
    private readonly Mock<ILogger<OracleService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainClientFactory;
    private readonly Mock<IBlockchainClient> _mockBlockchainClient;
    private readonly OracleService _oracleService;

    public DataSourceManagementTests()
    {
        _mockLogger = new Mock<ILogger<OracleService>>();
        _mockConfiguration = new Mock<IServiceConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockBlockchainClientFactory = new Mock<IBlockchainClientFactory>();
        _mockBlockchainClient = new Mock<IBlockchainClient>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x.GetValue("Oracle:MaxConcurrentRequests", "10")).Returns("10");
        _mockConfiguration.Setup(x => x.GetValue("Oracle:DefaultTimeout", "30000")).Returns("30000");
        _mockConfiguration.Setup(x => x.GetValue("Oracle:MaxRequestsPerBatch", "10")).Returns("10");

        // Setup blockchain client factory
        _mockBlockchainClientFactory.Setup(x => x.CreateClient(It.IsAny<BlockchainType>()))
            .Returns(_mockBlockchainClient.Object);

        // Setup blockchain client
        _mockBlockchainClient.Setup(x => x.GetBlockHeightAsync()).ReturnsAsync(1000000L);
        _mockBlockchainClient.Setup(x => x.GetBlockHashAsync(It.IsAny<long>()))
            .ReturnsAsync("0x1234567890abcdef");

        // Setup enclave manager
        _mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);

        _oracleService = new OracleService(
            _mockConfiguration.Object,
            _mockEnclaveManager.Object,
            _mockBlockchainClientFactory.Object,
            _mockLogger.Object);

        // Initialize the enclave and start the service for testing
        _oracleService.InitializeEnclaveAsync().Wait();
        _oracleService.StartAsync().Wait();
    }

    [Fact]
    public async Task RegisterDataSourceAsync_ValidHttpsUrl_HandlesNetworkFailureGracefully()
    {
        // Arrange
        var dataSource = "https://api.example.com/data";
        var description = "Test data source";

        // Act & Assert
        // Since we're testing with a mock URL that doesn't exist, we expect a network failure
        // The service should handle this gracefully by throwing an InvalidOperationException
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _oracleService.RegisterDataSourceAsync(dataSource, description, BlockchainType.NeoN3));

        exception.Message.Should().Contain("Failed to connect to data source");
    }

    [Fact]
    public async Task RegisterDataSourceAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidDataSource = "not-a-valid-url";
        var description = "Test data source";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.RegisterDataSourceAsync(invalidDataSource, description, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterDataSourceAsync_HttpUrl_ThrowsArgumentException()
    {
        // Arrange
        var httpDataSource = "http://api.example.com/data"; // HTTP instead of HTTPS
        var description = "Test data source";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.RegisterDataSourceAsync(httpDataSource, description, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterDataSourceAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var dataSource = "https://api.example.com/data";
        var description = "Test data source";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.RegisterDataSourceAsync(dataSource, description, (BlockchainType)999));
    }

    [Fact]
    public async Task RegisterDataSourceAsync_DuplicateDataSource_HandlesNetworkFailure()
    {
        // Arrange
        var dataSource = "https://api.example.com/data";
        var description = "Test data source";

        // Act & Assert
        // Since we're testing with a mock URL that doesn't exist, both attempts will fail with network errors
        // This tests that the service handles network failures consistently
        var exception1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _oracleService.RegisterDataSourceAsync(dataSource, description, BlockchainType.NeoN3));

        var exception2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _oracleService.RegisterDataSourceAsync(dataSource, description, BlockchainType.NeoN3));

        exception1.Message.Should().Contain("Failed to connect to data source");
        exception2.Message.Should().Contain("Failed to connect to data source");
    }

    [Fact]
    public async Task RemoveDataSourceAsync_NonExistentDataSource_ReturnsFalse()
    {
        // Arrange
        var dataSource = "https://api.nonexistent.com/data";

        // Act
        var result = await _oracleService.RemoveDataSourceAsync(dataSource, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveDataSourceAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var dataSource = "https://api.example.com/data";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.RemoveDataSourceAsync(dataSource, (BlockchainType)999));
    }

    [Fact]
    public async Task GetDataSourcesAsync_ValidBlockchain_ReturnsEmptyCollection()
    {
        // Act
        var result = await _oracleService.GetDataSourcesAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No data sources registered yet
    }

    [Fact]
    public async Task GetDataSourcesAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.GetDataSourcesAsync((BlockchainType)999));
    }

    [Fact]
    public async Task UpdateDataSourceAsync_NonExistentDataSource_ReturnsFalse()
    {
        // Arrange
        var dataSource = "https://api.nonexistent.com/data";
        var description = "Updated description";

        // Act
        var result = await _oracleService.UpdateDataSourceAsync(dataSource, description, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDataSourceAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var dataSource = "https://api.example.com/data";
        var description = "Updated description";

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.UpdateDataSourceAsync(dataSource, description, (BlockchainType)999));
    }

    [Fact]
    public async Task GetSupportedDataSourcesAsync_ValidBlockchain_ReturnsEmptyCollection()
    {
        // Act
        var result = await _oracleService.GetSupportedDataSourcesAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No data sources registered yet
    }

    [Fact]
    public async Task GetSupportedDataSourcesAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _oracleService.GetSupportedDataSourcesAsync((BlockchainType)999));
    }
}
