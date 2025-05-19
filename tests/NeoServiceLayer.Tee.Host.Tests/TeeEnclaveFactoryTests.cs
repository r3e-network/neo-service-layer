using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Tests.Mocks;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests
{
    public class TeeEnclaveFactoryTests
    {
        private readonly Mock<ILogger<TeeEnclaveFactory>> _loggerMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<OpenEnclaveInterface> _openEnclaveInterfaceMock;
        // SGX is not supported in this version, using OpenEnclave for all cases

        public TeeEnclaveFactoryTests()
        {
            _loggerMock = new Mock<ILogger<TeeEnclaveFactory>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _openEnclaveInterfaceMock = new Mock<OpenEnclaveInterface>();

            _serviceProviderMock.Setup(x => x.GetService(typeof(OpenEnclaveInterface)))
                .Returns(_openEnclaveInterfaceMock.Object);
        }

        [Fact]
        public void CreateEnclaveInterface_WithOpenEnclaveType_ReturnsOpenEnclaveInterface()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Tee:Type", "OpenEnclave" }
                })
                .Build();

            var factory = new TeeEnclaveFactory(_loggerMock.Object, configuration, _serviceProviderMock.Object);

            // Act
            var result = factory.CreateEnclaveInterface();

            // Assert
            Assert.Same(_openEnclaveInterfaceMock.Object, result);
        }

        [Fact]
        public void CreateEnclaveInterface_WithSgxType_ReturnsOpenEnclaveInterface()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Tee:Type", "SGX" }
                })
                .Build();

            var factory = new TeeEnclaveFactory(_loggerMock.Object, configuration, _serviceProviderMock.Object);

            // Act
            var result = factory.CreateEnclaveInterface();

            // Assert
            // SGX is not supported in this version, so it should return OpenEnclaveInterface
            Assert.Same(_openEnclaveInterfaceMock.Object, result);
        }

        [Fact]
        public void CreateEnclaveInterface_WithUnknownType_ReturnsOpenEnclaveInterface()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Tee:Type", "Unknown" }
                })
                .Build();

            var factory = new TeeEnclaveFactory(_loggerMock.Object, configuration, _serviceProviderMock.Object);

            // Act
            var result = factory.CreateEnclaveInterface();

            // Assert
            Assert.Same(_openEnclaveInterfaceMock.Object, result);
        }

        [Fact]
        public void CreateEnclaveInterface_WithNoType_ReturnsOpenEnclaveInterface()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            var factory = new TeeEnclaveFactory(_loggerMock.Object, configuration, _serviceProviderMock.Object);

            // Act
            var result = factory.CreateEnclaveInterface();

            // Assert
            Assert.Same(_openEnclaveInterfaceMock.Object, result);
        }

        [Fact]
        public void RegisterServices_RegistersCorrectServices()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Tee:Type", "OpenEnclave" }
                })
                .Build();

            var services = new ServiceCollection();

            // Act
            TeeEnclaveFactory.RegisterServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetService<TeeEnclaveFactory>();

            Assert.NotNull(factory);
        }
    }
}
