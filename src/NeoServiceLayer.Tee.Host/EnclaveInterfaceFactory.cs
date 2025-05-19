using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Enclave;

namespace NeoServiceLayer.Tee.Host
{
    public class EnclaveInterfaceFactory
    {
        private readonly ILogger<EnclaveInterfaceFactory> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public EnclaveInterfaceFactory(
            ILogger<EnclaveInterfaceFactory> logger,
            IConfiguration configuration,
            ILoggerFactory? loggerFactory = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public ITeeEnclaveInterface CreateEnclaveInterface()
        {
            string enclaveType = _configuration["Enclave:Type"] ?? "OpenEnclave";
            string enclavePath = _configuration["Enclave:Path"];

            if (string.IsNullOrEmpty(enclavePath))
            {
                // Try to find the enclave in the default locations
                string[] possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liboe_enclave.signed.so"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "liboe_enclave.signed.so"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "liboe_enclave.signed.dll"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "liboe_enclave.signed.dll")
                };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        enclavePath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(enclavePath))
                {
                    throw new FileNotFoundException("Enclave file not found. Please specify the path in configuration.");
                }
            }

            _logger.LogInformation("Creating enclave interface of type {EnclaveType} with path {EnclavePath}", enclaveType, enclavePath);

            switch (enclaveType.ToLowerInvariant())
            {
                case "openenclave":
                    return new OpenEnclaveInterface(
                        _loggerFactory.CreateLogger<OpenEnclaveInterface>(),
                        enclavePath);

                // Open Enclave is the default and preferred option
                default:
                    _logger.LogWarning("Unsupported enclave type: {EnclaveType}. Falling back to Open Enclave.", enclaveType);
                    return new OpenEnclaveInterface(
                        _loggerFactory.CreateLogger<OpenEnclaveInterface>(),
                        enclavePath);
            }
        }
    }
}
