using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Factory for creating TEE interfaces.
    /// </summary>
    public class TeeInterfaceFactory : ITeeInterfaceFactory
    {
        private readonly ILogger<TeeInterfaceFactory> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the TeeInterfaceFactory class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public TeeInterfaceFactory(
            ILogger<TeeInterfaceFactory> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public ITeeInterface CreateTeeInterface(string enclavePath, bool simulationMode)
        {
            // Get the TEE type from configuration
            string teeType = _configuration.GetValue<string>("Tee:Type", "Occlum");

            _logger.LogInformation("Creating TEE interface for type: {TeeType}", teeType);

            // Create the appropriate TEE interface based on the type
            switch (teeType.ToLowerInvariant())
            {
                case "sgx":
                    _logger.LogInformation("Creating SGX interface");
                    return CreateSgxInterface(enclavePath, simulationMode);

                case "occlum":
                    _logger.LogInformation("Creating Occlum interface");
                    return CreateOcclumInterface(enclavePath, simulationMode);

                default:
                    _logger.LogWarning("Unknown TEE type: {TeeType}. Falling back to Occlum.", teeType);
                    return CreateOcclumInterface(enclavePath, simulationMode);
            }
        }



        /// <summary>
        /// Creates an Occlum interface.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The Occlum interface.</returns>
        public ITeeInterface CreateOcclumInterface(string enclavePath, bool simulationMode)
        {
            _logger.LogInformation("Creating Occlum interface with enclave path: {EnclavePath}, simulation mode: {SimulationMode}", enclavePath, simulationMode);

            // Set the simulation mode environment variable
            if (simulationMode)
            {
                Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "1");
            }
            else
            {
                Environment.SetEnvironmentVariable("OCCLUM_SIMULATION", "0");
            }

            // Set the Occlum environment variable
            Environment.SetEnvironmentVariable("USE_OCCLUM", "1");

            // Create the Occlum interface
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<OcclumInterface>();
            return new OcclumInterface(logger, enclavePath, _serviceProvider);
        }

        /// <inheritdoc/>
        public ISgxEnclaveInterface CreateSgxInterface(string enclavePath, bool simulationMode)
        {
            _logger.LogInformation("Creating SGX interface with enclave path: {EnclavePath}, simulation mode: {SimulationMode}", enclavePath, simulationMode);

            // SGX is now supported through Occlum
            _logger.LogInformation("SGX is supported through Occlum.");
            return (ISgxEnclaveInterface)CreateOcclumInterface(enclavePath, simulationMode);
        }

        /// <summary>
        /// Registers the TEE interface services with the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register the TEE interface factory
            services.AddSingleton<ITeeInterfaceFactory, TeeInterfaceFactory>();

            // Register the Occlum interface
            services.AddSingleton<OcclumInterface>();

            // Register the Occlum manager
            services.AddSingleton<IOcclumManager, OcclumManager>();
        }
    }
}
