using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Monitoring.Models;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.TestInfrastructure;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Services.Monitoring.Tests;

public class MonitoringServiceTests : TestBase
{
    private readonly Mock<ILogger<MonitoringService>> _loggerMock;
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly MonitoringService _service;

    public MonitoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<MonitoringService>>();
        _enclaveManagerMock = new Mock<IEnclaveManager>();

        _service = new MonitoringService(
            _loggerMock.Object,
            _enclaveManagerMock.Object,
            null);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldReturnSystemHealth()
    {
        // Arrange
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.GetSystemHealthAsync(blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OverallStatus.Should().BeDefined();
        result.ServiceStatuses.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldRecordMetric()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            Unit = "count",
            ServiceName = "TestService",
            Tags = new Dictionary<string, string> { { "env", "test" } }
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.RecordMetricAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.MetricId.Should().NotBeNullOrEmpty();
        result.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetServiceMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        var serviceName = "TestService";
        var recordRequest = new RecordMetricRequest
        {
            MetricName = "response_time",
            Value = 150.5,
            Unit = "ms",
            ServiceName = serviceName
        };
        var blockchainType = BlockchainType.NeoN3;

        // Record a metric first
        await _service.RecordMetricAsync(recordRequest, blockchainType);

        var metricsRequest = new ServiceMetricsRequest
        {
            ServiceName = serviceName,
            StartTime = DateTime.UtcNow.AddMinutes(-1),
            EndTime = DateTime.UtcNow.AddMinutes(1)
        };

        // Act
        var result = await _service.GetServiceMetricsAsync(metricsRequest, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ServiceName.Should().Be(serviceName);
        result.Metrics.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAlertRuleAsync_ShouldCreateAlertRule()
    {
        // Arrange
        var request = new CreateAlertRuleRequest
        {
            RuleName = "High CPU Alert",
            ServiceName = "TestService",
            MetricName = "cpu_usage",
            Condition = AlertCondition.GreaterThan,
            Threshold = 80.0,
            Severity = Models.AlertSeverity.Warning,
            NotificationChannels = new[] { "email", "slack" }
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.CreateAlertRuleAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RuleId.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldReturnAlerts()
    {
        // Arrange
        var request = new GetAlertsRequest
        {
            ServiceName = "TestService",
            Limit = 10
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.GetActiveAlertsAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Alerts.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnLogs()
    {
        // Arrange
        var request = new GetLogsRequest
        {
            ServiceName = "TestService",
            LogLevel = MonitoringLogLevel.Information,
            Limit = 100
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.GetLogsAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.LogEntries.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = new[] { "TestService" }
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.GetPerformanceStatisticsAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SystemPerformance.Should().NotBeNull();
        result.ServicePerformances.Should().NotBeNull();
    }

    [Fact]
    public async Task StartMonitoringAsync_ShouldStartMonitoring()
    {
        // Arrange
        var request = new StartMonitoringRequest
        {
            ServiceName = "TestService",
            MonitoringInterval = TimeSpan.FromMinutes(1),
            MetricsToMonitor = new[] { "cpu_usage", "memory_usage" }
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.StartMonitoringAsync(request, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().NotBeNullOrEmpty();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task StopMonitoringAsync_ShouldStopMonitoring()
    {
        // Arrange
        var serviceName = "TestService";

        // Start monitoring first
        var startRequest = new StartMonitoringRequest
        {
            ServiceName = serviceName,
            MonitoringInterval = TimeSpan.FromMinutes(1)
        };
        await _service.StartMonitoringAsync(startRequest, BlockchainType.NeoN3);

        var stopRequest = new StopMonitoringRequest
        {
            ServiceName = serviceName
        };
        var blockchainType = BlockchainType.NeoN3;

        // Act
        var result = await _service.StopMonitoringAsync(stopRequest, blockchainType);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetCachedHealthStatuses_ShouldReturnHealthStatuses()
    {
        // Act
        var result = _service.GetCachedHealthStatuses();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ClearHealthCache_ShouldClearCache()
    {
        // Act & Assert (should not throw)
        _service.ClearHealthCache();
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ShouldReturnHealthStatus()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        var result = await _service.CheckServiceHealthAsync(serviceName);

        // Assert
        result.Should().NotBeNull();
        result.ServiceName.Should().Be(serviceName);
        result.Status.Should().BeDefined();
        result.LastCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetAllCachedMetrics_ShouldReturnMetrics()
    {
        // Act
        var result = _service.GetAllCachedMetrics();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ClearMetricsCache_WithServiceName_ShouldClearServiceCache()
    {
        // Arrange
        var serviceName = "TestService";

        // Act & Assert (should not throw)
        _service.ClearMetricsCache(serviceName);
    }

    [Fact]
    public void ClearAllMetricsCache_ShouldClearAllCache()
    {
        // Act & Assert (should not throw)
        _service.ClearAllMetricsCache();
    }
}
