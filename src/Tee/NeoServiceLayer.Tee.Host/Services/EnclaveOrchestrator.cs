using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace NeoServiceLayer.Tee.Host.Services;

/// <summary>
/// Orchestrator for enclave operations, delegating to specialized services.
/// Replaces the god object EnclaveManager with a more maintainable architecture.
/// </summary>
public class EnclaveOrchestrator : IEnclaveOrchestrator
{
    private readonly ILogger<EnclaveOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnclaveKmsService _kmsService;
    private readonly IEnclaveStorageService _storageService;
    private readonly IEnclaveComplianceService _complianceService;
    private readonly IEnclaveOracleService _oracleService;
    private readonly IEnclaveComputeService _computeService;

    public EnclaveOrchestrator(
        ILogger<EnclaveOrchestrator> logger,
        IServiceProvider serviceProvider,
        IEnclaveKmsService kmsService,
        IEnclaveStorageService storageService,
        IEnclaveComplianceService complianceService,
        IEnclaveOracleService oracleService,
        IEnclaveComputeService computeService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _kmsService = kmsService ?? throw new ArgumentNullException(nameof(kmsService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _complianceService = complianceService ?? throw new ArgumentNullException(nameof(complianceService));
        _oracleService = oracleService ?? throw new ArgumentNullException(nameof(oracleService));
        _computeService = computeService ?? throw new ArgumentNullException(nameof(computeService));
    }

    /// <summary>
    /// Initializes all enclave services.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Enclave Orchestrator and all services");

        try
        {
            // Initialize services in parallel where possible
            var initTasks = new[]
            {
                _kmsService.InitializeAsync(cancellationToken),
                _storageService.InitializeAsync(cancellationToken),
                _complianceService.InitializeAsync(cancellationToken),
                _oracleService.InitializeAsync(cancellationToken),
                _computeService.InitializeAsync(cancellationToken)
            };

            await Task.WhenAll(initTasks).ConfigureAwait(false);

            _logger.LogInformation("Enclave Orchestrator initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Enclave Orchestrator");
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within the appropriate enclave service.
    /// </summary>
    public async Task<T> ExecuteOperationAsync<T>(
        EnclaveOperation operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing operation {Operation} in enclave", operation);

        try
        {
            // Route to appropriate service based on operation type
            return operation switch
            {
                EnclaveOperation.KeyManagement => await _kmsService.ExecuteAsync(action, cancellationToken).ConfigureAwait(false),
                EnclaveOperation.Storage => await _storageService.ExecuteAsync(action, cancellationToken).ConfigureAwait(false),
                EnclaveOperation.Compliance => await _complianceService.ExecuteAsync(action, cancellationToken).ConfigureAwait(false),
                EnclaveOperation.Oracle => await _oracleService.ExecuteAsync(action, cancellationToken).ConfigureAwait(false),
                EnclaveOperation.Compute => await _computeService.ExecuteAsync(action, cancellationToken).ConfigureAwait(false),
                _ => throw new NotSupportedException($"Operation {operation} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute operation {Operation}", operation);
            throw;
        }
    }

    /// <summary>
    /// Gets the health status of all enclave services.
    /// </summary>
    public async Task<EnclaveHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = await Task.WhenAll(
            _kmsService.CheckHealthAsync(cancellationToken),
            _storageService.CheckHealthAsync(cancellationToken),
            _complianceService.CheckHealthAsync(cancellationToken),
            _oracleService.CheckHealthAsync(cancellationToken),
            _computeService.CheckHealthAsync(cancellationToken)
        ).ConfigureAwait(false);

        return new EnclaveHealthStatus
        {
            IsHealthy = Array.TrueForAll(healthChecks, h => h),
            KmsServiceHealthy = healthChecks[0],
            StorageServiceHealthy = healthChecks[1],
            ComplianceServiceHealthy = healthChecks[2],
            OracleServiceHealthy = healthChecks[3],
            ComputeServiceHealthy = healthChecks[4],
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Shuts down all enclave services gracefully.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down Enclave Orchestrator");

        try
        {
            var shutdownTasks = new[]
            {
                _kmsService.ShutdownAsync(cancellationToken),
                _storageService.ShutdownAsync(cancellationToken),
                _complianceService.ShutdownAsync(cancellationToken),
                _oracleService.ShutdownAsync(cancellationToken),
                _computeService.ShutdownAsync(cancellationToken)
            };

            await Task.WhenAll(shutdownTasks).ConfigureAwait(false);

            _logger.LogInformation("Enclave Orchestrator shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Enclave Orchestrator shutdown");
            throw;
        }
    }
}

/// <summary>
/// Interface for the enclave orchestrator.
/// </summary>
public interface IEnclaveOrchestrator
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteOperationAsync<T>(EnclaveOperation operation, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task<EnclaveHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of enclave operations.
/// </summary>
public enum EnclaveOperation
{
    KeyManagement,
    Storage,
    Compliance,
    Oracle,
    Compute
}

/// <summary>
/// Health status of enclave services.
/// </summary>
public class EnclaveHealthStatus
{
    public bool IsHealthy { get; set; }
    public bool KmsServiceHealthy { get; set; }
    public bool StorageServiceHealthy { get; set; }
    public bool ComplianceServiceHealthy { get; set; }
    public bool OracleServiceHealthy { get; set; }
    public bool ComputeServiceHealthy { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Base interface for specialized enclave services.
/// </summary>
public interface IEnclaveService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for KMS operations in enclave.
/// </summary>
public interface IEnclaveKmsService : IEnclaveService
{
    Task<string> GenerateKeyAsync(string keyId, string keyType, CancellationToken cancellationToken = default);
    Task<string> SignDataAsync(string keyId, byte[] data, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(string keyId, byte[] data, byte[] signature, CancellationToken cancellationToken = default);
    Task<byte[]> EncryptAsync(string keyId, byte[] plaintext, CancellationToken cancellationToken = default);
    Task<byte[]> DecryptAsync(string keyId, byte[] ciphertext, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for storage operations in enclave.
/// </summary>
public interface IEnclaveStorageService : IEnclaveService
{
    Task<string> StoreAsync(string key, byte[] data, CancellationToken cancellationToken = default);
    Task<byte[]> RetrieveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<string[]> ListKeysAsync(string prefix, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for compliance operations in enclave.
/// </summary>
public interface IEnclaveComplianceService : IEnclaveService
{
    Task<bool> CheckTransactionComplianceAsync(string transactionData, string[] rules, CancellationToken cancellationToken = default);
    Task<string> GenerateComplianceReportAsync(string reportType, CancellationToken cancellationToken = default);
    Task<bool> ValidateKycAsync(string userData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for oracle operations in enclave.
/// </summary>
public interface IEnclaveOracleService : IEnclaveService
{
    Task<string> FetchDataAsync(string url, CancellationToken cancellationToken = default);
    Task<decimal> GetPriceAsync(string symbol, string source, CancellationToken cancellationToken = default);
    Task<string> AggregateDataAsync(string[] sources, string method, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for compute operations in enclave.
/// </summary>
public interface IEnclaveComputeService : IEnclaveService
{
    Task<string> ExecuteScriptAsync(string script, string runtime, CancellationToken cancellationToken = default);
    Task<byte[]> ProcessDataAsync(byte[] input, string operation, CancellationToken cancellationToken = default);
    Task<string> RunMachineLearningModelAsync(string modelId, byte[] inputData, CancellationToken cancellationToken = default);
}