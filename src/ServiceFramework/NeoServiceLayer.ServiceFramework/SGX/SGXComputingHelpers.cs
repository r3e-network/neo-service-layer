using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework.SGX;

/// <summary>
/// Helper utilities for SGX computing operations.
/// Provides common patterns and utilities for services using SGX.
/// </summary>
public static class SGXComputingHelpers
{
    #region Context Creation Helpers

    /// <summary>
    /// Creates a standard SGX execution context for data processing.
    /// </summary>
    /// <param name="operation">The operation type (encrypt, aggregate, validate, etc.).</param>
    /// <param name="data">The data to process.</param>
    /// <param name="options">Additional options.</param>
    /// <returns>Configured SGX execution context.</returns>
    public static SGXExecutionContext CreateDataProcessingContext(
        string operation, 
        object data, 
        Dictionary<string, object>? options = null)
    {
        var parameters = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["data"] = data,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (options != null)
        {
            foreach (var kvp in options)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }

        return new SGXExecutionContext
        {
            JavaScriptCode = SGXComputingTemplates.GetTemplate("secure_validation"),
            Parameters = parameters,
            TimeoutMs = 30000,
            RequiredPermissions = new List<string> { $"sgx:data:{operation}" }
        };
    }

    /// <summary>
    /// Creates an SGX context for cryptographic operations.
    /// </summary>
    /// <param name="operation">Crypto operation (encrypt, decrypt, hash, sign).</param>
    /// <param name="data">Data to process.</param>
    /// <param name="algorithm">Cryptographic algorithm.</param>
    /// <param name="keySize">Key size in bits.</param>
    /// <returns>Configured SGX execution context.</returns>
    public static SGXExecutionContext CreateCryptoContext(
        string operation, 
        byte[] data, 
        string algorithm = "AES-256-GCM", 
        int keySize = 256)
    {
        return new SGXExecutionContext
        {
            JavaScriptCode = SGXComputingTemplates.GetTemplate("secure_encryption"),
            Parameters = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["data"] = Convert.ToBase64String(data),
                ["algorithm"] = algorithm,
                ["keySize"] = keySize,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            },
            TimeoutMs = 60000, // Crypto operations may take longer
            RequiredPermissions = new List<string> { $"sgx:crypto:{operation}" }
        };
    }

    /// <summary>
    /// Creates an SGX context for voting operations.
    /// </summary>
    /// <param name="votes">List of votes.</param>
    /// <param name="candidates">List of candidates.</param>
    /// <param name="method">Voting method.</param>
    /// <param name="anonymize">Whether to anonymize votes.</param>
    /// <returns>Configured SGX execution context.</returns>
    public static SGXExecutionContext CreateVotingContext(
        List<object> votes, 
        List<string> candidates, 
        string method = "simple_majority", 
        bool anonymize = true)
    {
        return new SGXExecutionContext
        {
            JavaScriptCode = SGXComputingTemplates.GetTemplate("secure_voting"),
            Parameters = new Dictionary<string, object>
            {
                ["votes"] = votes,
                ["candidates"] = candidates,
                ["votingMethod"] = method,
                ["anonymize"] = anonymize,
                ["auditTrail"] = true,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            },
            TimeoutMs = 120000, // Voting operations may process many votes
            RequiredPermissions = new List<string> { "sgx:voting:execute", $"sgx:voting:{method}" }
        };
    }

    #endregion

    #region Storage Helpers

    /// <summary>
    /// Creates a standard SGX storage context with compression and metadata.
    /// </summary>
    /// <param name="key">Storage key.</param>
    /// <param name="data">Data to store.</param>
    /// <param name="contentType">Content type.</param>
    /// <param name="enableCompression">Whether to enable compression.</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>Configured SGX storage context.</returns>
    public static SGXStorageContext CreateSecureStorageContext(
        string key,
        object data,
        string contentType = "application/json",
        bool enableCompression = true,
        Dictionary<string, object>? metadata = null)
    {
        var dataBytes = data switch
        {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => JsonSerializer.SerializeToUtf8Bytes(data)
        };

        var storageMetadata = new Dictionary<string, object>
        {
            ["createdAt"] = DateTime.UtcNow.ToString("O"),
            ["dataType"] = data.GetType().Name,
            ["originalSize"] = dataBytes.Length
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                storageMetadata[kvp.Key] = kvp.Value;
            }
        }

        return new SGXStorageContext
        {
            Key = key,
            Data = dataBytes,
            ContentType = contentType,
            Compression = enableCompression ? SGXCompressionType.GZip : SGXCompressionType.None,
            Metadata = storageMetadata,
            Policy = new SGXStoragePolicy
            {
                SealingType = SGXSealingPolicyType.MrSigner,
                ExpiresAt = DateTime.UtcNow.AddYears(1), // Default 1 year expiration
                AllowSharing = false
            },
            Tags = new List<string> { "sgx-secured", "auto-generated" }
        };
    }

    /// <summary>
    /// Creates a temporary storage context for session-based data.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="key">Storage key within session.</param>
    /// <param name="data">Data to store.</param>
    /// <param name="expirationMinutes">Expiration in minutes.</param>
    /// <returns>Configured SGX storage context for temporary data.</returns>
    public static SGXStorageContext CreateTempStorageContext(
        string sessionId,
        string key,
        object data,
        int expirationMinutes = 60)
    {
        var fullKey = $"temp:{sessionId}:{key}";
        
        return CreateSecureStorageContext(fullKey, data, "application/json", true, new Dictionary<string, object>
        {
            ["sessionId"] = sessionId,
            ["temporary"] = true,
            ["expirationMinutes"] = expirationMinutes
        })
        {
            Policy = new SGXStoragePolicy
            {
                SealingType = SGXSealingPolicyType.MrEnclave,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                AllowSharing = false
            },
            Tags = new List<string> { "temporary", "session", sessionId }
        };
    }

    #endregion

    #region Computation Helpers

    /// <summary>
    /// Creates an SGX computation context for privacy-preserving analytics.
    /// </summary>
    /// <param name="inputKeys">Input data storage keys.</param>
    /// <param name="outputKey">Output storage key.</param>
    /// <param name="analyticsType">Type of analytics (aggregate, correlation, etc.).</param>
    /// <param name="parameters">Computation parameters.</param>
    /// <returns>Configured SGX computation context.</returns>
    public static SGXComputationContext CreateAnalyticsContext(
        List<string> inputKeys,
        string outputKey,
        string analyticsType,
        Dictionary<string, object>? parameters = null)
    {
        var computationCode = analyticsType.ToLowerInvariant() switch
        {
            "aggregate" or "aggregation" => SGXComputingTemplates.GetTemplate("secure_aggregation"),
            "voting" => SGXComputingTemplates.GetTemplate("secure_voting"),
            _ => SGXComputingTemplates.GetTemplate("secure_aggregation") // Default to aggregation
        };

        var computationParams = new Dictionary<string, object>
        {
            ["analyticsType"] = analyticsType,
            ["inputCount"] = inputKeys.Count,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                computationParams[kvp.Key] = kvp.Value;
            }
        }

        return new SGXComputationContext
        {
            ComputationCode = computationCode,
            InputKeys = inputKeys,
            OutputKeys = new List<string> { outputKey },
            Parameters = computationParams,
            PrivacyLevel = SGXPrivacyLevel.High,
            MaxComputationTimeMs = 300000, // 5 minutes
            PreserveIntermediateResults = false
        };
    }

    /// <summary>
    /// Creates an SGX computation context for multi-party computation.
    /// </summary>
    /// <param name="parties">List of party identifiers.</param>
    /// <param name="computation">Computation to perform.</param>
    /// <param name="threshold">Minimum parties required.</param>
    /// <param name="outputKey">Output storage key.</param>
    /// <returns>Configured SGX computation context for MPC.</returns>
    public static SGXComputationContext CreateMPCContext(
        List<string> parties,
        string computation,
        int threshold,
        string outputKey)
    {
        return new SGXComputationContext
        {
            ComputationCode = SGXComputingTemplates.GetTemplate("multi_party_computation"),
            InputKeys = parties.Select(party => $"mpc:share:{party}").ToList(),
            OutputKeys = new List<string> { outputKey },
            Parameters = new Dictionary<string, object>
            {
                ["parties"] = parties,
                ["computation"] = computation,
                ["threshold"] = threshold,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            },
            PrivacyLevel = SGXPrivacyLevel.Maximum,
            MaxComputationTimeMs = 600000, // 10 minutes for MPC
            PreserveIntermediateResults = false
        };
    }

    #endregion

    #region Batch Operation Helpers

    /// <summary>
    /// Creates a batch context for multiple secure computations.
    /// </summary>
    /// <param name="operations">List of computation operations.</param>
    /// <param name="isAtomic">Whether batch should be atomic.</param>
    /// <param name="maxExecutionTimeMs">Maximum execution time.</param>
    /// <returns>Configured SGX batch context.</returns>
    public static SGXBatchContext CreateComputationBatch(
        List<(string jsCode, Dictionary<string, object> parameters)> operations,
        bool isAtomic = true,
        int maxExecutionTimeMs = 300000)
    {
        var batchOperations = operations.Select(op => new SGXBatchOperation
        {
            OperationType = SGXOperationType.Computation,
            JavaScriptCode = op.jsCode,
            Parameters = op.parameters
        }).ToList();

        return new SGXBatchContext
        {
            Operations = batchOperations,
            IsAtomic = isAtomic,
            MaxExecutionTimeMs = maxExecutionTimeMs,
            Metadata = new Dictionary<string, object>
            {
                ["batchId"] = Guid.NewGuid().ToString(),
                ["createdAt"] = DateTime.UtcNow.ToString("O"),
                ["operationCount"] = operations.Count
            }
        };
    }

    /// <summary>
    /// Creates a batch context for multiple storage operations.
    /// </summary>
    /// <param name="storageOperations">List of storage contexts.</param>
    /// <param name="isAtomic">Whether batch should be atomic.</param>
    /// <returns>Configured SGX batch context for storage.</returns>
    public static SGXBatchContext CreateStorageBatch(
        List<SGXStorageContext> storageOperations,
        bool isAtomic = true)
    {
        var batchOperations = storageOperations.Select(storage => new SGXBatchOperation
        {
            OperationType = SGXOperationType.Storage,
            StorageContext = storage
        }).ToList();

        return new SGXBatchContext
        {
            Operations = batchOperations,
            IsAtomic = isAtomic,
            MaxExecutionTimeMs = 600000, // 10 minutes for storage operations
            Metadata = new Dictionary<string, object>
            {
                ["batchId"] = Guid.NewGuid().ToString(),
                ["createdAt"] = DateTime.UtcNow.ToString("O"),
                ["storageCount"] = storageOperations.Count,
                ["totalSize"] = storageOperations.Sum(s => s.Data.Length)
            }
        };
    }

    #endregion

    #region Session Helpers

    /// <summary>
    /// Creates a session context for related SGX operations.
    /// </summary>
    /// <param name="sessionName">Name of the session.</param>
    /// <param name="allowedOperations">Operations allowed in this session.</param>
    /// <param name="accessibleKeys">Keys accessible in this session.</param>
    /// <param name="timeoutMinutes">Session timeout in minutes.</param>
    /// <returns>Configured SGX session context.</returns>
    public static SGXSessionContext CreateOperationSession(
        string sessionName,
        List<SGXOperationType> allowedOperations,
        List<string> accessibleKeys,
        int timeoutMinutes = 60)
    {
        return new SGXSessionContext
        {
            SessionName = sessionName,
            TimeoutMinutes = timeoutMinutes,
            AllowedOperations = allowedOperations,
            AccessibleKeys = accessibleKeys,
            Configuration = new Dictionary<string, object>
            {
                ["createdAt"] = DateTime.UtcNow.ToString("O"),
                ["createdBy"] = "SGXComputingHelpers",
                ["maxOperations"] = 100, // Limit operations per session
                ["logOperations"] = true
            }
        };
    }

    /// <summary>
    /// Creates a secure session for privacy-preserving computations.
    /// </summary>
    /// <param name="sessionName">Name of the session.</param>
    /// <param name="privacyLevel">Required privacy level.</param>
    /// <param name="timeoutMinutes">Session timeout in minutes.</param>
    /// <returns>Configured SGX session context for privacy operations.</returns>
    public static SGXSessionContext CreatePrivacySession(
        string sessionName,
        SGXPrivacyLevel privacyLevel,
        int timeoutMinutes = 30)
    {
        var allowedOperations = privacyLevel switch
        {
            SGXPrivacyLevel.Maximum => new List<SGXOperationType> { SGXOperationType.Computation },
            SGXPrivacyLevel.High => new List<SGXOperationType> { SGXOperationType.Computation, SGXOperationType.Storage },
            _ => new List<SGXOperationType> { SGXOperationType.Computation, SGXOperationType.Storage, SGXOperationType.Retrieval }
        };

        return new SGXSessionContext
        {
            SessionName = $"privacy-{sessionName}",
            TimeoutMinutes = timeoutMinutes,
            AllowedOperations = allowedOperations,
            AccessibleKeys = new List<string>(), // Will be populated as needed
            Configuration = new Dictionary<string, object>
            {
                ["privacyLevel"] = privacyLevel.ToString(),
                ["restrictedAccess"] = privacyLevel >= SGXPrivacyLevel.High,
                ["auditAll"] = true,
                ["createdAt"] = DateTime.UtcNow.ToString("O")
            }
        };
    }

    #endregion

    #region Validation and Utility Helpers

    /// <summary>
    /// Validates parameters for SGX execution context.
    /// </summary>
    /// <param name="context">The execution context to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    public static (bool IsValid, List<string> Errors) ValidateExecutionContext(SGXExecutionContext context)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(context.JavaScriptCode))
        {
            errors.Add("JavaScript code is required");
        }

        if (context.JavaScriptCode?.Length > 100000) // 100KB limit
        {
            errors.Add("JavaScript code exceeds maximum size limit");
        }

        if (context.TimeoutMs <= 0 || context.TimeoutMs > 3600000) // Max 1 hour
        {
            errors.Add("Timeout must be between 1ms and 1 hour");
        }

        if (context.Parameters?.Count > 100)
        {
            errors.Add("Too many parameters (maximum 100)");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Sanitizes storage key to ensure it's safe for SGX operations.
    /// </summary>
    /// <param name="key">The storage key to sanitize.</param>
    /// <returns>Sanitized storage key.</returns>
    public static string SanitizeStorageKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Storage key cannot be null or empty");
        }

        // Remove invalid characters and limit length
        var sanitized = new string(key
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ':' || c == '.')
            .Take(200)
            .ToArray());

        if (string.IsNullOrEmpty(sanitized))
        {
            throw new ArgumentException("Storage key contains no valid characters");
        }

        return sanitized;
    }

    /// <summary>
    /// Estimates computation complexity based on JavaScript code.
    /// </summary>
    /// <param name="jsCode">JavaScript code to analyze.</param>
    /// <returns>Estimated complexity score (1-10).</returns>
    public static int EstimateComplexity(string jsCode)
    {
        if (string.IsNullOrEmpty(jsCode))
        {
            return 1;
        }

        var complexity = 1;

        // Count loops and conditions
        complexity += CountOccurrences(jsCode, "for") * 2;
        complexity += CountOccurrences(jsCode, "while") * 2;
        complexity += CountOccurrences(jsCode, "if") * 1;
        complexity += CountOccurrences(jsCode, "function") * 1;

        // Count crypto operations
        complexity += CountOccurrences(jsCode, "crypto.") * 3;
        complexity += CountOccurrences(jsCode, "encrypt") * 2;
        complexity += CountOccurrences(jsCode, "hash") * 1;

        // Count data operations
        complexity += CountOccurrences(jsCode, "JSON.parse") * 1;
        complexity += CountOccurrences(jsCode, "JSON.stringify") * 1;

        return Math.Min(complexity, 10); // Cap at 10
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Creates a standardized error result for SGX operations.
    /// </summary>
    /// <typeparam name="T">Type of SGX result.</typeparam>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="operation">Operation that failed.</param>
    /// <returns>Error result of type T.</returns>
    public static T CreateErrorResult<T>(string errorMessage, string? operation = null) where T : class, new()
    {
        var result = new T();
        
        // Use reflection to set common properties
        var successProp = typeof(T).GetProperty("Success");
        successProp?.SetValue(result, false);
        
        var errorProp = typeof(T).GetProperty("ErrorMessage");
        errorProp?.SetValue(result, errorMessage);
        
        var timestampProp = typeof(T).GetProperty("Timestamp");
        timestampProp?.SetValue(result, DateTime.UtcNow);

        return result;
    }

    #endregion
}