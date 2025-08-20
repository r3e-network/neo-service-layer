using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Core.Tests.ServiceFramework;

/// <summary>
/// Tests for ServiceRegistry class to verify service management and orchestration patterns.
/// </summary>
public class ServiceRegistryTests
{
    private readonly Mock<ILogger<ServiceRegistry>> _mockLogger = new();
    private readonly ServiceRegistry _serviceRegistry;

    public ServiceRegistryTests()
    {
        _serviceRegistry = new ServiceRegistry(_mockLogger.Object);
    }

    #region Registration Tests

    [Fact]
    public void RegisterService_WithValidService_ShouldRegisterSuccessfully()
    {
        // Arrange
        var mockService = CreateMockService("TestService");
        var eventRaised = false;
        _serviceRegistry.ServiceRegistered += (_, _) => eventRaised = true;

        // Act
        _serviceRegistry.RegisterService(mockService.Object);

        // Assert
        _serviceRegistry.ServiceExists("TestService").Should().BeTrue();
        _serviceRegistry.GetServiceCount().Should().Be(1);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterServiceAsync_WithValidService_ShouldRegisterSuccessfully()
    {
        // Arrange
        var mockService = CreateMockService("TestServiceAsync");
        var eventRaised = false;
        _serviceRegistry.ServiceRegistered += (_, _) => eventRaised = true;

        // Act
        await _serviceRegistry.RegisterServiceAsync(mockService.Object);

        // Assert
        _serviceRegistry.ServiceExists("TestServiceAsync").Should().BeTrue();
        _serviceRegistry.GetServiceCount().Should().Be(1);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void RegisterService_WithDuplicateName_ShouldNotRegisterAndLogWarning()
    {
        // Arrange
        var mockService1 = CreateMockService("DuplicateService");
        var mockService2 = CreateMockService("DuplicateService");
        _serviceRegistry.RegisterService(mockService1.Object);

        // Act
        _serviceRegistry.RegisterService(mockService2.Object);

        // Assert
        _serviceRegistry.GetServiceCount().Should().Be(1);
        VerifyLoggerCalled(LogLevel.Warning, "already registered");
    }

    [Fact]
    public void RegisterService_WithNullService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _serviceRegistry.RegisterService(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterServiceAsync_WithNullService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _serviceRegistry.RegisterServiceAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Unregistration Tests

    [Fact]
    public void UnregisterService_WithExistingService_ShouldUnregisterSuccessfully()
    {
        // Arrange
        var mockService = CreateMockService("ServiceToUnregister");
        _serviceRegistry.RegisterService(mockService.Object);
        var eventRaised = false;
        _serviceRegistry.ServiceUnregistered += (_, _) => eventRaised = true;

        // Act
        var result = _serviceRegistry.UnregisterService("ServiceToUnregister");

        // Assert
        result.Should().BeTrue();
        _serviceRegistry.ServiceExists("ServiceToUnregister").Should().BeFalse();
        _serviceRegistry.GetServiceCount().Should().Be(0);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void UnregisterService_WithNonExistentService_ShouldReturnFalseAndLogWarning()
    {
        // Act
        var result = _serviceRegistry.UnregisterService("NonExistentService");

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Warning, "is not registered");
    }

    [Fact]
    public void UnregisterService_WithNullOrEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var actionNull = () => _serviceRegistry.UnregisterService(null!);
        var actionEmpty = () => _serviceRegistry.UnregisterService("");

        actionNull.Should().Throw<ArgumentException>();
        actionEmpty.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Service Retrieval Tests

    [Fact]
    public void GetService_WithExistingService_ShouldReturnService()
    {
        // Arrange
        var mockService = CreateMockService("ExistingService");
        _serviceRegistry.RegisterService(mockService.Object);

        // Act
        var result = _serviceRegistry.GetService("ExistingService");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockService.Object);
    }

    [Fact]
    public void GetService_WithNonExistentService_ShouldReturnNullAndLogWarning()
    {
        // Act
        var result = _serviceRegistry.GetService("NonExistentService");

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(LogLevel.Warning, "is not registered");
    }

    [Fact]
    public void GetService_WithNullOrEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var actionNull = () => _serviceRegistry.GetService(null!);
        var actionEmpty = () => _serviceRegistry.GetService("");

        actionNull.Should().Throw<ArgumentException>();
        actionEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetServiceGeneric_WithCorrectType_ShouldReturnTypedService()
    {
        // Arrange
        var mockBlockchainService = CreateMockBlockchainService("BlockchainService");
        _serviceRegistry.RegisterService(mockBlockchainService.Object);

        // Act
        var result = _serviceRegistry.GetService<IBlockchainService>("BlockchainService");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockBlockchainService.Object);
    }

    [Fact]
    public void GetServiceGeneric_WithIncorrectType_ShouldReturnNullAndLogWarning()
    {
        // Arrange
        var mockService = CreateMockService("RegularService");
        _serviceRegistry.RegisterService(mockService.Object);

        // Act
        var result = _serviceRegistry.GetService<IBlockchainService>("RegularService");

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(LogLevel.Warning, "is not of type");
    }

    #endregion

    #region Service Collection Tests

    [Fact]
    public void GetAllServices_WithMultipleServices_ShouldReturnAllServices()
    {
        // Arrange
        var service1 = CreateMockService("Service1");
        var service2 = CreateMockService("Service2");
        var service3 = CreateMockService("Service3");

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);
        _serviceRegistry.RegisterService(service3.Object);

        // Act
        var result = _serviceRegistry.GetAllServices().ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(service1.Object);
        result.Should().Contain(service2.Object);
        result.Should().Contain(service3.Object);
    }

    [Fact]
    public async Task GetAllServicesAsync_WithMultipleServices_ShouldReturnAllServices()
    {
        // Arrange
        var service1 = CreateMockService("Service1");
        var service2 = CreateMockService("Service2");

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = (await _serviceRegistry.GetAllServicesAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(service1.Object);
        result.Should().Contain(service2.Object);
    }

    [Fact]
    public void GetAllServicesGeneric_WithSpecificType_ShouldReturnOnlyMatchingServices()
    {
        // Arrange
        var regularService = CreateMockService("RegularService");
        var blockchainService1 = CreateMockBlockchainService("BlockchainService1");
        var blockchainService2 = CreateMockBlockchainService("BlockchainService2");

        _serviceRegistry.RegisterService(regularService.Object);
        _serviceRegistry.RegisterService(blockchainService1.Object);
        _serviceRegistry.RegisterService(blockchainService2.Object);

        // Act
        var result = _serviceRegistry.GetAllServices<IBlockchainService>().ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(blockchainService1.Object);
        result.Should().Contain(blockchainService2.Object);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void FindServicesByNamePattern_WithValidPattern_ShouldReturnMatchingServices()
    {
        // Arrange
        var service1 = CreateMockService("TestService1");
        var service2 = CreateMockService("TestService2");
        var service3 = CreateMockService("ProductionService");

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);
        _serviceRegistry.RegisterService(service3.Object);

        // Act
        var result = _serviceRegistry.FindServicesByNamePattern("Test.*").ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(service1.Object);
        result.Should().Contain(service2.Object);
        result.Should().NotContain(service3.Object);
    }

    [Fact]
    public void FindServicesByNamePattern_WithInvalidPattern_ShouldReturnEmptyAndLogError()
    {
        // Arrange
        var service = CreateMockService("TestService");
        _serviceRegistry.RegisterService(service.Object);

        // Act
        var result = _serviceRegistry.FindServicesByNamePattern("[invalid").ToList();

        // Assert
        result.Should().BeEmpty();
        VerifyLoggerCalled(LogLevel.Error, "Invalid regular expression pattern");
    }

    [Fact]
    public void FindServicesByNamePattern_WithNullOrEmptyPattern_ShouldThrowArgumentException()
    {
        // Act & Assert
        var actionNull = () => _serviceRegistry.FindServicesByNamePattern(null!);
        var actionEmpty = () => _serviceRegistry.FindServicesByNamePattern("");

        actionNull.Should().Throw<ArgumentException>();
        actionEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FindServicesByCapability_WithSpecificCapability_ShouldReturnMatchingServices()
    {
        // Arrange
        var regularService = CreateMockService("RegularService");
        var blockchainService = CreateMockBlockchainService("BlockchainService");

        _serviceRegistry.RegisterService(regularService.Object);
        _serviceRegistry.RegisterService(blockchainService.Object);

        // Act
        var result = _serviceRegistry.FindServicesByCapability<IBlockchainService>().ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(blockchainService.Object);
    }

    [Fact]
    public void FindServicesByBlockchainType_WithSpecificBlockchainType_ShouldReturnSupportingServices()
    {
        // Arrange
        var neoService = CreateMockBlockchainService("NeoService", BlockchainType.NeoN3);
        var neoXService = CreateMockBlockchainService("NeoXService", BlockchainType.NeoX);
        var multiService = CreateMockBlockchainService("MultiService", BlockchainType.NeoN3, BlockchainType.NeoX);

        _serviceRegistry.RegisterService(neoService.Object);
        _serviceRegistry.RegisterService(neoXService.Object);
        _serviceRegistry.RegisterService(multiService.Object);

        // Act
        var neoResults = _serviceRegistry.FindServicesByBlockchainType(BlockchainType.NeoN3).ToList();
        var neoXResults = _serviceRegistry.FindServicesByBlockchainType(BlockchainType.NeoX).ToList();

        // Assert
        neoResults.Should().HaveCount(2);
        neoResults.Should().Contain(neoService.Object);
        neoResults.Should().Contain(multiService.Object);

        neoXResults.Should().HaveCount(2);
        neoXResults.Should().Contain(neoXService.Object);
        neoXResults.Should().Contain(multiService.Object);
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task InitializeAllServicesAsync_WithAllSuccessfulServices_ShouldReturnTrue()
    {
        // Arrange
        var service1 = CreateMockService("Service1", initializeResult: true);
        var service2 = CreateMockService("Service2", initializeResult: true);

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = await _serviceRegistry.InitializeAllServicesAsync();

        // Assert
        result.Should().BeTrue();
        service1.Verify(s => s.InitializeAsync(), Times.Once);
        service2.Verify(s => s.InitializeAsync(), Times.Once);
        VerifyLoggerCalled(LogLevel.Information, "All services initialized successfully");
    }

    [Fact]
    public async Task InitializeAllServicesAsync_WithSomeFailedServices_ShouldReturnFalse()
    {
        // Arrange
        var service1 = CreateMockService("Service1", initializeResult: true);
        var service2 = CreateMockService("Service2", initializeResult: false);

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = await _serviceRegistry.InitializeAllServicesAsync();

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Warning, "Some services failed to initialize");
    }

    [Fact]
    public async Task InitializeServicesByPatternAsync_WithMatchingServices_ShouldInitializeOnlyMatching()
    {
        // Arrange
        var testService1 = CreateMockService("TestService1", initializeResult: true);
        var testService2 = CreateMockService("TestService2", initializeResult: true);
        var prodService = CreateMockService("ProductionService", initializeResult: true);

        _serviceRegistry.RegisterService(testService1.Object);
        _serviceRegistry.RegisterService(testService2.Object);
        _serviceRegistry.RegisterService(prodService.Object);

        // Act
        var result = await _serviceRegistry.InitializeServicesByPatternAsync("Test.*");

        // Assert
        result.Should().BeTrue();
        testService1.Verify(s => s.InitializeAsync(), Times.Once);
        testService2.Verify(s => s.InitializeAsync(), Times.Once);
        prodService.Verify(s => s.InitializeAsync(), Times.Never);
    }

    [Fact]
    public async Task InitializeServicesByPatternAsync_WithNoMatchingServices_ShouldReturnTrueAndLogWarning()
    {
        // Arrange
        var service = CreateMockService("Service");
        _serviceRegistry.RegisterService(service.Object);

        // Act
        var result = await _serviceRegistry.InitializeServicesByPatternAsync("NoMatch.*");

        // Assert
        result.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Warning, "No services found matching pattern");
    }

    [Fact]
    public async Task StartAllServicesAsync_WithAllSuccessfulServices_ShouldReturnTrue()
    {
        // Arrange
        var service1 = CreateMockService("Service1", startResult: true);
        var service2 = CreateMockService("Service2", startResult: true);

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = await _serviceRegistry.StartAllServicesAsync();

        // Assert
        result.Should().BeTrue();
        service1.Verify(s => s.StartAsync(), Times.Once);
        service2.Verify(s => s.StartAsync(), Times.Once);
        VerifyLoggerCalled(LogLevel.Information, "All services started successfully");
    }

    [Fact]
    public async Task StopAllServicesAsync_WithAllSuccessfulServices_ShouldReturnTrue()
    {
        // Arrange
        var service1 = CreateMockService("Service1", stopResult: true);
        var service2 = CreateMockService("Service2", stopResult: true);

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = await _serviceRegistry.StopAllServicesAsync();

        // Assert
        result.Should().BeTrue();
        service1.Verify(s => s.StopAsync(), Times.Once);
        service2.Verify(s => s.StopAsync(), Times.Once);
        VerifyLoggerCalled(LogLevel.Information, "All services stopped successfully");
    }

    #endregion

    #region Health Monitoring Tests

    [Fact]
    public async Task GetAllServicesHealthAsync_ShouldReturnHealthForAllServices()
    {
        // Arrange
        var service1 = CreateMockService("Service1", healthStatus: ServiceHealth.Healthy);
        var service2 = CreateMockService("Service2", healthStatus: ServiceHealth.Degraded);

        _serviceRegistry.RegisterService(service1.Object);
        _serviceRegistry.RegisterService(service2.Object);

        // Act
        var result = await _serviceRegistry.GetAllServicesHealthAsync();

        // Assert
        result.Should().HaveCount(2);
        result["Service1"].Should().Be(ServiceHealth.Healthy);
        result["Service2"].Should().Be(ServiceHealth.Degraded);
    }

    [Fact]
    public async Task GetServicesHealthByPatternAsync_ShouldReturnHealthForMatchingServices()
    {
        // Arrange
        var testService = CreateMockService("TestService", healthStatus: ServiceHealth.Healthy);
        var prodService = CreateMockService("ProductionService", healthStatus: ServiceHealth.Degraded);

        _serviceRegistry.RegisterService(testService.Object);
        _serviceRegistry.RegisterService(prodService.Object);

        // Act
        var result = await _serviceRegistry.GetServicesHealthByPatternAsync("Test.*");

        // Assert
        result.Should().HaveCount(1);
        result["TestService"].Should().Be(ServiceHealth.Healthy);
        result.Should().NotContainKey("ProductionService");
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task ServiceHealthChanged_WhenHealthChanges_ShouldRaiseEvent()
    {
        // Arrange
        var service = CreateMockService("TestService", healthStatus: ServiceHealth.Healthy);
        _serviceRegistry.RegisterService(service.Object);

        // First call to establish initial health status
        await _serviceRegistry.GetAllServicesHealthAsync();

        ServiceHealthChangedEventArgs? eventArgs = null;
        _serviceRegistry.ServiceHealthChanged += (_, args) => eventArgs = args;

        // Act - Change health status
        service.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Degraded);
        await _serviceRegistry.GetAllServicesHealthAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Service.Should().Be(service.Object);
        eventArgs.PreviousHealth.Should().Be(ServiceHealth.Healthy);
        eventArgs.CurrentHealth.Should().Be(ServiceHealth.Degraded);
    }

    #endregion

    #region Helper Methods

    private Mock<IService> CreateMockService(
        string name,
        bool initializeResult = true,
        bool startResult = true,
        bool stopResult = true,
        ServiceHealth healthStatus = ServiceHealth.Healthy)
    {
        var mock = new Mock<IService>();
        mock.Setup(s => s.Name).Returns(name);
        mock.Setup(s => s.Description).Returns($"Description for {name}");
        mock.Setup(s => s.Version).Returns("1.0.0");
        mock.Setup(s => s.IsRunning).Returns(true);
        mock.Setup(s => s.Dependencies).Returns(Enumerable.Empty<object>());
        mock.Setup(s => s.Capabilities).Returns(Enumerable.Empty<Type>());
        mock.Setup(s => s.Metadata).Returns(new Dictionary<string, string>());
        mock.Setup(s => s.InitializeAsync()).ReturnsAsync(initializeResult);
        mock.Setup(s => s.StartAsync()).ReturnsAsync(startResult);
        mock.Setup(s => s.StopAsync()).ReturnsAsync(stopResult);
        mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(healthStatus);
        mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object>());
        mock.Setup(s => s.ValidateDependenciesAsync(It.IsAny<IEnumerable<IService>>())).ReturnsAsync(true);
        return mock;
    }

    private Mock<IBlockchainService> CreateMockBlockchainService(
        string name,
        params BlockchainType[] supportedBlockchains)
    {
        var mock = new Mock<IBlockchainService>();
        mock.Setup(s => s.Name).Returns(name);
        mock.Setup(s => s.Description).Returns($"Blockchain description for {name}");
        mock.Setup(s => s.Version).Returns("1.0.0");
        mock.Setup(s => s.IsRunning).Returns(true);
        mock.Setup(s => s.Dependencies).Returns(Enumerable.Empty<object>());
        mock.Setup(s => s.Capabilities).Returns(new[] { typeof(IBlockchainService) });
        mock.Setup(s => s.Metadata).Returns(new Dictionary<string, string>());
        mock.Setup(s => s.InitializeAsync()).ReturnsAsync(true);
        mock.Setup(s => s.StartAsync()).ReturnsAsync(true);
        mock.Setup(s => s.StopAsync()).ReturnsAsync(true);
        mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
        mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object>());
        mock.Setup(s => s.ValidateDependenciesAsync(It.IsAny<IEnumerable<IService>>())).ReturnsAsync(true);
        mock.Setup(s => s.SupportedBlockchains).Returns(supportedBlockchains);
        mock.Setup(s => s.SupportsBlockchain(It.IsAny<BlockchainType>()))
            .Returns<BlockchainType>(bt => supportedBlockchains.Contains(bt));
        return mock;
    }

    private void VerifyLoggerCalled(LogLevel level, string messageContains)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}

/// <summary>
/// Tests for ServiceEventArgs and ServiceHealthChangedEventArgs classes.
/// </summary>
public class ServiceEventArgsTests
{
    [Fact]
    public void ServiceEventArgs_WithValidService_ShouldSetServiceProperty()
    {
        // Arrange
        var mockService = new Mock<IService>();
        mockService.Setup(s => s.Name).Returns("TestService");

        // Act
        var eventArgs = new ServiceEventArgs(mockService.Object);

        // Assert
        eventArgs.Service.Should().Be(mockService.Object);
    }

    [Fact]
    public void ServiceHealthChangedEventArgs_WithValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var mockService = new Mock<IService>();
        mockService.Setup(s => s.Name).Returns("TestService");
        var previousHealth = ServiceHealth.Healthy;
        var currentHealth = ServiceHealth.Degraded;

        // Act
        var eventArgs = new ServiceHealthChangedEventArgs(mockService.Object, previousHealth, currentHealth);

        // Assert
        eventArgs.Service.Should().Be(mockService.Object);
        eventArgs.PreviousHealth.Should().Be(previousHealth);
        eventArgs.CurrentHealth.Should().Be(currentHealth);
    }
}
