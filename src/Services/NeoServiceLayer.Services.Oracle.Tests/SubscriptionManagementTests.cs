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
/// Unit tests for Oracle Service subscription management functionality.
/// </summary>
public class SubscriptionManagementTests
{
    private readonly Mock<ILogger<OracleService>> _mockLogger;
    private readonly Mock<IServiceConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IBlockchainClientFactory> _mockBlockchainClientFactory;
    private readonly Mock<IBlockchainClient> _mockBlockchainClient;
    private readonly OracleService _oracleService;

    public SubscriptionManagementTests()
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
        _mockEnclaveManager.Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"value\": 42, \"timestamp\": \"2024-01-01T00:00:00Z\"}");

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
    public async Task SubscribeToFeedAsync_ValidParameters_ReturnsSubscriptionId()
    {
        // Arrange
        var feedId = "test-feed";
        var parameters = new Dictionary<string, string>
        {
            ["interval"] = "1000", // 1 second for testing
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price"
        };
        var callbackInvoked = false;
        Task callback(string data)
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        }

        // Act
        var subscriptionId = await _oracleService.SubscribeToFeedAsync(feedId, parameters, callback);

        // Assert
        subscriptionId.Should().NotBeNullOrEmpty();
        Guid.TryParse(subscriptionId, out _).Should().BeTrue(); // Should be a valid GUID

        // Wait a bit to see if callback is invoked
        await Task.Delay(1500);
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeToFeedAsync_NoIntervalParameter_UsesDefaultInterval()
    {
        // Arrange
        var feedId = "test-feed";
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price"
        };
        Task callback(string data) => Task.CompletedTask;

        // Act
        var subscriptionId = await _oracleService.SubscribeToFeedAsync(feedId, parameters, callback);

        // Assert
        subscriptionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubscribeToFeedAsync_InvalidIntervalParameter_UsesDefaultInterval()
    {
        // Arrange
        var feedId = "test-feed";
        var parameters = new Dictionary<string, string>
        {
            ["interval"] = "invalid-number",
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price"
        };
        Task callback(string data) => Task.CompletedTask;

        // Act
        var subscriptionId = await _oracleService.SubscribeToFeedAsync(feedId, parameters, callback);

        // Assert
        subscriptionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnsubscribeFromFeedAsync_ExistingSubscription_ReturnsTrue()
    {
        // Arrange
        var feedId = "test-feed";
        var parameters = new Dictionary<string, string>
        {
            ["dataSource"] = "https://api.example.com",
            ["dataPath"] = "data.price"
        };
        Task callback(string data) => Task.CompletedTask;

        var subscriptionId = await _oracleService.SubscribeToFeedAsync(feedId, parameters, callback);

        // Act
        var result = await _oracleService.UnsubscribeFromFeedAsync(subscriptionId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeFromFeedAsync_NonExistentSubscription_ReturnsFalse()
    {
        // Arrange
        var nonExistentSubscriptionId = Guid.NewGuid().ToString();

        // Act
        var result = await _oracleService.UnsubscribeFromFeedAsync(nonExistentSubscriptionId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableFeedsAsync_ReturnsEmptyCollection()
    {
        // Act
        var result = await _oracleService.GetAvailableFeedsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No data sources registered yet
    }

    [Fact]
    public async Task GetFeedMetadataAsync_NonExistentFeed_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentFeedId = "https://api.nonexistent.com/data";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.GetFeedMetadataAsync(nonExistentFeedId));
    }

    [Fact]
    public async Task GetFeedMetadataAsync_EmptyFeedId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _oracleService.GetFeedMetadataAsync(""));
    }

    [Fact]
    public async Task GetFeedMetadataAsync_NullFeedId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _oracleService.GetFeedMetadataAsync(null!));
    }

    [Fact]
    public async Task SubscribeToFeedAsync_ServiceNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        await _oracleService.StopAsync(); // Stop the service

        var feedId = "test-feed";
        var parameters = new Dictionary<string, string>();
        Task callback(string data) => Task.CompletedTask;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _oracleService.SubscribeToFeedAsync(feedId, parameters, callback));
    }

    [Fact]
    public async Task MultipleSubscriptions_DifferentFeeds_AllWorkCorrectly()
    {
        // Arrange
        var feedId1 = "test-feed-1";
        var feedId2 = "test-feed-2";
        var parameters1 = new Dictionary<string, string>
        {
            ["interval"] = "500",
            ["dataSource"] = "https://api.example1.com"
        };
        var parameters2 = new Dictionary<string, string>
        {
            ["interval"] = "500",
            ["dataSource"] = "https://api.example2.com"
        };

        var callback1Invoked = false;
        var callback2Invoked = false;

        Task callback1(string data)
        {
            callback1Invoked = true;
            return Task.CompletedTask;
        }

        Task callback2(string data)
        {
            callback2Invoked = true;
            return Task.CompletedTask;
        }

        // Act
        var subscriptionId1 = await _oracleService.SubscribeToFeedAsync(feedId1, parameters1, callback1);
        var subscriptionId2 = await _oracleService.SubscribeToFeedAsync(feedId2, parameters2, callback2);

        // Assert
        subscriptionId1.Should().NotBeNullOrEmpty();
        subscriptionId2.Should().NotBeNullOrEmpty();
        subscriptionId1.Should().NotBe(subscriptionId2);

        // Wait for callbacks to be invoked
        await Task.Delay(1000);
        callback1Invoked.Should().BeTrue();
        callback2Invoked.Should().BeTrue();
    }
}
