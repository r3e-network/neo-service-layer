using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.TestInfrastructure;
using System;

namespace NeoServiceLayer.Integration.Tests.TestHelpers
{
    public static class TestServiceRegistration
    {
        public static IServiceCollection AddTestEnclaveServices(this IServiceCollection services)
        {
            // Register mock enclave wrapper
            var mockEnclaveWrapper = new Mock<IEnclaveWrapper>();
            mockEnclaveWrapper.Setup(x => x.Initialize()).Returns(true);
            mockEnclaveWrapper.Setup(x => x.SealData(It.IsAny<byte[]>())).Returns(new byte[] { 1, 2, 3 });
            mockEnclaveWrapper.Setup(x => x.UnsealData(It.IsAny<byte[]>())).Returns(new byte[] { 4, 5, 6 });
            mockEnclaveWrapper.Setup(x => x.GetAttestation()).Returns(new byte[] { 7, 8, 9 });
            services.AddSingleton(mockEnclaveWrapper.Object);

            // Register mock enclave manager
            var mockEnclaveManager = new Mock<IEnclaveManager>();
            mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
            mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);
            mockEnclaveManager.Setup(x => x.DestroyEnclaveAsync()).ReturnsAsync(true);
            services.AddSingleton(mockEnclaveManager.Object);

            // Register test enclave service using TestEnclaveProvider
            services.AddSingleton<IEnclaveService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TestEnclaveProvider>>();
                return new TestEnclaveProvider(logger);
            });

            return services;
        }

        public static IServiceCollection AddTestNeoServiceLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Add core services using the Core version
            NeoServiceLayer.Core.Configuration.ServiceRegistration.AddNeoServiceLayer(services, configuration);
            
            // Add test-specific services
            services.AddTestEnclaveServices();
            
            // Add mock blockchain client
            services.AddSingleton<IBlockchainClient, MockBlockchainClient>();
            services.AddSingleton<IBlockchainClientFactory, MockBlockchainClientFactory>();
            
            return services;
        }
    }
}