using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Health.Models;
using NeoServiceLayer.Services.Health.Monitoring;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


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
            Status = HealthStatus.Healthy,
            UptimePercentage = 99.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics
            {
                MemoryUsage = 1000000,
                CpuUsage = 50.0
            }}
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == AlertType.Performance);
        alerts.First(a => a.AlertType == AlertType.Performance).Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_LowUptime_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = HealthStatus.Healthy,
            UptimePercentage = 90.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics() }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == AlertType.Performance);
        alerts.First(a => a.AlertType == AlertType.Performance).Severity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_NodeOffline_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = HealthStatus.Unhealthy,
            UptimePercentage = 99.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics() }
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
            MinUptimePercentage = 95.0
        };

        // Act
        var alerts = _monitoringHelper.CheckThresholdsAndCreateAlerts(healthReport, threshold);

        // Assert
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.AlertType == AlertType.Connectivity);
        alerts.First(a => a.AlertType == AlertType.Connectivity).Severity.Should().Be(AlertSeverity.Critical);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_HighMemoryUsage_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = HealthStatus.Healthy,
            UptimePercentage = 99.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics
            {
                MemoryUsage = 10_000_000_000, // 10GB
                CpuUsage = 50.0
            }}
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
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
        alerts.Should().Contain(a => a.AlertType == AlertType.Resource);
        alerts.First(a => a.AlertType == AlertType.Resource).Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_HighCpuUsage_CreatesAlert()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = HealthStatus.Healthy,
            UptimePercentage = 99.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics
            {
                MemoryUsage = 1_000_000,
                CpuUsage = 90.0
            }}
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
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
        alerts.Should().Contain(a => a.AlertType == AlertType.Resource);
        alerts.First(a => a.AlertType == AlertType.Resource).Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public void CheckThresholdsAndCreateAlerts_AllThresholdsOk_ReturnsNoAlerts()
    {
        // Arrange
        var healthReport = new NodeHealthReport
        {
            NodeAddress = "test-node",
            ResponseTime = TimeSpan.FromSeconds(2),
            Status = HealthStatus.Healthy,
            UptimePercentage = 99.0,
            Metrics = new List<HealthMetrics> { new HealthMetrics
            {
                MemoryUsage = 1_000_000,
                CpuUsage = 50.0
            }}
        };

        var threshold = new HealthThreshold
        {
            MaxResponseTime = 5000,
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
                Status = HealthStatus.Healthy,
                UptimePercentage = 99.0
            },
            ["node2"] = new NodeHealthReport
            {
                NodeAddress = "node2",
                Status = HealthStatus.Healthy,
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
                Status = HealthStatus.Healthy,
                UptimePercentage = 99.0
            },
            ["node2"] = new NodeHealthReport
            {
                NodeAddress = "node2",
                Status = HealthStatus.Unhealthy,
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
