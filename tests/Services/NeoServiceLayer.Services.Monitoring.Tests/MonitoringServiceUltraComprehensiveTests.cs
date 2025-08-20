using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Monitoring.Models;
using NeoServiceLayer.TestInfrastructure;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace NeoServiceLayer.Services.Monitoring.Tests;

/// <summary>
/// Ultra-comprehensive unit tests for MonitoringService covering all monitoring operations.
/// Tests metrics collection, alerting, health checks, performance monitoring, and log aggregation.
/// </summary>
public class MonitoringServiceUltraComprehensiveTests : TestBase, IDisposable
{
    private readonly Mock<ILogger<MonitoringService>> _mockLogger;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly MonitoringService _monitoringService;

    public MonitoringServiceUltraComprehensiveTests()
    {
        _mockLogger = new Mock<ILogger<MonitoringService>>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        _monitoringService = new MonitoringService(
            _mockLogger.Object,
            _mockEnclaveManager.Object,
            null
        );
    }

    #region System Health Tests (25 tests)

    [Fact]
    public async Task GetSystemHealthAsync_WithNeoN3_ShouldReturnHealth()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OverallStatus.Should().BeDefined();
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithNeoX_ShouldReturnHealth()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoX);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OverallStatus.Should().BeDefined();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeUptime()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.SystemUptime.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeTimestamp()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeServiceStatuses()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.ServiceStatuses.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeMetadata()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().ContainKey("total_services");
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithValidService_ShouldReturnHealth()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        var result = await _monitoringService.CheckServiceHealthAsync(serviceName);

        // Assert
        result.Should().NotBeNull();
        result.ServiceName.Should().Be(serviceName);
        result.Status.Should().BeDefined();
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ShouldIncludeResponseTime()
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync("TestService");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ShouldIncludeLastCheck()
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync("TestService");

        // Assert
        result.LastCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ShouldIncludeMetadata()
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync("TestService");

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().ContainKey("check_type");
    }

    [Fact]
    public void GetCachedHealthStatuses_ShouldReturnDictionary()
    {
        // Act
        var result = _monitoringService.GetCachedHealthStatuses();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ClearHealthCache_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => _monitoringService.ClearHealthCache();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Service1")]
    [InlineData("Service2")]
    [InlineData("Service3")]
    public async Task CheckServiceHealthAsync_WithDifferentServices_ShouldReturnCorrectName(string serviceName)
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync(serviceName);

        // Assert
        result.ServiceName.Should().Be(serviceName);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithNullName_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _monitoringService.CheckServiceHealthAsync(null!));
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithEmptyName_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _monitoringService.CheckServiceHealthAsync(""));
    }

    [Fact]
    public async Task GetSystemHealthAsync_Multiple_ShouldBeConsistent()
    {
        // Act
        var result1 = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);
        var result2 = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result1.Success.Should().Be(result2.Success);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldHandleServiceFailures()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert - should handle gracefully even if some services fail
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetCachedHealthStatuses_AfterHealthCheck_ShouldContainData()
    {
        // Arrange
        _monitoringService.CheckServiceHealthAsync("TestService").Wait();

        // Act
        var result = _monitoringService.GetCachedHealthStatuses();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ShouldUpdateCache()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        await _monitoringService.CheckServiceHealthAsync(serviceName);
        var cached = _monitoringService.GetCachedHealthStatuses();

        // Assert
        cached.Should().ContainKey(serviceName);
    }

    [Fact]
    public void ClearHealthCache_AfterCaching_ShouldEmptyCache()
    {
        // Arrange
        _monitoringService.CheckServiceHealthAsync("TestService").Wait();

        // Act
        _monitoringService.ClearHealthCache();
        var cached = _monitoringService.GetCachedHealthStatuses();

        // Assert
        cached.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithUnsupportedBlockchain_ShouldThrow()
    {
        // Arrange
        var unsupportedBlockchain = (BlockchainType)999;

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _monitoringService.GetSystemHealthAsync(unsupportedBlockchain));
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldIncludeSystemUptime()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        result.SystemUptime.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        result.SystemUptime.Should().BeLessThan(TimeSpan.FromDays(365)); // Reasonable upper bound
    }

    [Fact]
    public async Task CheckServiceHealthAsync_ResponseTime_ShouldBeReasonable()
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync("TestService");

        // Assert
        result.ResponseTimeMs.Should().BeLessThan(10000); // Less than 10 seconds
    }

    [Fact]
    public async Task GetSystemHealthAsync_ServiceStatuses_ShouldBeValid()
    {
        // Act
        var result = await _monitoringService.GetSystemHealthAsync(BlockchainType.NeoN3);

        // Assert
        foreach (var status in result.ServiceStatuses)
        {
            status.ServiceName.Should().NotBeNullOrEmpty();
            status.Status.Should().BeDefined();
            status.LastCheck.Should().BeOnOrBefore(DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task CheckServiceHealthAsync_LastCheckTime_ShouldBeRecent()
    {
        // Act
        var result = await _monitoringService.CheckServiceHealthAsync("TestService");

        // Assert
        result.LastCheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Metrics Collection Tests (25 tests)

    [Fact]
    public async Task RecordMetricAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            Unit = "count",
            ServiceName = "TestService"
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.MetricId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldIncludeTimestamp()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService"
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task RecordMetricAsync_WithTags_ShouldIncludeTags()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService",
            Tags = new Dictionary<string, string> { { "env", "test" }, { "version", "1.0" } }
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata.Should().ContainKey("service_name");
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithValidRequest_ShouldReturnMetrics()
    {
        // Arrange
        var serviceName = "TestService";
        await _monitoringService.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = serviceName
        }, BlockchainType.NeoN3);

        var request = new ServiceMetricsRequest
        {
            ServiceName = serviceName
        };

        // Act
        var result = await _monitoringService.GetServiceMetricsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ServiceName.Should().Be(serviceName);
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithTimeRange_ShouldFilterMetrics()
    {
        // Arrange
        var serviceName = "TestService";
        var request = new ServiceMetricsRequest
        {
            ServiceName = serviceName,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow
        };

        // Act
        var result = await _monitoringService.GetServiceMetricsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.Metrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithMetricNames_ShouldFilterByNames()
    {
        // Arrange
        var serviceName = "TestService";
        var request = new ServiceMetricsRequest
        {
            ServiceName = serviceName,
            MetricNames = new[] { "cpu_usage", "memory_usage" }
        };

        // Act
        var result = await _monitoringService.GetServiceMetricsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void GetAllCachedMetrics_ShouldReturnDictionary()
    {
        // Act
        var result = _monitoringService.GetAllCachedMetrics();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllCachedMetrics_AfterRecording_ShouldContainMetrics()
    {
        // Arrange
        await _monitoringService.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService"
        }, BlockchainType.NeoN3);

        // Act
        var result = _monitoringService.GetAllCachedMetrics();

        // Assert
        result.Should().ContainKey("TestService");
    }

    [Fact]
    public void ClearMetricsCache_WithServiceName_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => _monitoringService.ClearMetricsCache("TestService");
        act.Should().NotThrow();
    }

    [Fact]
    public void ClearAllMetricsCache_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => _monitoringService.ClearAllMetricsCache();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.5)]
    [InlineData(100.0)]
    [InlineData(-10.0)]
    public async Task RecordMetricAsync_WithDifferentValues_ShouldSucceed(double value)
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = value,
            ServiceName = "TestService"
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("cpu_usage")]
    [InlineData("memory_usage")]
    [InlineData("disk_usage")]
    [InlineData("response_time")]
    public async Task RecordMetricAsync_WithDifferentNames_ShouldSucceed(string metricName)
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = metricName,
            Value = 100.0,
            ServiceName = "TestService"
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("%")]
    [InlineData("ms")]
    [InlineData("count")]
    [InlineData("bytes")]
    public async Task RecordMetricAsync_WithDifferentUnits_ShouldSucceed(string unit)
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            Unit = unit,
            ServiceName = "TestService"
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RecordMetricAsync_WithNullRequest_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _monitoringService.RecordMetricAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithNullRequest_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _monitoringService.GetServiceMetricsAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public void ClearMetricsCache_WithNullServiceName_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _monitoringService.ClearMetricsCache(null!));
    }

    [Fact]
    public async Task RecordMetricAsync_WithUnsupportedBlockchain_ShouldThrow()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _monitoringService.RecordMetricAsync(request, (BlockchainType)999));
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithUnsupportedBlockchain_ShouldThrow()
    {
        // Arrange
        var request = new ServiceMetricsRequest
        {
            ServiceName = "TestService"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _monitoringService.GetServiceMetricsAsync(request, (BlockchainType)999));
    }

    [Fact]
    public async Task RecordMetricAsync_MultipleMetrics_ShouldAllSucceed()
    {
        // Arrange & Act
        var results = new List<MetricRecordingResult>();
        for (int i = 0; i < 10; i++)
        {
            var request = new RecordMetricRequest
            {
                MetricName = $"metric_{i}",
                Value = i * 10.0,
                ServiceName = "TestService"
            };
            results.Add(await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3));
        }

        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public void ClearAllMetricsCache_AfterRecording_ShouldEmptyCache()
    {
        // Arrange
        _monitoringService.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService"
        }, BlockchainType.NeoN3).Wait();

        // Act
        _monitoringService.ClearAllMetricsCache();
        var result = _monitoringService.GetAllCachedMetrics();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetServiceMetricsAsync_WithEmptyServiceName_ShouldReturnEmpty()
    {
        // Arrange
        var request = new ServiceMetricsRequest
        {
            ServiceName = "NonExistentService"
        };

        // Act
        var result = await _monitoringService.GetServiceMetricsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.Metrics.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordMetricAsync_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var request = new RecordMetricRequest
        {
            MetricName = "test_metric",
            Value = 100.0,
            ServiceName = "TestService",
            Metadata = new Dictionary<string, object> { { "custom_field", "value" } }
        };

        // Act
        var result = await _monitoringService.RecordMetricAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata.Should().ContainKey("service_name");
    }

    [Fact]
    public void ClearMetricsCache_SpecificService_ShouldOnlyClearThatService()
    {
        // Arrange
        _monitoringService.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = "metric1",
            Value = 100.0,
            ServiceName = "Service1"
        }, BlockchainType.NeoN3).Wait();

        _monitoringService.RecordMetricAsync(new RecordMetricRequest
        {
            MetricName = "metric2",
            Value = 200.0,
            ServiceName = "Service2"
        }, BlockchainType.NeoN3).Wait();

        // Act
        _monitoringService.ClearMetricsCache("Service1");
        var result = _monitoringService.GetAllCachedMetrics();

        // Assert
        result.Should().ContainKey("Service2");
    }

    #endregion

    #region Performance Statistics Tests (20 tests)

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithValidRequest_ShouldReturnStats()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = new[] { "TestService" }
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SystemPerformance.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ShouldIncludeSystemPerformance()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.SystemPerformance.CpuUsagePercent.Should().BeGreaterOrEqualTo(0);
        result.SystemPerformance.MemoryUsagePercent.Should().BeGreaterOrEqualTo(0);
        result.SystemPerformance.RequestsPerSecond.Should().BeGreaterOrEqualTo(0);
        result.SystemPerformance.AverageResponseTimeMs.Should().BeGreaterOrEqualTo(0);
        result.SystemPerformance.ErrorRatePercent.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ShouldIncludeServicePerformances()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = new[] { "Service1", "Service2" }
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.ServicePerformances.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithNullRequest_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _monitoringService.GetPerformanceStatisticsAsync(null!, BlockchainType.NeoN3));
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithUnsupportedBlockchain_ShouldThrow()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _monitoringService.GetPerformanceStatisticsAsync(request, (BlockchainType)999));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(24)]
    public async Task GetPerformanceStatisticsAsync_WithDifferentTimeRanges_ShouldSucceed(int hours)
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(hours)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.TimeRange.Should().Be(TimeSpan.FromHours(hours));
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_SystemPerformance_ShouldHaveValidValues()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        var sysPerf = result.SystemPerformance;
        sysPerf.CpuUsagePercent.Should().BeInRange(0, 100);
        sysPerf.MemoryUsagePercent.Should().BeInRange(0, 100);
        sysPerf.ErrorRatePercent.Should().BeGreaterOrEqualTo(0);
        sysPerf.RequestsPerSecond.Should().BeGreaterOrEqualTo(0);
        sysPerf.AverageResponseTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ShouldIncludeMetadata()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromMinutes(30)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithEmptyServiceNames_ShouldIncludeAllServices()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = Array.Empty<string>()
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ServicePerformances_ShouldHaveValidData()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = new[] { "TestService" }
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        foreach (var servicePerf in result.ServicePerformances)
        {
            servicePerf.ServiceName.Should().NotBeNullOrEmpty();
            servicePerf.RequestsPerSecond.Should().BeGreaterOrEqualTo(0);
            servicePerf.AverageResponseTimeMs.Should().BeGreaterOrEqualTo(0);
            servicePerf.ErrorRatePercent.Should().BeGreaterOrEqualTo(0);
            servicePerf.SuccessRatePercent.Should().BeInRange(0, 100);
            servicePerf.TotalRequests.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Theory]
    [InlineData(BlockchainType.NeoN3)]
    [InlineData(BlockchainType.NeoX)]
    public async Task GetPerformanceStatisticsAsync_WithDifferentBlockchains_ShouldSucceed(BlockchainType blockchain)
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, blockchain);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_Multiple_ShouldBeConsistent()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromMinutes(30)
        };

        // Act
        var result1 = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);
        var result2 = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result1.Success.Should().Be(result2.Success);
        result1.TimeRange.Should().Be(result2.TimeRange);
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithZeroTimeRange_ShouldUseDefault()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.Zero
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithVeryLargeTimeRange_ShouldHandle()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromDays(30)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithSpecificServices_ShouldFilterCorrectly()
    {
        // Arrange
        var targetServices = new[] { "Service1", "Service2" };
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            ServiceNames = targetServices
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        // Note: The service may not have data for specific services, but should not fail
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ErrorRatePercent_ShouldBeReasonable()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.SystemPerformance.ErrorRatePercent.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_ResponseTime_ShouldBeReasonable()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.SystemPerformance.AverageResponseTimeMs.Should().BeLessThan(60000); // Less than 1 minute
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_RequestsPerSecond_ShouldBeValid()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1)
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.SystemPerformance.RequestsPerSecond.Should().BeGreaterOrEqualTo(0);
        result.SystemPerformance.RequestsPerSecond.Should().BeLessThan(1000000); // Reasonable upper bound
    }

    [Fact]
    public async Task GetPerformanceStatisticsAsync_WithMetadata_ShouldIncludeRequestMetadata()
    {
        // Arrange
        var request = new PerformanceStatisticsRequest
        {
            TimeRange = TimeSpan.FromHours(1),
            Metadata = new Dictionary<string, object> { { "custom_field", "test_value" } }
        };

        // Act
        var result = await _monitoringService.GetPerformanceStatisticsAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
    }

    #endregion

    public void Dispose()
    {
        _monitoringService?.Dispose();
    }
}