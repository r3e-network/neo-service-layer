using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Text.Json;
using NeoServiceLayer.Services.Oracle.Models;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Core.SGX;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Enclave operations for the Oracle Service.
/// </summary>
public partial class OracleService
{
    /// <summary>
    /// Fetches and processes data using privacy-preserving operations in the SGX enclave.
    /// </summary>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The data path.</param>
    /// <param name="request">The oracle request.</param>
    /// <returns>The privacy-preserving oracle result.</returns>
    private async Task<PrivacyOracleResult> FetchDataWithPrivacyAsync(
        string dataSource, string dataPath, OracleRequest? request = null)
    {
        if (_enclaveManager == null)
        {
            // Fallback if enclave not available
            return new PrivacyOracleResult
            {
                RequestId = request?.RequestId ?? Guid.NewGuid().ToString(),
                DataHash = HashData(dataSource + dataPath),
                SourceProof = GenerateSimpleSourceProof(dataSource, dataPath),
                Success = true
            };
        }

        // Prepare oracle data for privacy-preserving fetching
        var oracleData = new
        {
            dataSource = new
            {
                url = dataSource,
                protocol = new Uri(dataSource).Scheme,
                host = new Uri(dataSource).Host,
                path = dataPath
            },
            verification = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                nonce = Guid.NewGuid().ToString(),
                requestId = request?.RequestId ?? Guid.NewGuid().ToString()
            }
        };

        var requestInfo = new
        {
            feedId = request?.FeedId ?? "direct",
            parameters = request?.Parameters ?? new Dictionary<string, object>(),
            blockchain = request?.BlockchainType.ToString() ?? "NeoN3"
        };

        var operation = "fetch";

        var jsParams = new
        {
            operation,
            oracleData,
            requestInfo
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        // Execute privacy-preserving data fetching in SGX
        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.OracleOperations,
            paramsJson);

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Privacy-preserving oracle fetch returned null");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            throw new InvalidOperationException("Privacy-preserving oracle fetch failed");
        }

        // Extract privacy-preserving oracle result
        var oracleResult = resultJson.GetProperty("result");

        return new PrivacyOracleResult
        {
            RequestId = oracleResult.GetProperty("requestId").GetString() ?? "",
            DataHash = oracleResult.GetProperty("dataHash").GetString() ?? "",
            SourceProof = ExtractSourceProof(oracleResult.GetProperty("sourceProof")),
            Success = oracleResult.GetProperty("valid").GetBoolean(),
            VerificationData = ExtractVerificationData(oracleResult)
        };
    }

    /// <summary>
    /// Validates data source reputation using privacy-preserving operations.
    /// </summary>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="historicalData">Historical reliability data.</param>
    /// <returns>True if the data source is reputable.</returns>
    private async Task<bool> ValidateDataSourceReputationAsync(
        string dataSource, Dictionary<string, object>? historicalData = null)
    {
        if (_enclaveManager == null)
        {
            // Basic validation if enclave not available
            return IsValidDataSource(dataSource);
        }

        var oracleData = new
        {
            dataSource = new
            {
                url = dataSource,
                protocol = new Uri(dataSource).Scheme,
                host = new Uri(dataSource).Host
            }
        };

        var requestInfo = new
        {
            feedId = "reputation_check",
            parameters = historicalData ?? new Dictionary<string, object>()
        };

        var operation = "validate";

        var jsParams = new
        {
            operation,
            oracleData,
            requestInfo
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.OracleOperations,
            paramsJson);

        if (string.IsNullOrEmpty(result))
            return false;

        try
        {
            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);
            return resultJson.TryGetProperty("success", out var success) && success.GetBoolean();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Processes batch oracle requests with privacy preservation.
    /// </summary>
    /// <param name="requests">The oracle requests.</param>
    /// <returns>Privacy-preserving batch results.</returns>
    private async Task<PrivacyBatchOracleResult> ProcessBatchRequestsWithPrivacyAsync(
        IEnumerable<OracleRequest> requests)
    {
        if (_enclaveManager == null)
        {
            // Fallback if enclave not available
            var results = new List<InternalBatchOracleResult>();
            foreach (var request in requests)
            {
                results.Add(new InternalBatchOracleResult
                {
                    RequestId = request.RequestId,
                    DataHash = HashData(request.Url + request.Path),
                    Success = true
                });
            }

            return new PrivacyBatchOracleResult
            {
                BatchId = Guid.NewGuid().ToString(),
                Results = results,
                AggregateProof = GenerateAggregateProof(results),
                Success = true
            };
        }

        var batchData = requests.Select(r => new
        {
            dataSource = new
            {
                url = r.Url,
                protocol = new Uri(r.Url).Scheme,
                host = new Uri(r.Url).Host,
                path = r.Path
            },
            verification = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                nonce = Guid.NewGuid().ToString(),
                requestId = r.RequestId
            }
        }).ToArray();

        var operation = "batch";

        var jsParams = new
        {
            operation,
            oracleData = new { batch = batchData },
            requestInfo = new { feedId = "batch", parameters = new Dictionary<string, object>() }
        };

        string paramsJson = JsonSerializer.Serialize(jsParams);

        string result = await _enclaveManager.ExecuteJavaScriptAsync(
            PrivacyComputingJavaScriptTemplates.OracleOperations,
            paramsJson);

        if (string.IsNullOrEmpty(result))
            throw new InvalidOperationException("Privacy-preserving batch oracle processing returned null");

        var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

        if (!resultJson.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            throw new InvalidOperationException("Privacy-preserving batch oracle processing failed");
        }

        var batchResult = resultJson.GetProperty("result");

        return new PrivacyBatchOracleResult
        {
            BatchId = batchResult.GetProperty("batchId").GetString() ?? "",
            Results = ExtractBatchResults(batchResult.GetProperty("results")),
            AggregateProof = ExtractAggregateProof(batchResult.GetProperty("aggregateProof")),
            Success = true
        };
    }

    /// <summary>
    /// Hashes data for privacy.
    /// </summary>
    private string HashData(string data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash.Take(16).ToArray());
    }

    /// <summary>
    /// Generates a simple source proof.
    /// </summary>
    private SourceProof GenerateSimpleSourceProof(string dataSource, string dataPath)
    {
        return new SourceProof
        {
            SourceHash = HashData(dataSource),
            PathHash = HashData(dataPath),
            Timestamp = DateTimeOffset.UtcNow,
            Signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{dataSource}-{dataPath}").Take(32).ToArray())
        };
    }

    /// <summary>
    /// Extracts source proof from JSON.
    /// </summary>
    private SourceProof ExtractSourceProof(JsonElement proofElement)
    {
        return new SourceProof
        {
            SourceHash = proofElement.GetProperty("sourceHash").GetString() ?? "",
            PathHash = proofElement.GetProperty("pathHash").GetString() ?? "",
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(proofElement.GetProperty("timestamp").GetInt64()),
            Signature = proofElement.GetProperty("signature").GetString() ?? ""
        };
    }

    /// <summary>
    /// Extracts verification data from JSON.
    /// </summary>
    private Dictionary<string, object> ExtractVerificationData(JsonElement resultElement)
    {
        var verificationData = new Dictionary<string, object>();

        if (resultElement.TryGetProperty("verification", out var verification))
        {
            verificationData["timestamp"] = verification.GetProperty("timestamp").GetInt64();
            verificationData["nonce"] = verification.GetProperty("nonce").GetString() ?? "";
            verificationData["blockHeight"] = verification.GetProperty("blockHeight").GetInt64();
        }

        return verificationData;
    }

    /// <summary>
    /// Extracts batch results from JSON.
    /// </summary>
    private List<InternalBatchOracleResult> ExtractBatchResults(JsonElement resultsElement)
    {
        var results = new List<InternalBatchOracleResult>();

        if (resultsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var resultElement in resultsElement.EnumerateArray())
            {
                results.Add(new InternalBatchOracleResult
                {
                    RequestId = resultElement.GetProperty("requestId").GetString() ?? "",
                    DataHash = resultElement.GetProperty("dataHash").GetString() ?? "",
                    Success = resultElement.GetProperty("success").GetBoolean()
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Generates aggregate proof for batch results.
    /// </summary>
    private AggregateProof GenerateAggregateProof(List<InternalBatchOracleResult> results)
    {
        var hashes = results.Select(r => r.DataHash).ToArray();
        var aggregateHash = HashData(string.Join("-", hashes));

        return new AggregateProof
        {
            BatchId = Guid.NewGuid().ToString(),
            AggregateHash = aggregateHash,
            ResultCount = results.Count,
            Timestamp = DateTimeOffset.UtcNow,
            Proof = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(aggregateHash).Take(32).ToArray())
        };
    }

    /// <summary>
    /// Extracts aggregate proof from JSON.
    /// </summary>
    private AggregateProof ExtractAggregateProof(JsonElement proofElement)
    {
        return new AggregateProof
        {
            BatchId = proofElement.GetProperty("batchId").GetString() ?? "",
            AggregateHash = proofElement.GetProperty("aggregateHash").GetString() ?? "",
            ResultCount = proofElement.GetProperty("resultCount").GetInt32(),
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(proofElement.GetProperty("timestamp").GetInt64()),
            Proof = proofElement.GetProperty("proof").GetString() ?? ""
        };
    }

    /// <summary>
    /// Privacy-preserving oracle result.
    /// </summary>
    private class PrivacyOracleResult
    {
        public string RequestId { get; set; } = "";
        public string DataHash { get; set; } = "";
        public SourceProof SourceProof { get; set; } = new();
        public bool Success { get; set; }
        public Dictionary<string, object> VerificationData { get; set; } = new();
    }

    /// <summary>
    /// Source proof.
    /// </summary>
    private class SourceProof
    {
        public string SourceHash { get; set; } = "";
        public string PathHash { get; set; } = "";
        public DateTimeOffset Timestamp { get; set; }
        public string Signature { get; set; } = "";
    }

    /// <summary>
    /// Privacy-preserving batch oracle result.
    /// </summary>
    private class PrivacyBatchOracleResult
    {
        public string BatchId { get; set; } = "";
        public List<InternalBatchOracleResult> Results { get; set; } = new();
        public AggregateProof AggregateProof { get; set; } = new();
        public bool Success { get; set; }
    }

    /// <summary>
    /// Internal batch oracle result.
    /// </summary>
    private class InternalBatchOracleResult
    {
        public string RequestId { get; set; } = "";
        public string DataHash { get; set; } = "";
        public bool Success { get; set; }
    }

    /// <summary>
    /// Aggregate proof for batch operations.
    /// </summary>
    private class AggregateProof
    {
        public string BatchId { get; set; } = "";
        public string AggregateHash { get; set; } = "";
        public int ResultCount { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Proof { get; set; } = "";
    }
}
