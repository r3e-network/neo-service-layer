using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Tests for the ServiceMetricsCollector class.
/// </summary>
public class ServiceMetricsCollectorTests
{
    private readonly Mock<IServiceRegistry> _serviceRegistryMock;
    private readonly Mock<ILogger<ServiceMetricsCollector>> _loggerMock;
    private readonly ServiceMetricsCollector _metricsCollector;

    public ServiceMetricsCollectorTests()
    {
        _serviceRegistryMock = new Mock<IServiceRegistry>();
        _loggerMock = new Mock<ILogger<ServiceMetricsCollector>>();
        _metricsCollector = new ServiceMetricsCollector(_serviceRegistryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CollectAllMetricsAsync_ShouldReturnMetricsForAllServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("Service1");
        service1Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric1", 42 } });

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("Service2");
        service2Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric2", "Value" } });

        var services = new List<IService> { service1Mock.Object, service2Mock.Object };
        _serviceRegistryMock.Setup(r => r.GetAllServices()).Returns(services);

        // Act
        var result = await _metricsCollector.CollectAllMetricsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("Service1"));
        Assert.True(result.ContainsKey("Service2"));
        Assert.Equal(42, result["Service1"]["Metric1"]);
        Assert.Equal("Value", result["Service2"]["Metric2"]);
        service1Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
        service2Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task CollectAllMetricsAsync_ShouldHandleExceptions()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("Service1");
        service1Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric1", 42 } });

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("Service2");
        service2Mock.Setup(s => s.GetMetricsAsync()).ThrowsAsync(new Exception("Test exception"));

        var services = new List<IService> { service1Mock.Object, service2Mock.Object };
        _serviceRegistryMock.Setup(r => r.GetAllServices()).Returns(services);

        // Act
        var result = await _metricsCollector.CollectAllMetricsAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("Service1"));
        Assert.False(result.ContainsKey("Service2"));
        Assert.Equal(42, result["Service1"]["Metric1"]);
        service1Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
        service2Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task CollectServiceMetricsAsync_ShouldReturnMetricsForSpecificService()
    {
        // Arrange
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric", 42 } });
        _serviceRegistryMock.Setup(r => r.GetService("TestService")).Returns(serviceMock.Object);

        // Act
        var result = await _metricsCollector.CollectServiceMetricsAsync("TestService");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("Metric"));
        Assert.Equal(42, result["Metric"]);
        serviceMock.Verify(s => s.GetMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task CollectServiceMetricsAsync_ShouldReturnNull_WhenServiceNotFound()
    {
        // Arrange
        _serviceRegistryMock.Setup(r => r.GetService("NonExistentService")).Returns((IService?)null);

        // Act
        var result = await _metricsCollector.CollectServiceMetricsAsync("NonExistentService");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CollectServiceMetricsAsync_ShouldHandleExceptions()
    {
        // Arrange
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.GetMetricsAsync()).ThrowsAsync(new Exception("Test exception"));
        _serviceRegistryMock.Setup(r => r.GetService("TestService")).Returns(serviceMock.Object);

        // Act
        var result = await _metricsCollector.CollectServiceMetricsAsync("TestService");

        // Assert
        Assert.Null(result);
        serviceMock.Verify(s => s.GetMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task CollectMetricsByPatternAsync_ShouldReturnMetricsForMatchingServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("Service1");
        service1Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric1", 42 } });

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("Service2");
        service2Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "Metric2", "Value" } });

        var services = new List<IService> { service1Mock.Object, service2Mock.Object };
        _serviceRegistryMock.Setup(r => r.FindServicesByNamePattern("Service.*")).Returns(services);

        // Act
        var result = await _metricsCollector.CollectMetricsByPatternAsync("Service.*");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("Service1"));
        Assert.True(result.ContainsKey("Service2"));
        Assert.Equal(42, result["Service1"]["Metric1"]);
        Assert.Equal("Value", result["Service2"]["Metric2"]);
        service1Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
        service2Mock.Verify(s => s.GetMetricsAsync(), Times.Once);
    }

    [Fact]
    public void StartCollecting_ShouldStartTimer()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);

        // Act
        _metricsCollector.StartCollecting(interval);

        // Assert
        // We can't directly verify that the timer was started, but we can verify that calling it again logs a warning
        _metricsCollector.StartCollecting(interval);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StopCollecting_ShouldStopTimer()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);
        _metricsCollector.StartCollecting(interval);

        // Act
        _metricsCollector.StopCollecting();

        // Assert
        // We can't directly verify that the timer was stopped, but we can verify that calling it again logs a warning
        _metricsCollector.StopCollecting();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not running")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MetricsCollected_ShouldRaiseEvent()
    {
        // Arrange
        var eventRaised = false;
        var metrics = new Dictionary<string, IDictionary<string, object>>();
        var timestamp = DateTime.UtcNow;

        _metricsCollector.MetricsCollected += (sender, e) =>
        {
            eventRaised = true;
            Assert.Same(metrics, e.Metrics);
            Assert.Equal(timestamp, e.Timestamp);
        };

        // Act
        var eventArgs = new ServiceMetricsEventArgs(metrics, timestamp);
        var method = typeof(ServiceMetricsCollector).GetMethod("OnMetricsCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_metricsCollector, new object[] { eventArgs });

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Dispose_ShouldDisposeTimer()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(1);
        _metricsCollector.StartCollecting(interval);

        // Act
        _metricsCollector.Dispose();

        // Assert
        // We can't directly verify that the timer was disposed, but we can verify that calling StartCollecting again works
        _metricsCollector.StartCollecting(interval);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
