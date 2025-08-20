using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health;
using NeoServiceLayer.Services.Health.Models;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Services.Health.Tests;

/// <summary>
/// Unit tests for the Health Service.
/// </summary>
public class HealthServiceTests : IDisposable
{
    private readonly Mock<ILogger<HealthService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly HealthService _healthService;

    public HealthServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();
        _mockStorageService = new Mock<IStorageService>();
        _healthService = new HealthService(_mockLogger.Object, _mockEnclaveManager.Object, _mockStorageService.Object);

        // Initialize the service and enclave
        _healthService.InitializeAsync().Wait();
        _healthService.InitializeEnclaveAsync().Wait();
        _healthService.StartAsync().Wait();
    }

    [Fact]
    public async Task RegisterNodeForMonitoringAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new NodeRegistrationRequest
        {
            NodeAddress = "test-node-address",
            PublicKey = "test-public-key",
            IsConsensusNode = true,
            Thresholds = new HealthThreshold
            {
                MaxResponseTime = 5000, // 5 seconds in milliseconds
                MinUptimePercentage = 95.0
            }
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // Act
        var result = await _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
        _mockStorageService.Verify(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), BlockchainType.NeoN3), Times.AtLeast(1));
    }

    [Fact]
    public async Task RegisterNodeForMonitoringAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _healthService.RegisterNodeForMonitoringAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterNodeForMonitoringAsync_UnsupportedBlockchain_ThrowsNotSupportedException()
    {
        // Arrange
        var request = new NodeRegistrationRequest
        {
            NodeAddress = "test-node-address",
            PublicKey = "test-public-key"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoX));
    }

    [Fact]
    public async Task UnregisterNodeAsync_ExistingNode_ReturnsTrue()
    {
        // Arrange
        var nodeAddress = "test-node-address";
        var request = new NodeRegistrationRequest
        {
            NodeAddress = nodeAddress,
            PublicKey = "test-public-key"
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // First register the node
        await _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _healthService.UnregisterNodeAsync(nodeAddress, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnregisterNodeAsync_NonExistentNode_ReturnsFalse()
    {
        // Arrange
        var nodeAddress = "non-existent-node";

        // Act
        var result = await _healthService.UnregisterNodeAsync(nodeAddress, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetNodeHealthReportAsync_ExistingNode_ReturnsHealthReport()
    {
        // Arrange
        var nodeAddress = "test-node-address";
        var request = new NodeRegistrationRequest
        {
            NodeAddress = nodeAddress,
            PublicKey = "test-public-key"
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // First register the node
        await _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _healthService.GetNodeHealthAsync(nodeAddress, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result!.NodeAddress.Should().Be(nodeAddress);
    }

    [Fact]
    public async Task GetNodeHealthReportAsync_NonExistentNode_ReturnsNull()
    {
        // Arrange
        var nodeAddress = "non-existent-node";

        // Act
        var result = await _healthService.GetNodeHealthAsync(nodeAddress, BlockchainType.NeoN3);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetHealthThresholdAsync_ExistingNode_ReturnsTrue()
    {
        // Arrange
        var nodeAddress = "test-node-address";
        var request = new NodeRegistrationRequest
        {
            NodeAddress = nodeAddress,
            PublicKey = "test-public-key"
        };

        var newThreshold = new HealthThreshold
        {
            MaxResponseTime = 10000,
            MinUptimePercentage = 90.0
        };

        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ReturnsAsync(new StorageMetadata());

        // First register the node
        await _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3);

        // Act
        var result = await _healthService.SetHealthThresholdAsync(nodeAddress, newThreshold, BlockchainType.NeoN3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetHealthThresholdAsync_NonExistentNode_ReturnsFalse()
    {
        // Arrange
        var nodeAddress = "non-existent-node";
        var threshold = new HealthThreshold();

        // Act
        var result = await _healthService.SetHealthThresholdAsync(nodeAddress, threshold, BlockchainType.NeoN3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsAlerts()
    {
        // Act
        var result = await _healthService.GetActiveAlertsAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<HealthAlert>>();
    }

    [Fact]
    public async Task GetNetworkMetricsAsync_ReturnsNetworkMetrics()
    {
        // Act
        var result = await _healthService.GetNetworkMetricsAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.SuccessRate.Should().BeInRange(0, 1);
        result.CustomMetrics.Should().ContainKey("NetworkHealth");
    }

    [Fact]
    public void ServiceInfo_HasCorrectProperties()
    {
        // Assert
        _healthService.Name.Should().Be("HealthService");
        _healthService.Description.Should().Be("Neo N3 consensus node health monitoring service");
        _healthService.Version.Should().Be("1.0.0");
        _healthService.SupportedBlockchains.Should().Contain(BlockchainType.NeoN3);
        _healthService.SupportedBlockchains.Should().NotContain(BlockchainType.NeoX);
    }

    [Fact]
    public void SupportsBlockchain_NeoN3_ReturnsTrue()
    {
        // Act & Assert
        _healthService.SupportsBlockchain(BlockchainType.NeoN3).Should().BeTrue();
    }

    [Fact]
    public void SupportsBlockchain_NeoX_ReturnsFalse()
    {
        // Act & Assert
        _healthService.SupportsBlockchain(BlockchainType.NeoX).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterNodeForMonitoringAsync_NullNodeAddress_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new NodeRegistrationRequest
        {
            NodeAddress = null!,
            PublicKey = "test-public-key"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task RegisterNodeForMonitoringAsync_EmptyNodeAddress_ThrowsArgumentException()
    {
        // Arrange
        var request = new NodeRegistrationRequest
        {
            NodeAddress = "",
            PublicKey = "test-public-key"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _healthService.RegisterNodeForMonitoringAsync(request, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task UnregisterNodeAsync_NullNodeAddress_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _healthService.UnregisterNodeAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task UnregisterNodeAsync_EmptyNodeAddress_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _healthService.UnregisterNodeAsync("", BlockchainType.NeoN3));
    }

    [Fact]
    public async Task SetHealthThresholdAsync_NullThreshold_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _healthService.SetHealthThresholdAsync("test-node", null!, BlockchainType.NeoN3));
    }

    public void Dispose()
    {
        _healthService?.Dispose();
    }
}
