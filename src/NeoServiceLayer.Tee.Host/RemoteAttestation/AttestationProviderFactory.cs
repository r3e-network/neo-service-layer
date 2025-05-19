using System;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Occlum;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Factory for creating attestation providers.
    /// </summary>
    public class AttestationProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttestationProviderFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
        public AttestationProviderFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates an attestation provider for the specified enclave.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to use for attestation.</param>
        /// <returns>An attestation provider for the enclave.</returns>
        public IAttestationProvider CreateAttestationProvider(IntPtr enclaveId)
        {
            if (enclaveId == IntPtr.Zero)
            {
                throw new ArgumentException("Enclave ID cannot be zero", nameof(enclaveId));
            }

            // Check if Occlum is enabled
            if (Environment.GetEnvironmentVariable("USE_OCCLUM") == "1")
            {
                return CreateOcclumAttestationProvider();
            }
            else
            {
                // Create a logger for the attestation provider
                var logger = _loggerFactory.CreateLogger<OpenEnclaveAttestationProvider>();

                // Create the attestation provider
                return new OpenEnclaveAttestationProvider(logger, enclaveId);
            }
        }

        /// <summary>
        /// Creates an Occlum attestation provider.
        /// </summary>
        /// <returns>An Occlum attestation provider.</returns>
        public IAttestationProvider CreateOcclumAttestationProvider()
        {
            // Create a logger for the Occlum attestation provider
            var logger = _loggerFactory.CreateLogger<OcclumAttestationProvider>();

            // Create the Occlum manager
            var occlumLogger = _loggerFactory.CreateLogger<OcclumManager>();
            var occlumOptions = new OcclumOptions
            {
                InstanceDir = Environment.GetEnvironmentVariable("OCCLUM_INSTANCE_DIR") ?? "/occlum_instance",
                LogLevel = Environment.GetEnvironmentVariable("OCCLUM_LOG_LEVEL") ?? "info",
                NodeJsPath = Environment.GetEnvironmentVariable("OCCLUM_NODEJS_PATH") ?? "/bin/node",
                TempDir = Environment.GetEnvironmentVariable("OCCLUM_TEMP_DIR") ?? "/tmp"
            };
            var occlumManager = new OcclumManager(occlumLogger, occlumOptions);

            // Create the attestation provider
            return new OcclumAttestationProvider(logger, occlumManager);
        }

        /// <summary>
        /// Creates a mock attestation provider for testing.
        /// </summary>
        /// <returns>A mock attestation provider.</returns>
        public IAttestationProvider CreateMockAttestationProvider()
        {
            // Create a logger for the mock attestation provider
            var logger = _loggerFactory.CreateLogger<MockAttestationProvider>();

            // Create the mock attestation provider
            return new MockAttestationProvider(logger);
        }
    }
}
