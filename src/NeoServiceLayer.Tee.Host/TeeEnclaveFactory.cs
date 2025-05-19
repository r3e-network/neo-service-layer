using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Factory for creating TEE enclave interfaces.
    /// This factory provides a unified way to create enclave interfaces
    /// for different TEE implementations (SGX, Occlum).
    /// </summary>
    public class TeeEnclaveFactory : ITeeEnclaveFactory
    {
        private readonly ILogger<TeeEnclaveFactory> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeeEnclaveFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public TeeEnclaveFactory(
            ILogger<TeeEnclaveFactory> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates an enclave interface.
        /// </summary>
        /// <returns>The enclave interface.</returns>
        public ITeeEnclaveInterface CreateEnclaveInterface()
        {
            return CreateEnclave(null, false);
        }

        /// <summary>
        /// Creates an enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The enclave interface.</returns>
        public ITeeEnclaveInterface CreateEnclave(string enclavePath, bool simulationMode)
        {
            // Get the TEE settings from configuration
            var teeSettings = _configuration.GetSection("Tee").Get<TeeEnclaveSettings>();
            if (teeSettings == null)
            {
                _logger.LogWarning("TEE settings not found in configuration. Using default settings.");
                teeSettings = new TeeEnclaveSettings();
            }

            // Validate the settings
            var validator = new TeeEnclaveSettingsValidator(_logger);
            try
            {
                validator.ValidateAndThrow(teeSettings);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid TEE settings. Using default settings.");
                teeSettings = new TeeEnclaveSettings();
            }

            _logger.LogInformation("Creating enclave interface for TEE type: {TeeType}", teeSettings.Type);

            // Create the appropriate enclave interface based on the TEE type
            switch (teeSettings.Type.ToLowerInvariant())
            {
                case "sgx":
                    _logger.LogInformation("Creating SGX enclave interface");
                    // SGX is supported through Occlum
                    return _serviceProvider.GetRequiredService<OcclumInterface>();

                case "occlum":
                    _logger.LogInformation("Creating Occlum interface");
                    return _serviceProvider.GetRequiredService<OcclumInterface>();

                default:
                    _logger.LogWarning("Unknown TEE type: {TeeType}. Falling back to Occlum.", teeSettings.Type);
                    return _serviceProvider.GetRequiredService<OcclumInterface>();
            }
        }

        /// <summary>
        /// Registers the TEE enclave services with the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Get the TEE type from configuration
            string teeType = configuration.GetValue<string>("Tee:Type", "Occlum");

            // Register the appropriate enclave interface based on the TEE type
            switch (teeType.ToLowerInvariant())
            {
                case "sgx":
                    // SGX is supported through Occlum
                    services.AddSingleton<OcclumInterface>();
                    break;

                case "occlum":
                    services.AddSingleton<OcclumInterface>();
                    break;

                default:
                    services.AddSingleton<OcclumInterface>();
                    break;
            }

            // Register the factory
            services.AddSingleton<ITeeEnclaveFactory, TeeEnclaveFactory>();
        }
    }
}
