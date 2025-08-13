using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Oracle.Models;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Batch operations functionality for the Oracle service.
/// </summary>
public partial class OracleService
{
    /// <inheritdoc/>
    public async Task<OracleBatchResponse> BatchRequestAsync(IEnumerable<OracleRequest> requests, BlockchainType blockchainType)
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

        var requestsList = requests.ToList();
        if (!requestsList.Any())
        {
            throw new ArgumentException("Request collection cannot be empty.", nameof(requests));
        }

        var maxRequestsPerBatch = int.Parse(_configuration.GetValue("Oracle:MaxRequestsPerBatch", "10"));
        if (requestsList.Count > maxRequestsPerBatch)
        {
            throw new ArgumentException($"Number of requests in a batch cannot exceed {maxRequestsPerBatch}.", nameof(requests));
        }

        try
        {
            _requestCount += requestsList.Count;
            _lastRequestTime = DateTime.UtcNow;

            // Get blockchain data for verification
            var client = _blockchainClientFactory.CreateClient(blockchainType);
            var blockHeight = await client.GetBlockHeightAsync();
            var block = await client.GetBlockAsync(blockHeight);
            var blockHash = block.Hash;

            // Process batch requests using privacy-preserving operations
            var privacyBatchResult = await ProcessBatchRequestsWithPrivacyAsync(requestsList);

            Logger.LogDebug("Privacy-preserving batch oracle processing completed: BatchId={BatchId}, ResultCount={Count}",
                privacyBatchResult.BatchId, privacyBatchResult.Results.Count);

            // Process each request
            var responses = new List<OracleResponse>();
            foreach (var request in requestsList)
            {
                try
                {
                    // Update data source access statistics
                    lock (_dataSources)
                    {
                        var existingDataSource = _dataSources.FirstOrDefault(ds => ds.Url == request.Url && ds.BlockchainType == blockchainType);
                        if (existingDataSource != null)
                        {
                            existingDataSource.LastAccessedAt = DateTime.UtcNow;
                            existingDataSource.AccessCount++;
                        }
                    }

                    // Get data from the enclave
                    string data;
                    if (IsEnclaveInitialized)
                    {
                        data = await _enclaveManager.GetDataAsync(request.Url, request.Path);
                    }
                    else
                    {
                        // Fallback to mock data
                        data = $"{{\"value\": 42, \"source\": \"{request.Url}\", \"path\": \"{request.Path}\", \"timestamp\": \"{DateTime.UtcNow}\"}}";
                    }

                    // Create a signature for the data
                    var dataToSign = $"{request.Url}:{request.Path}:{data}:{blockHash}";
                    string privateKeyHex = "6f7261636c652d736572766963652d6b6579"; // "oracle-service-key" in hex
                    string signatureHex = await _enclaveManager.SignDataAsync(dataToSign, privateKeyHex);
                    var signature = Convert.FromHexString(signatureHex);

                    // Create the response
                    var response = new OracleResponse
                    {
                        RequestId = request.RequestId,
                        Data = data,
                        Proof = Convert.ToBase64String(signature),
                        Timestamp = DateTime.UtcNow,
                        BlockchainType = blockchainType,
                        BlockHeight = blockHeight,
                        BlockHash = blockHash,
                        Signature = Convert.ToBase64String(signature)
                    };

                    responses.Add(response);
                    _successCount++;
                }
                catch (Exception ex)
                {
                    _failureCount++;
                    Logger.LogError(ex, "Error processing request {RequestId} for URL {Url} and path {Path}",
                        request.RequestId, request.Url, request.Path);
                    // Continue with other requests even if one fails
                }
            }

            // Create a batch signature
            var batchId = Guid.NewGuid().ToString();
            var batchData = string.Join(",", responses.Select(r => r.RequestId));
            var batchDataToSign = $"{batchId}:{batchData}:{blockHash}";
            string batchPrivateKeyHex = "6f7261636c652d736572766963652d62617463682d6b6579"; // "oracle-service-batch-key" in hex
            string batchSignatureHex = await _enclaveManager.SignDataAsync(batchDataToSign, batchPrivateKeyHex);
            var batchSignature = Convert.FromHexString(batchSignatureHex);

            // Create the batch response
            var batchResponse = new OracleBatchResponse
            {
                BatchId = batchId,
                Responses = responses,
                Proof = Convert.ToBase64String(batchSignature),
                Timestamp = DateTime.UtcNow,
                BlockchainType = blockchainType,
                BlockHeight = blockHeight,
                BlockHash = blockHash,
                Signature = Convert.ToBase64String(batchSignature)
            };

            UpdateMetric("LastSuccessTime", DateTime.UtcNow);
            UpdateMetric("BatchRequestCount", _requestCount);
            return batchResponse;
        }
        catch (Exception ex)
        {
            _failureCount++;
            UpdateMetric("LastFailureTime", DateTime.UtcNow);
            UpdateMetric("LastErrorMessage", ex.Message);
            Logger.LogError(ex, "Error processing batch request for blockchain {BlockchainType}",
                blockchainType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OracleResponse>> FetchDataBatchAsync(IEnumerable<OracleRequest> requests, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var responses = new List<OracleResponse>();

        foreach (var request in requests)
        {
            var response = await FetchDataAsync(request, blockchainType);
            responses.Add(response);
        }

        return responses;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyResponseAsync(OracleResponse response)
    {
        if (!SupportsBlockchain(response.BlockchainType))
        {
            throw new NotSupportedException($"Blockchain type {response.BlockchainType} is not supported.");
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
            // Verify the signature
            var dataToVerify = $"{response.RequestId}:{response.Data}:{response.BlockHash}";
            string publicKeyHex = "6f7261636c652d736572766963652d7075626b6579"; // "oracle-service-pubkey" in hex
            bool isValid = await _enclaveManager.VerifySignatureAsync(dataToVerify, response.Signature, publicKeyHex);

            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying response for request {RequestId}",
                response.RequestId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyDataAsync(OracleResponse response, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(response);
        return await VerifyResponseAsync(response);
    }
}
