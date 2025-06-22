using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Compute;

/// <summary>
/// Management methods for the Compute service.
/// </summary>
public partial class ComputeService
{
    /// <inheritdoc/>
    public async Task<bool> RegisterComputationAsync(string computationId, string computationCode, string computationType, string description, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check if the computation already exists
            if (_computationCache.ContainsKey(computationId))
            {
                throw new ArgumentException($"Computation with ID {computationId} already exists.");
            }

            // Register the computation in the enclave
            var payload = JsonSerializer.Serialize(new
            {
                ComputationId = computationId,
                ComputationCode = computationCode,
                ComputationType = computationType,
                Description = description
            });

            string result = await _enclaveManager.ExecuteJavaScriptAsync($"registerComputation({payload})");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Create metadata
                var metadata = new ComputationMetadata
                {
                    ComputationId = computationId,
                    ComputationType = computationType,
                    Description = description,
                    ComputationCode = computationCode,
                    CreatedAt = DateTime.UtcNow,
                    ExecutionCount = 0,
                    AverageExecutionTimeMs = 0
                };

                // Update the cache
                lock (_computationCache)
                {
                    _computationCache[computationId] = metadata;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return success;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error registering computation {ComputationId} for blockchain {BlockchainType}",
                computationId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UnregisterComputationAsync(string computationId, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check if the computation exists
            if (!_computationCache.ContainsKey(computationId))
            {
                throw new ArgumentException($"Computation with ID {computationId} does not exist.");
            }

            // Unregister the computation in the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"unregisterComputation('{computationId}')");
            bool success = JsonSerializer.Deserialize<bool>(result);

            if (success)
            {
                // Remove from cache
                lock (_computationCache)
                {
                    _computationCache.Remove(computationId);
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return success;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error unregistering computation {ComputationId} for blockchain {BlockchainType}",
                computationId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ComputationMetadata>> ListComputationsAsync(int skip, int take, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // List computations from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"listComputations({skip}, {take})");

            // Parse the result
            var computationList = JsonSerializer.Deserialize<List<ComputationMetadata>>(result) ??
                throw new InvalidOperationException("Failed to deserialize computation list.");

            // Update the cache
            lock (_computationCache)
            {
                foreach (var computation in computationList)
                {
                    _computationCache[computation.ComputationId] = computation;
                }
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return computationList;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error listing computations for blockchain {BlockchainType}", blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComputationMetadata> GetComputationMetadataAsync(string computationId, BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        if (!IsRunning)
        {
            throw new InvalidOperationException("Service is not running.");
        }

        try
        {
            _requestCount++;
            _lastRequestTime = DateTime.UtcNow;

            // Check the cache first
            lock (_computationCache)
            {
                if (_computationCache.TryGetValue(computationId, out var cachedMetadata))
                {
                    _successCount++;
                    UpdateMetric("LastSuccessTime", DateTime.UtcNow);
                    return cachedMetadata;
                }
            }

            // Get computation metadata from the enclave
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"getComputationMetadata('{computationId}')");

            // Parse the result
            var metadata = JsonSerializer.Deserialize<ComputationMetadata>(result) ??
                throw new ArgumentException($"Computation with ID {computationId} does not exist.");

            // Update the cache
            lock (_computationCache)
            {
                _computationCache[computationId] = metadata;
            }

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return metadata;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting computation metadata for {ComputationId} for blockchain {BlockchainType}",
                computationId, blockchainType);
            throw;
        }
    }
}
