using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests.Mocks;

namespace NeoServiceLayer.Tee.Host.Tests.Mocks
{
    // Mock classes for testing
    public class TeeEnclaveSettings
    {
        public string Type { get; set; } = "OpenEnclave";
        public EnclaveSettings EnclaveSettings { get; set; } = new EnclaveSettings();
        public JavaScriptEngineSettings JavaScriptEngine { get; set; } = new JavaScriptEngineSettings();
        public UserSecretsSettings UserSecrets { get; set; } = new UserSecretsSettings();
        public GasAccountingSettings GasAccounting { get; set; } = new GasAccountingSettings();
    }

    public class EnclaveSettings
    {
        public string EnclavePath { get; set; } = "bin/liboe_enclave.signed.so";
        public bool SimulationMode { get; set; } = true;
        public bool Debug { get; set; } = true;
        public bool OcclumSupport { get; set; } = true;
        public string OcclumInstanceDir { get; set; } = "/occlum_instance";
        public string OcclumLogLevel { get; set; } = "info";
    }

    public class JavaScriptEngineSettings
    {
        public int MaxMemoryMB { get; set; } = 512;
        public int MaxExecutionTimeMs { get; set; } = 5000;
        public bool EnableDebugger { get; set; } = false;
    }

    public class UserSecretsSettings
    {
        public int MaxSecretsPerUser { get; set; } = 100;
        public int MaxSecretSizeBytes { get; set; } = 4096;
    }

    public class GasAccountingSettings
    {
        public bool EnableGasAccounting { get; set; } = true;
        public long GasLimitPerExecution { get; set; } = 1000000;
        public double GasPriceMultiplier { get; set; } = 1.0;
    }

    public class TeeEnclaveSettingsValidator
    {
        private readonly ILogger _logger;

        public TeeEnclaveSettingsValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<string> Validate(TeeEnclaveSettings settings)
        {
            var errors = new List<string>();

            if (settings == null)
            {
                errors.Add("Settings cannot be null.");
                return errors;
            }

            // Validate TEE type
            if (string.IsNullOrEmpty(settings.Type))
            {
                errors.Add("TEE type cannot be null or empty.");
            }
            else if (settings.Type != "SGX" && settings.Type != "OpenEnclave")
            {
                errors.Add($"Invalid TEE type: {settings.Type}. Supported types are: SGX, OpenEnclave.");
            }

            return errors;
        }

        public void ValidateAndThrow(TeeEnclaveSettings settings)
        {
            var errors = Validate(settings);
            if (errors.Count > 0)
            {
                var errorMessage = string.Join(Environment.NewLine, errors);
                throw new ArgumentException(errorMessage, nameof(settings));
            }
        }
    }

    public class TeeEnclaveFactory : NeoServiceLayer.Tee.Host.Services.ITeeEnclaveFactory
    {
        private readonly ILogger<TeeEnclaveFactory> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public TeeEnclaveFactory(ILogger<TeeEnclaveFactory> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ITeeEnclaveInterface CreateEnclave(string enclavePath, bool simulationMode)
        {
            var mockOpenEnclaveInterface = _serviceProvider.GetService<MockOpenEnclaveInterface>();
            if (mockOpenEnclaveInterface == null)
            {
                throw new InvalidOperationException("MockOpenEnclaveInterface is not registered in the service provider.");
            }
            return mockOpenEnclaveInterface;
        }

        public ITeeEnclaveInterface CreateEnclaveInterface()
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
                    var mockSgxEnclaveInterface = _serviceProvider.GetService<MockSgxEnclaveInterface>();
                    if (mockSgxEnclaveInterface == null)
                    {
                        throw new InvalidOperationException("MockSgxEnclaveInterface is not registered in the service provider.");
                    }
                    return mockSgxEnclaveInterface;

                case "openenclave":
                case "oe":
                    _logger.LogInformation("Creating Open Enclave interface");
                    var mockOpenEnclaveInterface = _serviceProvider.GetService<MockOpenEnclaveInterface>();
                    if (mockOpenEnclaveInterface == null)
                    {
                        throw new InvalidOperationException("MockOpenEnclaveInterface is not registered in the service provider.");
                    }
                    return mockOpenEnclaveInterface;

                default:
                    _logger.LogWarning("Unknown TEE type: {TeeType}. Falling back to Open Enclave.", teeSettings.Type);
                    var defaultMockOpenEnclaveInterface = _serviceProvider.GetService<MockOpenEnclaveInterface>();
                    if (defaultMockOpenEnclaveInterface == null)
                    {
                        throw new InvalidOperationException("MockOpenEnclaveInterface is not registered in the service provider.");
                    }
                    return defaultMockOpenEnclaveInterface;
            }
        }

        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register the TEE enclave factory
            services.AddSingleton<NeoServiceLayer.Tee.Host.Services.ITeeEnclaveFactory, TeeEnclaveFactory>();

            // Register the enclave interfaces
            services.AddSingleton<MockOpenEnclaveInterface>();
            services.AddSingleton<MockSgxEnclaveInterface>();
        }
    }

}

namespace NeoServiceLayer.Tee.Host.Tests.Mocks.Exceptions
{
    [Serializable]
    public class EnclaveException : Exception
    {
        public EnclaveException() : base("An error occurred in the enclave.")
        {
        }

        public EnclaveException(string message) : base(message)
        {
        }

        public EnclaveException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class EnclaveOperationException : EnclaveException
    {
        public int ErrorCode { get; }

        public EnclaveOperationException() : base("An enclave operation failed.")
        {
        }

        public EnclaveOperationException(string message) : base(message)
        {
        }

        public EnclaveOperationException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public EnclaveOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
