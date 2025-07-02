using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Compute;

/// <summary>
/// Core implementation of the Compute service.
/// </summary>
public partial class ComputeService : EnclaveBlockchainServiceBase, IComputeService
{
    private new readonly IEnclaveManager _enclaveManager;
    private readonly IServiceConfiguration _configuration;
    private readonly Dictionary<string, ComputationMetadata> _computationCache = new();
    private readonly Dictionary<string, ComputationResult> _resultCache = new();
    private int _requestCount;
    private int _successCount;
    private int _failureCount;
    private DateTime _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeService"/> class.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ComputeService(
        IEnclaveManager enclaveManager,
        IServiceConfiguration configuration,
        ILogger<ComputeService> logger,
        IServiceProvider? serviceProvider = null)
        : base("Compute", "High-Performance Verifiable Compute Service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })
    {
        _enclaveManager = enclaveManager;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _requestCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _lastRequestTime = DateTime.MinValue;

        // Add capabilities
        AddCapability<IComputeService>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("MaxComputationCount", _configuration.GetValue("Compute:MaxComputationCount", "1000"));
        SetMetadata("MaxExecutionTimeMs", _configuration.GetValue("Compute:MaxExecutionTimeMs", "30000"));
        SetMetadata("SupportedComputationTypes", "JavaScript,WebAssembly");

        // Add dependencies
        AddRequiredDependency<IEnclaveService>("EnclaveManager", "1.0.0");
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Compute Service enclave...");
            await _enclaveManager.InitializeAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Compute Service enclave.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting Compute Service...");

            // Load existing computations from the enclave
            await RefreshComputationCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Compute Service.");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping Compute Service...");
            _computationCache.Clear();
            _resultCache.Clear();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Compute Service.");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Refreshes the computation cache from the enclave.
    /// </summary>
    private async Task RefreshComputationCacheAsync()
    {
        try
        {
            string result = await _enclaveManager.ExecuteJavaScriptAsync("listAllComputations()");
            var computations = JsonSerializer.Deserialize<List<ComputationMetadata>>(result) ?? new List<ComputationMetadata>();

            lock (_computationCache)
            {
                _computationCache.Clear();
                foreach (var computation in computations)
                {
                    _computationCache[computation.ComputationId] = computation;
                }
            }

            Logger.LogInformation("Loaded {Count} computations from enclave", computations.Count);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to refresh computation cache from enclave");
        }
    }

    /// <summary>
    /// Updates computation statistics.
    /// </summary>
    private async Task UpdateComputationStatsAsync(string computationId, double executionTimeMs)
    {
        try
        {
            if (_computationCache.TryGetValue(computationId, out var metadata))
            {
                metadata.ExecutionCount++;
                metadata.AverageExecutionTimeMs = (metadata.AverageExecutionTimeMs * (metadata.ExecutionCount - 1) + executionTimeMs) / metadata.ExecutionCount;
                metadata.LastUsedAt = DateTime.UtcNow;

                // Update in enclave
                var payload = JsonSerializer.Serialize(new
                {
                    ComputationId = computationId,
                    ExecutionCount = metadata.ExecutionCount,
                    AverageExecutionTimeMs = metadata.AverageExecutionTimeMs,
                    LastUsedAt = metadata.LastUsedAt
                });

                await _enclaveManager.ExecuteJavaScriptAsync($"updateComputationStats({payload})");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update computation statistics for {ComputationId}", computationId);
        }
    }

    /// <summary>
    /// Gets computation metadata internally.
    /// </summary>
    private async Task<ComputationMetadata?> GetComputationMetadataInternalAsync(string computationId)
    {
        try
        {
            // Check cache first
            if (_computationCache.TryGetValue(computationId, out var cachedMetadata))
            {
                return cachedMetadata;
            }

            // Get from enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getComputationMetadata('{computationId}')");
            var metadata = JsonSerializer.Deserialize<ComputationMetadata>(result);

            if (metadata != null)
            {
                lock (_computationCache)
                {
                    _computationCache[computationId] = metadata;
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get computation metadata for {ComputationId}", computationId);
            return null;
        }
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check enclave health
            if (!IsEnclaveInitialized)
                return ServiceHealth.Degraded;

            // Check if we can execute basic operations
            var healthCheck = await _enclaveManager.ExecuteJavaScriptAsync("healthCheck()");
            var isHealthy = healthCheck?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            return isHealthy ? ServiceHealth.Healthy : ServiceHealth.Degraded;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return ServiceHealth.Unhealthy;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Compute Service...");

            // Initialize persistent storage
            await InitializePersistentStorageAsync();

            // Initialize service-specific components
            await RefreshComputationCacheAsync();

            Logger.LogInformation("Compute Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Compute Service");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyComputationResultAsync(ComputationResult result, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            var request = new
            {
                Result = result,
                BlockchainType = blockchainType.ToString(),
                Timestamp = DateTime.UtcNow
            };

            var verificationResult = await _enclaveManager.ExecuteJavaScriptAsync($"verifyComputationResult({JsonSerializer.Serialize(request)})");
            var isValid = verificationResult?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            if (isValid)
            {
                _successCount++;
                Logger.LogInformation("Verified computation result {ComputationId}", result.ComputationId);
            }
            else
            {
                _failureCount++;
                Logger.LogWarning("Failed to verify computation result {ComputationId}", result.ComputationId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _failureCount++;
            Logger.LogError(ex, "Error verifying computation result {ComputationId}", result.ComputationId);
            throw;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposePersistenceResources();
        }
        base.Dispose(disposing);
    }
}
