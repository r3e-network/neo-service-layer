using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Attestation;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.JavaScriptExecution;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Host.RemoteAttestation;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Core functionality for the OcclumInterface.
    /// </summary>
    public partial class OcclumInterface : IDisposable
    {
        private readonly ILogger<OcclumInterface> _logger;
        private readonly string _enclavePath;
        private readonly OcclumManager _occlumManager;
        private readonly OcclumJavaScriptExecution _jsExecution;
        private readonly OcclumAttestation _attestation;
        private readonly IAttestationProvider _attestationProvider;
        private readonly IServiceProvider _serviceProvider;

        private EnclaveMeasurements _measurements;
        private bool _initialized;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the OcclumInterface class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        public OcclumInterface(
            ILogger<OcclumInterface> logger,
            string enclavePath,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _enclavePath = enclavePath ?? throw new ArgumentNullException(nameof(enclavePath));

            if (string.IsNullOrEmpty(enclavePath))
            {
                throw new ArgumentException("Enclave path cannot be null or empty", nameof(enclavePath));
            }

            if (!File.Exists(enclavePath))
            {
                throw new FileNotFoundException("Enclave file not found", enclavePath);
            }

            // Create Occlum options
            var options = new OcclumOptions
            {
                InstanceDir = Path.Combine(Path.GetDirectoryName(enclavePath), "occlum_instance"),
                LogLevel = "info",
                NodeJsPath = "/bin/node",
                TempDir = Path.Combine(Path.GetDirectoryName(enclavePath), "temp")
            };

            // Create the Occlum manager
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var occlumManagerLogger = loggerFactory.CreateLogger<OcclumManager>();
            _occlumManager = new OcclumManager(
                logger: occlumManagerLogger,
                options: options);

            // Initialize the attestation provider
            try
            {
                var attestationProviderFactory = new AttestationProviderFactory(loggerFactory);
                _attestationProvider = attestationProviderFactory.CreateOcclumAttestationProvider();
                _logger.LogInformation("Attestation provider initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize attestation provider. Falling back to mock implementation.");
                var attestationProviderFactory = new AttestationProviderFactory(loggerFactory);
                _attestationProvider = attestationProviderFactory.CreateMockAttestationProvider();
            }

            // Initialize the JavaScript execution component
            _jsExecution = new OcclumJavaScriptExecution(logger, _occlumManager);

            // Initialize the attestation component
            _attestation = new OcclumAttestation(logger, _attestationProvider);

            // Get enclave measurements
            _measurements = GetEnclaveMeasurementsInternal();

            _initialized = true;
            _logger.LogInformation("Occlum interface initialized successfully");
        }

        /// <summary>
        /// Gets the enclave measurements.
        /// </summary>
        private EnclaveMeasurements GetEnclaveMeasurementsInternal()
        {
            try
            {
                // Get measurements from attestation provider
                var measurements = _attestationProvider.GetMeasurements();
                return measurements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave measurements");
                return new EnclaveMeasurements
                {
                    MrEnclave = new byte[32],
                    MrSigner = new byte[32],
                    ProductId = 1,
                    SecurityVersion = 1,
                    Attributes = 0
                };
            }
        }

        /// <summary>
        /// Gets the MRENCLAVE measurement of the enclave.
        /// </summary>
        public byte[] MrEnclave => _measurements.MrEnclave;

        /// <summary>
        /// Gets the MRSIGNER measurement of the enclave.
        /// </summary>
        public byte[] MrSigner => _measurements.MrSigner;

        /// <summary>
        /// Gets the product ID of the enclave.
        /// </summary>
        public int ProductId => _measurements.ProductId;

        /// <summary>
        /// Gets the security version of the enclave.
        /// </summary>
        public int SecurityVersion => _measurements.SecurityVersion;

        /// <summary>
        /// Gets the attributes of the enclave.
        /// </summary>
        public int Attributes => _measurements.Attributes;

        /// <inheritdoc/>
        public byte[] GetMrEnclave() => MrEnclave;

        /// <inheritdoc/>
        public byte[] GetMrSigner() => MrSigner;

        /// <inheritdoc/>
        public void Initialize()
        {
            CheckDisposed();
            
            if (_initialized)
            {
                return;
            }

            try
            {
                // Perform synchronous initialization
                _occlumManager.Init();
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Occlum interface");
                throw new OcclumInitializationException("Failed to initialize Occlum enclave", ex);
            }
        }

        /// <inheritdoc/>
        public async Task InitializeOcclumAsync(string instanceDir, string logLevel)
        {
            CheckDisposed();
            
            try
            {
                await _occlumManager.InitializeInstanceAsync(instanceDir, logLevel);
                _logger.LogInformation("Occlum instance initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Occlum instance");
                throw new OcclumInitializationException("Failed to initialize Occlum instance", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteOcclumCommandAsync(string path, string[] args, string[] env)
        {
            CheckDisposed();
            
            try
            {
                return await _occlumManager.ExecuteCommandAsync(path, args, env);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Occlum command {Path}", path);
                throw new OcclumExecutionException("Failed to execute Occlum command", ex);
            }
        }

        /// <inheritdoc/>
        public string GetOcclumVersion()
        {
            CheckDisposed();
            
            try
            {
                return _occlumManager.GetVersion();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Occlum version");
                return "Unknown";
            }
        }

        /// <inheritdoc/>
        public bool IsOcclumSupportEnabled()
        {
            CheckDisposed();
            
            return _occlumManager.IsSupported();
        }

        /// <inheritdoc/>
        public string GetEnclaveConfiguration()
        {
            CheckDisposed();
            
            try
            {
                // Return the enclave configuration as a JSON string
                var config = new
                {
                    Type = "Occlum",
                    ProductId = ProductId,
                    SecurityVersion = SecurityVersion,
                    Attributes = Attributes,
                    OcclumVersion = GetOcclumVersion(),
                    OcclumSupport = IsOcclumSupportEnabled()
                };

                return System.Text.Json.JsonSerializer.Serialize(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave configuration");
                return "{}";
            }
        }

        /// <inheritdoc/>
        public async Task UpdateEnclaveConfigurationAsync(string configuration)
        {
            CheckDisposed();
            
            try
            {
                // Parse and apply the configuration
                var config = System.Text.Json.JsonSerializer.Deserialize<OcclumOptions>(configuration);
                
                // Update Occlum configuration
                await _occlumManager.UpdateConfigurationAsync(config);
                
                _logger.LogInformation("Enclave configuration updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enclave configuration");
                throw new OcclumConfigurationException("Failed to update enclave configuration", ex);
            }
        }

        /// <inheritdoc/>
        public IntPtr GetEnclaveId()
        {
            CheckDisposed();
            
            // Return the enclave ID
            // In Occlum, this is a mock value as the concept is different
            return new IntPtr(1);
        }

        /// <inheritdoc/>
        public byte[] GetRandomBytes(int length)
        {
            CheckDisposed();
            
            try
            {
                // Get random bytes from Occlum
                var randomBytes = new byte[length];
                new Random().NextBytes(randomBytes);  // Replace with actual Occlum implementation
                return randomBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random bytes");
                throw new EnclaveOperationException("Failed to get random bytes", ex);
            }
        }

        /// <inheritdoc/>
        public byte[] SignData(byte[] data)
        {
            CheckDisposed();
            
            try
            {
                // Sign data using Occlum
                // This is a placeholder - real implementation would use Occlum's signing functionality
                return _attestation.Sign(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw new EnclaveOperationException("Failed to sign data", ex);
            }
        }

        /// <inheritdoc/>
        public bool VerifySignature(byte[] data, byte[] signature)
        {
            CheckDisposed();
            
            try
            {
                // Verify signature using Occlum
                // This is a placeholder - real implementation would use Occlum's verification functionality
                return _attestation.Verify(data, signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                throw new EnclaveOperationException("Failed to verify signature", ex);
            }
        }

        /// <inheritdoc/>
        public byte[] GetAttestationReport(byte[] reportData)
        {
            CheckDisposed();
            
            try
            {
                // Get attestation report from Occlum
                return _attestation.GetAttestationReport(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation report");
                throw new EnclaveOperationException("Failed to get attestation report", ex);
            }
        }

        /// <inheritdoc/>
        public bool VerifyAttestationReport(byte[] report, byte[] expectedMrEnclave, byte[] expectedMrSigner)
        {
            CheckDisposed();
            
            try
            {
                // Verify attestation report using Occlum
                return _attestation.VerifyAttestationReport(report, expectedMrEnclave, expectedMrSigner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation report");
                throw new EnclaveOperationException("Failed to verify attestation report", ex);
            }
        }

        /// <inheritdoc/>
        public byte[] SealData(byte[] data)
        {
            CheckDisposed();
            
            try
            {
                // Seal data using Occlum
                // This is a placeholder - real implementation would use Occlum's sealing functionality
                return _attestation.SealData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sealing data");
                throw new EnclaveOperationException("Failed to seal data", ex);
            }
        }

        /// <inheritdoc/>
        public byte[] UnsealData(byte[] sealedData)
        {
            CheckDisposed();
            
            try
            {
                // Unseal data using Occlum
                // This is a placeholder - real implementation would use Occlum's unsealing functionality
                return _attestation.UnsealData(sealedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsealing data");
                throw new EnclaveOperationException("Failed to unseal data", ex);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the enclave.
        /// </summary>
        /// <param name="disposing">Whether the method is called from Dispose() or a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free managed resources
                _occlumManager?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Checks if the object has been disposed and throws an exception if it has.
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OcclumInterface));
            }
        }
    }
} 