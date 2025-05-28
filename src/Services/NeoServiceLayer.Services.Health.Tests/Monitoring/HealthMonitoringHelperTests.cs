using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health.Monitoring;
using Xunit;

namespace NeoServiceLayer.Services.Health.Tests.Monitoring;

/// <summary>
/// Unit tests for the Health Monitoring Helper.
/// </summary>
public class HealthMonitoringHelperTests
{
    private readonly Mock<ILogger<HealthMonitoringHelper>> _mockLogger;
    private readonly HealthMonitoringHelper _monitoringHelper;

    public HealthMonitoringHelperTests()
    {
        _mockLogger = new Mock<ILogger<HealthMonitoringHelper>>();
        _monitoringHelper = new HealthMonitoringHelper(_mockLogger.Object);
    }

    [Fact]
    public async Task PerformNodeHealthCheckAsync_ValidNode_ReturnsHealthReport()
    {
        // Arrange
        var nodeAddress = "test-node-address";
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _monitoringHelper.PerformNodeHealthCheckAsync(nodeAddress, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.NodeAddress.Should().Be(nodeAddress);
        result.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ResponseTime.Should().BePositive();
        result.Metrics.Should().NotBeNull();
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_HighResponseTime_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(10),
            Status = NodeStatus.Online,
            UptimePercentage = 99.0,
            Metrics = new HealthMetrics
            {
                MemoryUsage = 1000000,
                CpuUsage = 50.0
            }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == "HighResponseTime");
        alerts.First(a => a.AlertType == "HighResponseTime").Severity.Should().Be(HealthAlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_LowUptime_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = NodeStatus.Online,
            UptimePercentage = 90.0,
            Metrics = new HealthMetrics()
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == "LowUptime");
        alerts.First(a => a.AlertType == "LowUptime").Severity.Should().Be(HealthAlertSeverity.Error);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_NodeOffline_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = NodeStatus.Offline,
            UptimePercentage = 99.0,
            Metrics = new HealthMetrics()
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == "NodeOffline");
        alerts.First(a => a.AlertType == "NodeOffline").Severity.Should().Be(HealthAlertSeverity.Critical);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_HighMemoryUsage_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = NodeStatus.Online,
            UptimePercentage = 99.0,
            Metrics = new HealthMetrics
            {
                MemoryUsage = 10_000_000_000, // 10GB
                CpuUsage = 50.0
            }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0,
            CustomThresholds = new Dictionary<string, double>
            {
                ["MaxMemoryUsage"] = 8_000_000_000.0 // 8GB
            }
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == "HighMemoryUsage");
        alerts.First(a => a.AlertType == "HighMemoryUsage").Severity.Should().Be(HealthAlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_HighCpuUsage_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = NodeStatus.Online,
            UptimePercentage = 99.0,
            Metrics = new HealthMetrics
            {
                MemoryUsage = 1_000_000,
                CpuUsage = 90.0
            }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0,
            CustomThresholds = new Dictionary<string, double>
            {
                ["MaxCpuUsage"] = 80.0
            }
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == "HighCpuUsage");
        alerts.First(a => a.AlertType == "HighCpuUsage").Severity.Should().Be(HealthAlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_AllThresholdsOk_ReturnsNoAlerts()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = NodeStatus.Online,
            UptimePercentage = 99.0,
            Metrics = new HealthMetrics
            {
                MemoryUsage = 1_000_000,
                CpuUsage = 50.0
            }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = TimeSpan.FromSeconds(5),
            MinUptimePercentage = 95.0,
            CustomThresholds = new Dictionary<string, double>
            {
                ["MaxMemoryUsage"] = 8_000_000_000.0,
                ["MaxCpuUsage"] = 80.0
            }
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public void CalculateNetworkHealth_NoNodes_ReturnsZero()
    {
        // Arrange
        var monitoredNodes = new Dictionary<string, NodeHealthReport>();

        // Act
        var result = _monitoringHelper.CalculateNetworkHealth(monitoredNodes);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateNetworkHealth_AllNodesOnline_ReturnsHighScore()
    {
        // Arrange
        var monitoredNodes = new Dictionary<string, NodeHealthReport>
        {
            ["node1"] = new NodeHealthReport
            {
                NodeAddress = "node1",
                Status = NodeStatus.Online,
                UptimePercentage = 99.0
            },
            ["node2"] = new NodeHealthReport
            {
                NodeAddress = "node2",
                Status = NodeStatus.Online,
                UptimePercentage = 98.0
            }
        };

        // Act
        var result = _monitoringHelper.CalculateNetworkHealth(monitoredNodes);

        // Assert
        result.Should().BeGreaterThan(0.9);
        result.Should().BeLessOrEqualTo(1.0);
    }

    [Fact]
    public void CalculateNetworkHealth_SomeNodesOffline_ReturnsLowerScore()
    {
        // Arrange
        var monitoredNodes = new Dictionary<string, NodeHealthReport>
        {
            ["node1"] = new NodeHealthReport
            {
                NodeAddress = "node1",
                Status = NodeStatus.Online,
                UptimePercentage = 99.0
            },
            ["node2"] = new NodeHealthReport
            {
                NodeAddress = "node2",
                Status = NodeStatus.Offline,
                UptimePercentage = 80.0
            }
        };

        // Act
        var result = _monitoringHelper.CalculateNetworkHealth(monitoredNodes);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(0.9);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HealthMonitoringHelper(null!));
    }
}
