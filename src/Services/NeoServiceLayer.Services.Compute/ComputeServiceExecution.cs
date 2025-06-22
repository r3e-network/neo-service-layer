using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Compute;

/// <summary>
/// Execution methods for the Compute service.
/// </summary>
public partial class ComputeService
{
    /// <inheritdoc/>
    public async Task<ComputationResult> ExecuteComputationAsync(string computationId, IDictionary<string, string> parameters, BlockchainType blockchainType)
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
            if (!_computationCache.TryGetValue(computationId, out var metadata))
            {
                // Try to load it from the enclave
                metadata = await GetComputationMetadataAsync(computationId, blockchainType);
                if (metadata == null)
                {
                    throw new ArgumentException($"Computation with ID {computationId} does not exist.");
                }
            }

            // Prepare parameters
            var parametersJson = JsonSerializer.Serialize(parameters);

            // Get the computation metadata to retrieve the code
            var computationMetadata = await GetComputationMetadataInternalAsync(computationId);
            if (computationMetadata == null)
            {
                throw new ArgumentException($"Computation with ID {computationId} does not exist.");
            }

            // Execute computation in the enclave using the real compute function
            var startTime = DateTime.UtcNow;
            string result = await _enclaveManager.ExecuteJavaScriptAsync($"executeComputation('{computationId}', {JsonSerializer.Serialize(computationMetadata.ComputationCode ?? "")}, {parametersJson})");
            var endTime = DateTime.UtcNow;
            var executionTime = (endTime - startTime).TotalMilliseconds;

            // Parse the result from the enhanced compute function
            var enclaveResult = JsonSerializer.Deserialize<JsonElement>(result);

            ComputationResult computationResult;
            if (enclaveResult.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                // Extract the actual result
                var actualResult = enclaveResult.TryGetProperty("result", out var resultProp) ?
                    resultProp.ToString() : "null";

                computationResult = new ComputationResult
                {
                    ComputationId = computationId,
                    ResultData = actualResult,
                    ExecutionTimeMs = executionTime,
                    Timestamp = DateTime.UtcNow,
                    ResultId = Guid.NewGuid().ToString(),
                    BlockchainType = blockchainType,
                    Proof = "computed-proof"
                };
            }
            else
            {
                // Handle error case
                var errorMsg = enclaveResult.TryGetProperty("error", out var errorProp) ?
                    errorProp.GetString() : "Unknown error";

                computationResult = new ComputationResult
                {
                    ComputationId = computationId,
                    ResultData = $"{{\"error\": \"{errorMsg}\"}}",
                    ExecutionTimeMs = executionTime,
                    Timestamp = DateTime.UtcNow,
                    ResultId = Guid.NewGuid().ToString(),
                    BlockchainType = blockchainType,
                    Proof = "error-proof"
                };
            }

            // Update the result with additional information
            computationResult.ComputationId = computationId;
            computationResult.ExecutionTimeMs = executionTime;
            computationResult.Timestamp = endTime;
            computationResult.BlockchainType = blockchainType;
            computationResult.Parameters = parameters;

            // Generate a proof for the result
            string dataToSign = $"{computationId}:{computationResult.ResultData}:{JsonSerializer.Serialize(parameters)}";
            computationResult.Proof = await _enclaveManager.SignDataAsync(dataToSign, "compute-service-key");

            // Update the cache
            lock (_resultCache)
            {
                _resultCache[computationResult.ResultId] = computationResult;
            }

            // Update the computation metadata
            await UpdateComputationStatsAsync(computationId, executionTime);

            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return computationResult;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error executing computation {ComputationId} for blockchain {BlockchainType}",
                computationId, blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ComputationStatus> GetComputationStatusAsync(string computationId, BlockchainType blockchainType)
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
            if (!_computationCache.TryGetValue(computationId, out var metadata))
            {
                // Try to load it from the enclave
                metadata = await GetComputationMetadataAsync(computationId, blockchainType);
                if (metadata == null)
                {
                    return ComputationStatus.Failed;
                }
            }

            // For now, we don't have long-running computations, so it's either ready or not found
            _successCount++;
            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            return ComputationStatus.Completed;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error getting computation status for {ComputationId} for blockchain {BlockchainType}",
                computationId, blockchainType);
            return ComputationStatus.Failed;
        }
    }
}
