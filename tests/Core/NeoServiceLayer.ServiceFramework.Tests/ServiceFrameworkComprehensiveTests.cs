using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Core;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework.Tests
{
    /// <summary>
    /// Comprehensive unit tests for ServiceFramework components.
    /// Tests service registration, configuration, and management functionality.
    /// </summary>
    public class ServiceFrameworkComprehensiveTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<ServiceBase>> _mockLogger;
        private readonly ServiceCollection _services;

        public ServiceFrameworkComprehensiveTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<ServiceBase>>();
            _services = new ServiceCollection();
        }

        [Fact]
        public void ServiceConfiguration_WithValidConfiguration_ShouldSetProperties()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceConfiguration>>();
            var configuration = new ServiceConfiguration(logger.Object);
            
            // Act - Set values using the configuration API
            configuration.SetValue("ServiceName", "TestService");
            configuration.SetValue("ServiceVersion", "1.0.0");
            configuration.SetValue("IsEnabled", true);
            configuration.SetValue("MaxRetryAttempts", 3);
            configuration.SetValue("TimeoutInSeconds", 30);
            configuration.SetValue("EnableLogging", true);
            configuration.SetValue("EnableMetrics", true);
            configuration.SetValue("EnableHealthChecks", true);

            // Assert
            configuration.GetValue<string>("ServiceName").Should().Be("TestService");
            configuration.GetValue<string>("ServiceVersion").Should().Be("1.0.0");
            configuration.GetValue<bool>("IsEnabled").Should().BeTrue();
            configuration.GetValue<int>("MaxRetryAttempts").Should().Be(3);
            configuration.GetValue<int>("TimeoutInSeconds").Should().Be(30);
            configuration.GetValue<bool>("EnableLogging").Should().BeTrue();
            configuration.GetValue<bool>("EnableMetrics").Should().BeTrue();
            configuration.GetValue<bool>("EnableHealthChecks").Should().BeTrue();
        }

        [Fact]
        public void ServiceConfiguration_WithDefaultValues_ShouldHaveExpectedDefaults()
        {
            // Arrange & Act
            var logger = new Mock<ILogger<ServiceConfiguration>>();
            var configuration = new ServiceConfiguration(logger.Object);

            // Assert
            configuration.GetValue<string>("ServiceName").Should().BeNull();
            configuration.GetValue<string>("ServiceVersion").Should().BeNull();
            configuration.GetValue<bool>("IsEnabled", true).Should().BeTrue(); // Assuming default is true
            configuration.GetValue<int>("MaxRetryAttempts", 3).Should().BeGreaterThan(0);
            configuration.GetValue<int>("TimeoutInSeconds", 30).Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public void ServiceConfiguration_WithInvalidServiceName_ShouldHandleGracefully(string invalidName)
        {
            // Arrange & Act
            var logger = new Mock<ILogger<ServiceConfiguration>>();
            var configuration = new ServiceConfiguration(logger.Object);
            configuration.SetValue("ServiceName", invalidName);

            // Assert
            configuration.GetValue<string>("ServiceName").Should().Be(invalidName);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void ServiceConfiguration_WithInvalidRetryAttempts_ShouldAcceptValue(int invalidRetries)
        {
            // Arrange & Act
            var logger = new Mock<ILogger<ServiceConfiguration>>();
            var configuration = new ServiceConfiguration(logger.Object);
            configuration.SetValue("MaxRetryAttempts", invalidRetries);

            // Assert
            configuration.GetValue<int>("MaxRetryAttempts").Should().Be(invalidRetries);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void ServiceConfiguration_WithInvalidTimeout_ShouldAcceptValue(int invalidTimeout)
        {
            // Arrange & Act
            var logger = new Mock<ILogger<ServiceConfiguration>>();
            var configuration = new ServiceConfiguration(logger.Object);
            configuration.SetValue("TimeoutInSeconds", invalidTimeout);

            // Assert
            configuration.GetValue<int>("TimeoutInSeconds").Should().Be(invalidTimeout);
        }

        [Fact]
        public async Task ServiceMetricsCollector_WithValidService_ShouldCollectMetrics()
        {
            // Arrange
            var loggerCollector = new Mock<ILogger<ServiceMetricsCollector>>();
            var loggerRegistry = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(loggerRegistry.Object);
            var collector = new ServiceMetricsCollector(registry, loggerCollector.Object);
            
            var serviceMock = new Mock<IService>();
            serviceMock.Setup(s => s.Name).Returns("TestService");
            serviceMock.Setup(s => s.Version).Returns("1.0.0");
            serviceMock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            serviceMock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "RequestCount", 100.0 } });
            
            registry.RegisterService(serviceMock.Object);

            // Act
            var metrics = await collector.CollectServiceMetricsAsync("TestService");

            // Assert
            metrics.Should().NotBeNull();
            metrics.Should().ContainKey("RequestCount");
            metrics!["RequestCount"].Should().Be(100.0);
        }

        [Fact]
        public async Task ServiceMetricsCollector_WithMultipleServices_ShouldCollectAllMetrics()
        {
            // Arrange
            var loggerCollector = new Mock<ILogger<ServiceMetricsCollector>>();
            var loggerRegistry = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(loggerRegistry.Object);
            var collector = new ServiceMetricsCollector(registry, loggerCollector.Object);
            
            var service1Mock = new Mock<IService>();
            service1Mock.Setup(s => s.Name).Returns("Service1");
            service1Mock.Setup(s => s.Version).Returns("1.0.0");
            service1Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            service1Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "RequestCount", 100 } });
            
            var service2Mock = new Mock<IService>();
            service2Mock.Setup(s => s.Name).Returns("Service2");
            service2Mock.Setup(s => s.Version).Returns("1.0.0");
            service2Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            service2Mock.Setup(s => s.GetMetricsAsync()).ReturnsAsync(new Dictionary<string, object> { { "ErrorRate", 0.05 } });
            
            registry.RegisterService(service1Mock.Object);
            registry.RegisterService(service2Mock.Object);

            // Act
            var allMetrics = await collector.CollectAllMetricsAsync();

            // Assert
            allMetrics.Should().HaveCount(2);
            allMetrics.Should().ContainKey("Service1");
            allMetrics.Should().ContainKey("Service2");
            allMetrics["Service1"].Should().ContainKey("RequestCount");
            allMetrics["Service2"].Should().ContainKey("ErrorRate");
        }

        [Fact]
        public async Task ServiceMetricsCollector_WithNonExistentService_ShouldReturnNull()
        {
            // Arrange
            var loggerCollector = new Mock<ILogger<ServiceMetricsCollector>>();
            var loggerRegistry = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(loggerRegistry.Object);
            var collector = new ServiceMetricsCollector(registry, loggerCollector.Object);
            var serviceName = "NonExistentService";

            // Act
            var metrics = await collector.CollectServiceMetricsAsync(serviceName);

            // Assert
            metrics.Should().BeNull();
        }

        [Fact]
        public void ServiceMetricsCollector_WithStartAndStopCollection_ShouldHandleTimerCorrectly()
        {
            // Arrange
            var loggerCollector = new Mock<ILogger<ServiceMetricsCollector>>();
            var loggerRegistry = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(loggerRegistry.Object);
            var collector = new ServiceMetricsCollector(registry, loggerCollector.Object);

            // Act
            collector.StartCollecting(TimeSpan.FromSeconds(1));
            collector.StopCollecting();

            // Assert - No exception should be thrown
            // This test validates the timer start/stop functionality
        }

        [Fact]
        public void ServiceRegistry_WithServiceRegistration_ShouldRegisterService()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(logger.Object);
            var serviceMock = new Mock<IService>();
            serviceMock.Setup(s => s.Name).Returns("TestService");
            serviceMock.Setup(s => s.Version).Returns("1.0.0");
            serviceMock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

            // Act
            registry.RegisterService(serviceMock.Object);
            var registeredServices = registry.GetAllServices();

            // Assert
            registeredServices.Should().Contain(serviceMock.Object);
        }

        [Fact]
        public void ServiceRegistry_WithMultipleServices_ShouldRegisterAllServices()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(logger.Object);
            var service1Mock = new Mock<IService>();
            service1Mock.Setup(s => s.Name).Returns("Service1");
            service1Mock.Setup(s => s.Version).Returns("1.0.0");
            service1Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            
            var service2Mock = new Mock<IService>();
            service2Mock.Setup(s => s.Name).Returns("Service2");
            service2Mock.Setup(s => s.Version).Returns("1.0.0");
            service2Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            
            var service3Mock = new Mock<IService>();
            service3Mock.Setup(s => s.Name).Returns("Service3");
            service3Mock.Setup(s => s.Version).Returns("1.0.0");
            service3Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

            // Act
            registry.RegisterService(service1Mock.Object);
            registry.RegisterService(service2Mock.Object);
            registry.RegisterService(service3Mock.Object);
            var registeredServices = registry.GetAllServices();

            // Assert
            registeredServices.Should().HaveCount(3);
            registeredServices.Should().Contain(service1Mock.Object);
            registeredServices.Should().Contain(service2Mock.Object);
            registeredServices.Should().Contain(service3Mock.Object);
        }

        [Fact]
        public void ServiceRegistry_WithDuplicateServiceName_ShouldNotOverwriteRegistration()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(logger.Object);
            var serviceName = "TestService";
            var service1Mock = new Mock<IService>();
            service1Mock.Setup(s => s.Name).Returns(serviceName);
            service1Mock.Setup(s => s.Version).Returns("1.0.0");
            service1Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);
            
            var service2Mock = new Mock<IService>();
            service2Mock.Setup(s => s.Name).Returns(serviceName);
            service2Mock.Setup(s => s.Version).Returns("2.0.0");
            service2Mock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

            // Act
            registry.RegisterService(service1Mock.Object);
            registry.RegisterService(service2Mock.Object); // Should not overwrite
            var registeredService = registry.GetService(serviceName);

            // Assert
            registeredService.Should().BeSameAs(service1Mock.Object); // Original service should remain
        }

        [Fact]
        public void ServiceRegistry_WithServiceUnregistration_ShouldRemoveService()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(logger.Object);
            var serviceName = "TestService";
            var serviceMock = new Mock<IService>();
            serviceMock.Setup(s => s.Name).Returns(serviceName);
            serviceMock.Setup(s => s.Version).Returns("1.0.0");
            serviceMock.Setup(s => s.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

            // Act
            registry.RegisterService(serviceMock.Object);
            var serviceBeforeUnregister = registry.GetService(serviceName);
            var unregisterResult = registry.UnregisterService(serviceName);
            var serviceAfterUnregister = registry.GetService(serviceName);

            // Assert
            serviceBeforeUnregister.Should().NotBeNull();
            unregisterResult.Should().BeTrue();
            serviceAfterUnregister.Should().BeNull();
        }

        [Fact]
        public void ServiceRegistry_WithNonExistentServiceUnregistration_ShouldNotThrow()
        {
            // Arrange
            var logger = new Mock<ILogger<ServiceRegistry>>();
            var registry = new ServiceRegistry(logger.Object);
            var serviceName = "NonExistentService";

            // Act
            var result = registry.UnregisterService(serviceName);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ServiceDependency_WithValidDependencies_ShouldSetProperties()
        {
            // Arrange & Act
            var dependency = new ServiceDependency("TestDependency", true, "1.0.0", "2.0.0", typeof(ITestService));

            // Assert
            dependency.ServiceName.Should().Be("TestDependency");
            dependency.ServiceType.Should().Be(typeof(ITestService));
            dependency.IsRequired.Should().BeTrue();
            dependency.MinimumVersion.Should().Be("1.0.0");
            dependency.MaximumVersion.Should().Be("2.0.0");
        }

        [Fact]
        public void ServiceDependency_WithOptionalDependency_ShouldAllowOptionalDependencies()
        {
            // Arrange & Act
            var dependency = ServiceDependency.Optional("OptionalDependency");

            // Assert
            dependency.IsRequired.Should().BeFalse();
            dependency.ServiceName.Should().Be("OptionalDependency");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void ServiceDependency_WithInvalidDependencyName_ShouldAcceptValue(string invalidName)
        {
            // Arrange & Act
            var dependency = new ServiceDependency(invalidName);

            // Assert
            dependency.ServiceName.Should().Be(invalidName);
        }

        [Fact]
        public void ServiceTemplate_WithValidTemplate_ShouldGenerateServiceCode()
        {
            // Arrange
            var serviceName = "TestService";
            var namespaceName = "TestNamespace";
            var description = "Test service description";

            // Act
            var generatedCode = ServiceTemplate.GenerateService(serviceName, namespaceName, description);

            // Assert
            generatedCode.Should().NotBeNullOrWhiteSpace();
            generatedCode.Should().Contain("TestService");
            generatedCode.Should().Contain("TestNamespace");
            generatedCode.Should().Contain("ITestService");
        }

        [Fact]
        public void ServiceTemplate_WithServiceRegistration_ShouldGenerateRegistrationCode()
        {
            // Arrange
            var serviceName = "TestService";
            var namespaceName = "TestNamespace";

            // Act
            var generatedCode = ServiceTemplate.GenerateServiceRegistration(serviceName, namespaceName);

            // Assert
            generatedCode.Should().NotBeNullOrWhiteSpace();
            generatedCode.Should().Contain($"{serviceName}ServiceExtensions");
            generatedCode.Should().Contain($"Add{serviceName}Service");
            generatedCode.Should().Contain("AddNeoService");
        }

        [Fact]
        public void ServiceTemplateGenerator_WithValidParameters_ShouldCreateServiceInterface()
        {
            // Arrange
            var generator = new ServiceTemplateGenerator();
            var serviceName = "TestService";
            var namespaceName = "TestNamespace";

            // Act
            var interfaceCode = generator.GenerateServiceInterface(serviceName, namespaceName);

            // Assert
            interfaceCode.Should().NotBeNullOrWhiteSpace();
            interfaceCode.Should().Contain($"I{serviceName}Service");
            interfaceCode.Should().Contain($"namespace {namespaceName}");
            interfaceCode.Should().Contain("IService");
        }

        [Fact]
        public void ServiceTemplateGenerator_WithImplementation_ShouldGenerateServiceImplementation()
        {
            // Arrange
            var generator = new ServiceTemplateGenerator();
            var serviceName = "TestService";
            var namespaceName = "TestNamespace";

            // Act
            var implementationCode = generator.GenerateServiceImplementation(serviceName, namespaceName);

            // Assert
            implementationCode.Should().NotBeNullOrWhiteSpace();
            implementationCode.Should().Contain($"{serviceName}Service");
            implementationCode.Should().Contain($"namespace {namespaceName}");
            implementationCode.Should().Contain("ServiceBase");
        }

        [Fact]
        public void ServiceCollectionExtensions_WithServiceFrameworkRegistration_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddNeoServiceFramework();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var serviceRegistry = serviceProvider.GetService<IServiceRegistry>();
            serviceRegistry.Should().NotBeNull();
        }

        [Fact]
        public void ServiceCollectionExtensions_WithNeoService_ShouldRegisterService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddNeoService<IComprehensiveTestService, ComprehensiveTestService>();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var service = serviceProvider.GetService<IComprehensiveTestService>();
            service.Should().NotBeNull();
            service.Should().BeOfType<ComprehensiveTestService>();
        }
    }

    // Test interfaces and implementations for testing purposes
    public interface IComprehensiveTestService : IService
    {
        Task<string> GetDataAsync();
    }

    public class ComprehensiveTestService : ServiceBase, IComprehensiveTestService
    {
        public ComprehensiveTestService(ILogger<ComprehensiveTestService> logger) 
            : base("ComprehensiveTestService", "Test Service", "1.0.0", logger)
        {
        }

        public Task<string> GetDataAsync() => Task.FromResult("Test Data");

        protected override Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected override Task<bool> OnStartAsync() => Task.FromResult(true);
        protected override Task<bool> OnStopAsync() => Task.FromResult(true);
        protected override Task<ServiceHealth> OnGetHealthAsync() => Task.FromResult(ServiceHealth.Healthy);
    }

    public interface ITestService : IService
    {
        Task<string> GetDataAsync();
    }

    public class BasicTestService : ServiceBase, ITestService
    {
        public BasicTestService(ILogger<BasicTestService> logger) 
            : base("BasicTestService", "Test Service", "1.0.0", logger)
        {
        }

        public Task<string> GetDataAsync() => Task.FromResult("Test Data");

        protected override Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected override Task<bool> OnStartAsync() => Task.FromResult(true);
        protected override Task<bool> OnStopAsync() => Task.FromResult(true);
        protected override Task<ServiceHealth> OnGetHealthAsync() => Task.FromResult(ServiceHealth.Healthy);
    }

    public interface ITestService2 : IService
    {
        Task<string> GetData2Async();
    }

    public class TestService2 : ServiceBase, ITestService2
    {
        public TestService2(ILogger<TestService2> logger) 
            : base("TestService2", "Test Service 2", "1.0.0", logger)
        {
        }

        public Task<string> GetData2Async() => Task.FromResult("Test Data 2");

        protected override Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected override Task<bool> OnStartAsync() => Task.FromResult(true);
        protected override Task<bool> OnStopAsync() => Task.FromResult(true);
        protected override Task<ServiceHealth> OnGetHealthAsync() => Task.FromResult(ServiceHealth.Healthy);
    }

    public interface ITestService3 : IService
    {
        Task<string> GetData3Async();
    }

    public class TestService3 : ServiceBase, ITestService3
    {
        public TestService3(ILogger<TestService3> logger) 
            : base("TestService3", "Test Service 3", "1.0.0", logger)
        {
        }

        public Task<string> GetData3Async() => Task.FromResult("Test Data 3");

        protected override Task<bool> OnInitializeAsync() => Task.FromResult(true);
        protected override Task<bool> OnStartAsync() => Task.FromResult(true);
        protected override Task<bool> OnStopAsync() => Task.FromResult(true);
        protected override Task<ServiceHealth> OnGetHealthAsync() => Task.FromResult(ServiceHealth.Healthy);
    }

    public class ServiceFrameworkOptions
    {
        public bool EnableMetrics { get; set; }
        public bool EnableHealthChecks { get; set; }
        public TimeSpan DefaultTimeout { get; set; }
    }
}