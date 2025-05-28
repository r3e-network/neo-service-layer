using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health.Storage;
using NeoServiceLayer.Services.Storage;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Health.Tests.Storage;

/// <summary>
/// Unit tests for the Health Storage Helper.
/// </summary>
public class HealthStorageHelperTests
{
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<ILogger<HealthStorageHelper>> _mockLogger;
    private readonly HealthStorageHelper _storageHelper;

    public HealthStorageHelperTests()
    {
        _mockStorageService = new Mock<IStorageService>();
        _mockLogger = new Mock<ILogger<HealthStorageHelper>>();
        _storageHelper = new HealthStorageHelper(_mockStorageService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoadMonitoredNodesAsync_ValidData_ReturnsNodes()
    {
        // Arrange
        var testNodes = new Dictionary<string, NodeHealthReport>
        {
            ["node1"] = new NodeHealthReport
            {
                NodeAddress = "node1",
                Status = NodeStatus.Online,
                UptimePercentage = 99.5
            }
        };

        var jsonData = JsonSerializer.Serialize(testNodes);
        var data = System.Text.Encoding.UTF8.GetBytes(jsonData);

        _mockStorageService.Setup(x => x.RetrieveDataAsync("health:nodes", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadMonitoredNodesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["node1"].NodeAddress.Should().Be("node1");
        result["node1"].Status.Should().Be(NodeStatus.Online);
    }

    [Fact]
    public async Task LoadMonitoredNodesAsync_NoData_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockStorageService.Setup(x => x.RetrieveDataAsync("health:nodes", BlockchainType.NeoN3))
            .ThrowsAsync(new KeyNotFoundException("No data found"));

        // Act
        var result = await _storageHelper.LoadMonitoredNodesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadMonitoredNodesAsync_InvalidData_ReturnsEmptyDictionary()
    {
        // Arrange
        var invalidData = System.Text.Encoding.UTF8.GetBytes("invalid json");

        _mockStorageService.Setup(x => x.RetrieveDataAsync("health:nodes", BlockchainType.NeoN3))
            .Returns(Task.FromResult(invalidData));

        // Act
        var result = await _storageHelper.LoadMonitoredNodesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadActiveAlertsAsync_ValidData_ReturnsAlerts()
    {
        // Arrange
        var testAlerts = new Dictionary<string, HealthAlert>
        {
            ["alert1"] = new HealthAlert
            {
                Id = "alert1",
                NodeAddress = "node1",
                Severity = HealthAlertSeverity.Warning,
                AlertType = "HighResponseTime",
                Message = "Test alert"
            }
        };

        var jsonData = JsonSerializer.Serialize(testAlerts);
        var data = System.Text.Encoding.UTF8.GetBytes(jsonData);

        _mockStorageService.Setup(x => x.RetrieveDataAsync("health:alerts", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadActiveAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["alert1"].Id.Should().Be("alert1");
        result["alert1"].Severity.Should().Be(HealthAlertSeverity.Warning);
    }

    [Fact]
    public async Task LoadNodeThresholdsAsync_ValidData_ReturnsThresholds()
    {
        // Arrange
        var testThresholds = new Dictionary<string, HealthThreshold>
        {
            ["node1"] = new HealthThreshold
            {
                MaxResponseTime = TimeSpan.FromSeconds(5),
                MinUptimePercentage = 95.0
            }
        };

        var jsonData = JsonSerializer.Serialize(testThresholds);
        var data = System.Text.Encoding.UTF8.GetBytes(jsonData);

        _mockStorageService.Setup(x => x.RetrieveDataAsync("health:thresholds", BlockchainType.NeoN3))
            .Returns(Task.FromResult(data));

        // Act
        var result = await _storageHelper.LoadNodeThresholdsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result["node1"].MaxResponseTime.Should().Be(TimeSpan.FromSeconds(5));
        result["node1"].MinUptimePercentage.Should().Be(95.0);
    }

    [Fact]
    public async Task PersistMonitoredNodesAsync_ValidData_CallsStorageService()
    {
        // Arrange
        var testNodes = new Dictionary<string, NodeHealthReport>
        {
            ["node1"] = new NodeHealthReport
            {
                NodeAddress = "node1",
                Status = NodeStatus.Online
            }
        };

        // Act
        await _storageHelper.PersistMonitoredNodesAsync(testNodes);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "health:nodes",
            It.IsAny<byte[]>(),
            It.Is<StorageOptions>(o => o.Encrypt && o.Compress),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task PersistActiveAlertsAsync_ValidData_CallsStorageService()
    {
        // Arrange
        var testAlerts = new Dictionary<string, HealthAlert>
        {
            ["alert1"] = new HealthAlert
            {
                Id = "alert1",
                NodeAddress = "node1",
                Severity = HealthAlertSeverity.Error
            }
        };

        // Act
        await _storageHelper.PersistActiveAlertsAsync(testAlerts);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "health:alerts",
            It.IsAny<byte[]>(),
            It.Is<StorageOptions>(o => o.Encrypt && o.Compress),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public async Task PersistNodeThresholdsAsync_ValidData_CallsStorageService()
    {
        // Arrange
        var testThresholds = new Dictionary<string, HealthThreshold>
        {
            ["node1"] = new HealthThreshold
            {
                MaxResponseTime = TimeSpan.FromSeconds(10),
                MinUptimePercentage = 90.0
            }
        };

        // Act
        await _storageHelper.PersistNodeThresholdsAsync(testThresholds);

        // Assert
        _mockStorageService.Verify(x => x.StoreDataAsync(
            "health:thresholds",
            It.IsAny<byte[]>(),
            It.Is<StorageOptions>(o => o.Encrypt && o.Compress),
            BlockchainType.NeoN3), Times.Once);
    }

    [Fact]
    public void Constructor_NullStorageService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HealthStorageHelper(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HealthStorageHelper(_mockStorageService.Object, null!));
    }

    [Fact]
    public async Task PersistMonitoredNodesAsync_StorageException_LogsError()
    {
        // Arrange
        var testNodes = new Dictionary<string, NodeHealthReport>();
        _mockStorageService.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<StorageOptions>(), It.IsAny<BlockchainType>()))
            .ThrowsAsync(new InvalidOperationException("Storage error"));

        // Act
        await _storageHelper.PersistMonitoredNodesAsync(testNodes);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error persisting monitored nodes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
