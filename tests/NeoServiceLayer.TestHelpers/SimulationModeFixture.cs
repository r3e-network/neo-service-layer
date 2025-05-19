using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NeoServiceLayer.TestHelpers
{
    /// <summary>
    /// A test fixture for simulation mode tests that provides common setup and teardown functionality.
    /// </summary>
    public class SimulationModeFixture : IDisposable
    {
        public IConfiguration Configuration { get; }
        public IServiceProvider ServiceProvider { get; }
        public ILoggerFactory LoggerFactory { get; }
        public bool SimulationModeEnabled { get; }
        public string EnclavePath { get; }
        public string OcclumInstanceDir { get; }

        public SimulationModeFixture()
        {
            // Set up simulation mode
            Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");

            // Build configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("simulation-test-config.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = configBuilder.Build();

            // Get paths from configuration or environment variables
            EnclavePath = Environment.GetEnvironmentVariable("OCCLUM_ENCLAVE_PATH")
                ?? Configuration["Tee:Enclave:Path"]
                ?? "../src/NeoServiceLayer.Tee.Enclave/build/lib/libenclave.so";

            OcclumInstanceDir = Environment.GetEnvironmentVariable("OCCLUM_INSTANCE_DIR")
                ?? Configuration["Tee:Occlum:InstanceDir"]
                ?? "../occlum_instance";

            SimulationModeEnabled = Environment.GetEnvironmentVariable("OCCLUM_SIMULATION") == "1"
                || (Configuration["Tee:Enclave:SimulationMode"] != null && bool.TryParse(Configuration["Tee:Enclave:SimulationMode"], out bool result) && result);

            // Set up services
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // Add configuration
            services.AddSingleton(Configuration);

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();

            // Get logger factory
            LoggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
        }

        public void Dispose()
        {
            // Clean up resources
            (ServiceProvider as IDisposable)?.Dispose();
            (LoggerFactory as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Collection definition for simulation mode tests.
    /// </summary>
    [CollectionDefinition("SimulationMode")]
    public class SimulationModeCollection : ICollectionFixture<SimulationModeFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
