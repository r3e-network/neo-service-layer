using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Tests for the ServiceRegistry class.
/// </summary>
public class ServiceRegistryTests
{
    private readonly Mock<ILogger<ServiceRegistry>> _loggerMock;
    private readonly ServiceRegistry _registry;

    public ServiceRegistryTests()
    {
        _loggerMock = new Mock<ILogger<ServiceRegistry>>();
        _registry = new ServiceRegistry(_loggerMock.Object);
    }

    [Fact]
    public void RegisterService_ShouldAddServiceToRegistry()
    {
        // Arrange
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");

        // Act
        _registry.RegisterService(serviceMock.Object);

        // Assert
        Assert.Contains(serviceMock.Object, _registry.GetAllServices());
    }

    [Fact]
    public void RegisterService_ShouldRegisterMultipleServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.1.0");

        // Act
        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Assert
        Assert.Equal(2, _registry.GetAllServices().Count());
        Assert.Contains(service1Mock.Object, _registry.GetAllServices());
        Assert.Contains(service2Mock.Object, _registry.GetAllServices());
    }

    [Fact]
    public void GetService_ShouldReturnService_WhenServiceIsRegistered()
    {
        // Arrange
        var serviceMock = new Mock<IService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");

        _registry.RegisterService(serviceMock.Object);

        // Act
        var result = _registry.GetService("TestService");

        // Assert
        Assert.Same(serviceMock.Object, result);
    }

    [Fact]
    public void GetService_ShouldReturnNull_WhenServiceIsNotRegistered()
    {
        // Act
        var result = _registry.GetService("NonExistentService");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithType_ShouldReturnService_WhenServiceWithTypeIsRegistered()
    {
        // Arrange
        var serviceMock = new Mock<ITestService>();
        serviceMock.Setup(s => s.Name).Returns("TestService");
        serviceMock.Setup(s => s.Version).Returns("1.0.0");
        serviceMock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(ITestService) });

        _registry.RegisterService(serviceMock.Object);

        // Act
        var result = _registry.GetService<ITestService>("TestService");

        // Assert
        Assert.Same(serviceMock.Object, result);
    }

    [Fact]
    public void GetService_WithType_ShouldReturnNull_WhenServiceWithTypeIsNotRegistered()
    {
        // Act
        var result = _registry.GetService<ITestService>("NonExistentService");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllServices_WithType_ShouldReturnServices_WhenServicesWithTypeAreRegistered()
    {
        // Arrange
        var service1Mock = new Mock<ITestService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(ITestService) });

        var service2Mock = new Mock<ITestService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(ITestService) });

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        var results = _registry.GetAllServices<ITestService>().ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(service1Mock.Object, results);
        Assert.Contains(service2Mock.Object, results);
    }

    [Fact]
    public void GetAllServices_WithType_ShouldReturnEmptyCollection_WhenNoServicesWithTypeAreRegistered()
    {
        // Act
        var results = _registry.GetAllServices<ITestService>();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindServicesByCapability_ShouldReturnServices_WhenServicesWithCapabilityAreRegistered()
    {
        // Arrange
        var service1Mock = new Mock<ITestService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(ITestService) });

        var service2Mock = new Mock<ITestService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.Capabilities).Returns(new List<Type> { typeof(ITestService) });

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        var results = _registry.FindServicesByCapability<ITestService>().ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(service1Mock.Object, results);
        Assert.Contains(service2Mock.Object, results);
    }

    [Fact]
    public async Task InitializeAllServicesAsync_ShouldInitializeAllServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.InitializeAsync()).ReturnsAsync(true);

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.InitializeAsync()).ReturnsAsync(true);

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        bool result = await _registry.InitializeAllServicesAsync();

        // Assert
        Assert.True(result);
        service1Mock.Verify(s => s.InitializeAsync(), Times.Once);
        service2Mock.Verify(s => s.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartAllServicesAsync_ShouldStartAllServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.StartAsync()).ReturnsAsync(true);
        service1Mock.Setup(s => s.ValidateDependenciesAsync(It.IsAny<IEnumerable<IService>>())).ReturnsAsync(true);

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.StartAsync()).ReturnsAsync(true);
        service2Mock.Setup(s => s.ValidateDependenciesAsync(It.IsAny<IEnumerable<IService>>())).ReturnsAsync(true);

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        bool result = await _registry.StartAllServicesAsync();

        // Assert
        Assert.True(result);
        service1Mock.Verify(s => s.StartAsync(), Times.Once);
        service2Mock.Verify(s => s.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAllServicesAsync_ShouldStopAllServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.StopAsync()).ReturnsAsync(true);

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.StopAsync()).ReturnsAsync(true);

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        bool result = await _registry.StopAllServicesAsync();

        // Assert
        Assert.True(result);
        service1Mock.Verify(s => s.StopAsync(), Times.Once);
        service2Mock.Verify(s => s.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllServicesHealthAsync_ShouldReturnHealthForAllServices()
    {
        // Arrange
        var service1Mock = new Mock<IService>();
        service1Mock.Setup(s => s.Name).Returns("TestService1");
        service1Mock.Setup(s => s.Version).Returns("1.0.0");
        service1Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

        var service2Mock = new Mock<IService>();
        service2Mock.Setup(s => s.Name).Returns("TestService2");
        service2Mock.Setup(s => s.Version).Returns("1.0.0");
        service2Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Degraded);

        _registry.RegisterService(service1Mock.Object);
        _registry.RegisterService(service2Mock.Object);

        // Act
        var results = await _registry.GetAllServicesHealthAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(ServiceHealth.Healthy, results["TestService1"]);
        Assert.Equal(ServiceHealth.Degraded, results["TestService2"]);

        // The implementation might call GetHealthAsync multiple times, so we'll just verify
        // that it was called at least once for each service
        service1Mock.Verify(s => s.GetHealthAsync(), Times.AtLeastOnce);
        service2Mock.Verify(s => s.GetHealthAsync(), Times.AtLeastOnce);
    }
}

/// <summary>
/// Interface for testing service registry.
/// </summary>
public interface ITestService : IService
{
}
