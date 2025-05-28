using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.ServiceFramework;
using System.Text.Json;

namespace NeoServiceLayer.Api.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for ServiceController covering all API endpoints.
/// Tests service management, health checks, metrics, and error handling.
/// </summary>
public class ServiceControllerTests : IDisposable
{
    private readonly Mock<ILogger<ServiceController>> _mockLogger;
    private readonly Mock<IServiceRegistry> _mockServiceRegistry;
    private readonly ServiceController _controller;

    public ServiceControllerTests()
    {
        _mockLogger = new Mock<ILogger<ServiceController>>();
        _mockServiceRegistry = new Mock<IServiceRegistry>();

        SetupServiceRegistry();

        _controller = new ServiceController(_mockLogger.Object, _mockServiceRegistry.Object);
    }

    #region Service Management Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task GetServicesAsync_ValidRequest_ReturnsServiceList()
    {
        // Act
        var result = await _controller.GetServicesAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var services = okResult.Value.Should().BeAssignableTo<IEnumerable<ServiceInfo>>().Subject;
        services.Should().HaveCount(3);
        services.Should().Contain(s => s.Name == "TestService1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task GetServiceAsync_ExistingService_ReturnsService()
    {
        // Arrange
        const string serviceName = "TestService1";

        // Act
        var result = await _controller.GetServiceAsync(serviceName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var service = okResult.Value.Should().BeAssignableTo<ServiceInfo>().Subject;
        service.Name.Should().Be(serviceName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task GetServiceAsync_NonExistentService_ReturnsNotFound()
    {
        // Arrange
        const string nonExistentService = "NonExistentService";

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(nonExistentService))
            .ReturnsAsync((IService?)null);

        // Act
        var result = await _controller.GetServiceAsync(nonExistentService);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task StartServiceAsync_ValidService_StartsSuccessfully()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StartServiceAsync(serviceName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        mockService.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task StopServiceAsync_ValidService_StopsSuccessfully()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StopServiceAsync(serviceName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        mockService.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceManagement")]
    public async Task RestartServiceAsync_ValidService_RestartsSuccessfully()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockService
            .Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RestartServiceAsync(serviceName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        mockService.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockService.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "HealthChecks")]
    public async Task GetServiceHealthAsync_HealthyService_ReturnsHealthy()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.GetHealthAsync())
            .ReturnsAsync(ServiceHealth.Healthy);

        // Act
        var result = await _controller.GetServiceHealthAsync(serviceName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value.Should().BeAssignableTo<ServiceHealthInfo>().Subject;
        health.Status.Should().Be(ServiceHealth.Healthy);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "HealthChecks")]
    public async Task GetServiceHealthAsync_UnhealthyService_ReturnsUnhealthy()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.GetHealthAsync())
            .ReturnsAsync(ServiceHealth.Unhealthy);

        // Act
        var result = await _controller.GetServiceHealthAsync(serviceName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value.Should().BeAssignableTo<ServiceHealthInfo>().Subject;
        health.Status.Should().Be(ServiceHealth.Unhealthy);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "HealthChecks")]
    public async Task GetOverallHealthAsync_AllServicesHealthy_ReturnsHealthy()
    {
        // Arrange
        SetupHealthyServices();

        // Act
        var result = await _controller.GetOverallHealthAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value.Should().BeAssignableTo<OverallHealthInfo>().Subject;
        health.Status.Should().Be(ServiceHealth.Healthy);
        health.HealthyServices.Should().Be(3);
        health.UnhealthyServices.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "HealthChecks")]
    public async Task GetOverallHealthAsync_SomeServicesUnhealthy_ReturnsDegraded()
    {
        // Arrange
        SetupMixedHealthServices();

        // Act
        var result = await _controller.GetOverallHealthAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var health = okResult.Value.Should().BeAssignableTo<OverallHealthInfo>().Subject;
        health.Status.Should().Be(ServiceHealth.Degraded);
        health.HealthyServices.Should().Be(2);
        health.UnhealthyServices.Should().Be(1);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Metrics")]
    public async Task GetServiceMetricsAsync_ValidService_ReturnsMetrics()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        var expectedMetrics = new Dictionary<string, object>
        {
            ["RequestCount"] = 100,
            ["AverageResponseTime"] = 250.5,
            ["ErrorRate"] = 0.02
        };

        mockService
            .Setup(x => x.GetMetricsAsync())
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetServiceMetricsAsync(serviceName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var metrics = okResult.Value.Should().BeAssignableTo<Dictionary<string, object>>().Subject;
        metrics.Should().ContainKey("RequestCount");
        metrics["RequestCount"].Should().Be(100);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Metrics")]
    public async Task GetOverallMetricsAsync_ValidRequest_ReturnsAggregatedMetrics()
    {
        // Arrange
        SetupServicesWithMetrics();

        // Act
        var result = await _controller.GetOverallMetricsAsync();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var metrics = okResult.Value.Should().BeAssignableTo<OverallMetricsInfo>().Subject;
        metrics.TotalServices.Should().Be(3);
        metrics.RunningServices.Should().Be(3);
        metrics.TotalRequests.Should().BeGreaterThan(0);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Configuration")]
    public async Task GetServiceConfigurationAsync_ValidService_ReturnsConfiguration()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        var expectedConfig = new Dictionary<string, object>
        {
            ["MaxConnections"] = 100,
            ["Timeout"] = 30000,
            ["EnableLogging"] = true
        };

        mockService
            .Setup(x => x.GetConfigurationAsync())
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _controller.GetServiceConfigurationAsync(serviceName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var config = okResult.Value.Should().BeAssignableTo<Dictionary<string, object>>().Subject;
        config.Should().ContainKey("MaxConnections");
        config["MaxConnections"].Should().Be(100);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Configuration")]
    public async Task UpdateServiceConfigurationAsync_ValidConfiguration_UpdatesSuccessfully()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        var newConfig = new Dictionary<string, object>
        {
            ["MaxConnections"] = 200,
            ["Timeout"] = 60000
        };

        mockService
            .Setup(x => x.UpdateConfigurationAsync(It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateServiceConfigurationAsync(serviceName, newConfig);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        mockService.Verify(x => x.UpdateConfigurationAsync(
            It.Is<Dictionary<string, object>>(d => d.ContainsKey("MaxConnections"))), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task StartServiceAsync_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        const string serviceName = "TestService1";
        var mockService = CreateMockService(serviceName);

        _mockServiceRegistry
            .Setup(x => x.GetServiceAsync(serviceName))
            .ReturnsAsync(mockService.Object);

        mockService
            .Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service start failed"));

        // Act
        var result = await _controller.StartServiceAsync(serviceName);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ErrorHandling")]
    public async Task GetServiceAsync_InvalidServiceName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetServiceAsync("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private void SetupServiceRegistry()
    {
        var services = new List<IService>
        {
            CreateMockService("TestService1").Object,
            CreateMockService("TestService2").Object,
            CreateMockService("TestService3").Object
        };

        _mockServiceRegistry
            .Setup(x => x.GetAllServicesAsync())
            .ReturnsAsync(services);

        foreach (var service in services)
        {
            _mockServiceRegistry
                .Setup(x => x.GetServiceAsync(service.Name))
                .ReturnsAsync(service);
        }
    }

    private void SetupHealthyServices()
    {
        var services = _mockServiceRegistry.Object.GetAllServicesAsync().Result;
        foreach (var service in services)
        {
            var mockService = Mock.Get(service);
            mockService
                .Setup(x => x.GetHealthAsync())
                .ReturnsAsync(ServiceHealth.Healthy);
        }
    }

    private void SetupMixedHealthServices()
    {
        var services = _mockServiceRegistry.Object.GetAllServicesAsync().Result.ToList();
        
        Mock.Get(services[0])
            .Setup(x => x.GetHealthAsync())
            .ReturnsAsync(ServiceHealth.Healthy);
            
        Mock.Get(services[1])
            .Setup(x => x.GetHealthAsync())
            .ReturnsAsync(ServiceHealth.Healthy);
            
        Mock.Get(services[2])
            .Setup(x => x.GetHealthAsync())
            .ReturnsAsync(ServiceHealth.Unhealthy);
    }

    private void SetupServicesWithMetrics()
    {
        var services = _mockServiceRegistry.Object.GetAllServicesAsync().Result;
        foreach (var service in services)
        {
            var mockService = Mock.Get(service);
            mockService
                .Setup(x => x.GetMetricsAsync())
                .ReturnsAsync(new Dictionary<string, object>
                {
                    ["RequestCount"] = 100,
                    ["AverageResponseTime"] = 250.5
                });
                
            mockService
                .Setup(x => x.IsRunning)
                .Returns(true);
        }
    }

    private static Mock<IService> CreateMockService(string name)
    {
        var mockService = new Mock<IService>();
        mockService.Setup(x => x.Name).Returns(name);
        mockService.Setup(x => x.Description).Returns($"Test service {name}");
        mockService.Setup(x => x.Version).Returns("1.0.0");
        mockService.Setup(x => x.IsRunning).Returns(true);
        mockService.Setup(x => x.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
        
        return mockService;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #endregion

    #region Test Data Models

    public class ServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public ServiceHealth Health { get; set; }
    }

    public class ServiceHealthInfo
    {
        public ServiceHealth Status { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? Message { get; set; }
    }

    public class OverallHealthInfo
    {
        public ServiceHealth Status { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public int TotalServices { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class OverallMetricsInfo
    {
        public int TotalServices { get; set; }
        public int RunningServices { get; set; }
        public long TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public DateTime CollectedAt { get; set; }
    }

    #endregion
}
