using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Extensions;
using NeoServiceLayer.Tee.Host.Extensions;

namespace NeoServiceLayer.IntegrationTests
{
    /// <summary>
    /// Test fixture for integration tests.
    /// </summary>
    public class TestFixture : IDisposable
    {
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFixture"/> class.
        /// </summary>
        public TestFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Add TEE services
            services.AddTeeServices(configuration);

            // Add core services
            services.AddCoreServices(configuration);

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Disposes the test fixture.
        /// </summary>
        public void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
