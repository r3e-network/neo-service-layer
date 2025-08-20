using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Adapter that converts IEnclaveWrapper to IEnclaveService for dependency injection.
/// </summary>
public class EnclaveServiceAdapter : IEnclaveService
{
    private readonly IEnclaveWrapper _enclaveWrapper;
    private readonly ILogger<EnclaveServiceAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnclaveServiceAdapter"/> class.
    /// </summary>
    /// <param name="enclaveWrapper">The enclave wrapper.</param>
    /// <param name="logger">The logger.</param>
    public EnclaveServiceAdapter(IEnclaveWrapper enclaveWrapper, ILogger<EnclaveServiceAdapter> logger)
    {
        _enclaveWrapper = enclaveWrapper ?? throw new ArgumentNullException(nameof(enclaveWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Name => "Enclave Service";

    /// <inheritdoc/>
    public string Description => "Provides secure enclave operations using SGX/Occlum LibOS";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public bool IsEnclaveInitialized { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<object> Dependencies => Array.Empty<object>();

    /// <inheritdoc/>
    public IEnumerable<Type> Capabilities => new[] { typeof(IEnclaveWrapper) };

    /// <inheritdoc/>
    public IDictionary<string, string> Metadata => new Dictionary<string, string>
    {
        ["Type"] = "Enclave",
        ["Platform"] = "SGX/Occlum",
        ["Security"] = "Hardware-based TEE"
    };

    /// <inheritdoc/>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing enclave service...");
            IsEnclaveInitialized = _enclaveWrapper.Initialize();
            IsRunning = IsEnclaveInitialized;

            if (IsRunning)
            {
                _logger.LogInformation("Enclave service initialized successfully");
            }
            else
            {
                _logger.LogError("Failed to initialize enclave service");
            }

            return await Task.FromResult(IsRunning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing enclave service");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> InitializeEnclaveAsync()
    {
        try
        {
            _logger.LogInformation("Initializing enclave...");
            IsEnclaveInitialized = _enclaveWrapper.Initialize();

            if (IsEnclaveInitialized)
            {
                _logger.LogInformation("Enclave initialized successfully");
            }
            else
            {
                _logger.LogError("Failed to initialize enclave");
            }

            return await Task.FromResult(IsEnclaveInitialized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StartAsync()
    {
        try
        {
            _logger.LogInformation("Starting enclave service...");

            if (!IsEnclaveInitialized)
            {
                IsEnclaveInitialized = _enclaveWrapper.Initialize();
            }

            IsRunning = IsEnclaveInitialized;

            if (IsRunning)
            {
                _logger.LogInformation("Enclave service started successfully");
            }
            else
            {
                _logger.LogError("Failed to start enclave service");
            }

            return await Task.FromResult(IsRunning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting enclave service");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopAsync()
    {
        try
        {
            _logger.LogInformation("Stopping enclave service...");
            _enclaveWrapper.Dispose();
            IsRunning = false;
            IsEnclaveInitialized = false;
            _logger.LogInformation("Enclave service stopped successfully");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping enclave service");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceHealth> GetHealthAsync()
    {
        try
        {
            if (!IsRunning || !IsEnclaveInitialized)
            {
                return await Task.FromResult(ServiceHealth.NotRunning);
            }

            // Try a simple operation to check health
            var trustedTime = _enclaveWrapper.GetTrustedTime();
            return await Task.FromResult(trustedTime > 0 ? ServiceHealth.Healthy : ServiceHealth.Unhealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enclave health");
            return await Task.FromResult(ServiceHealth.Unhealthy);
        }
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, object>> GetMetricsAsync()
    {
        var metrics = new Dictionary<string, object>
        {
            ["IsRunning"] = IsRunning,
            ["IsEnclaveInitialized"] = IsEnclaveInitialized,
            ["TrustedTime"] = IsEnclaveInitialized ? _enclaveWrapper.GetTrustedTime() : 0,
            ["LastHealthCheck"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return await Task.FromResult(metrics);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateDependenciesAsync(IEnumerable<IService> availableServices)
    {
        // Enclave service has no external dependencies
        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAttestationAsync()
    {
        try
        {
            _logger.LogInformation("Getting attestation report...");

            if (!IsEnclaveInitialized)
            {
                throw new InvalidOperationException("Enclave is not initialized");
            }

            var attestation = _enclaveWrapper.GetAttestation();

            // Convert attestation to JSON string
            var report = new
            {
                Provider = "SGX",
                Timestamp = DateTime.UtcNow,
                Data = Convert.ToBase64String(attestation ?? Array.Empty<byte>()),
                IsValid = attestation != null && attestation.Length > 0
            };

            return await Task.FromResult(System.Text.Json.JsonSerializer.Serialize(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attestation");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateEnclaveAsync()
    {
        try
        {
            _logger.LogInformation("Validating enclave...");

            if (!IsEnclaveInitialized)
            {
                return false;
            }

            // Perform validation checks
            var trustedTime = _enclaveWrapper.GetTrustedTime();
            var attestation = _enclaveWrapper.GetAttestation();

            var isValid = trustedTime > 0 && attestation != null && attestation.Length > 0;

            if (isValid)
            {
                _logger.LogInformation("Enclave validation successful");
            }
            else
            {
                _logger.LogWarning("Enclave validation failed");
            }

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating enclave");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool HasEnclaveCapabilities => true;
}
