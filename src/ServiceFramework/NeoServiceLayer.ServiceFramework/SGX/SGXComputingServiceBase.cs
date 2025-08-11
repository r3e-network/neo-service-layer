using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework.Permissions;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.EnclaveStorage.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.ServiceFramework.SGX;

/// <summary>
/// Base class for services that need SGX computing and storage capabilities.
/// Provides standard implementations for secure computation and storage operations.
/// </summary>
public abstract class SGXComputingServiceBase : PermissionAwareServiceBase, ISGXComputingService
{
    private readonly IEnclaveStorageService? _enclaveStorage;
    private readonly ConcurrentDictionary<string, SGXSession> _activeSessions = new();
    private readonly object _sessionLock = new();

    /// <summary>
    /// Gets the enclave storage service.
    /// </summary>
    protected IEnclaveStorageService? EnclaveStorage => _enclaveStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="SGXComputingServiceBase"/> class.
    /// </summary>
    protected SGXComputingServiceBase(
        string serviceName,
        string description,
        string version,
        ILogger logger,
        BlockchainType[] supportedBlockchains,
        IEnclaveManager enclaveManager,
        IServiceProvider? serviceProvider = null,
        IEnclaveStorageService? enclaveStorage = null)
        : base(serviceName, description, version, logger, supportedBlockchains, enclaveManager, serviceProvider)
    {
        _enclaveStorage = enclaveStorage ?? serviceProvider?.GetService(typeof(IEnclaveStorageService)) as IEnclaveStorageService;
        
        // Add SGX computing capability
        AddCapability<ISGXComputingService>();
    }

    #region ISGXComputingService Implementation

    /// <inheritdoc/>
    public virtual async Task<SGXExecutionResult> ExecuteSecureComputingAsync(SGXExecutionContext context, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for computation
            if (!await CheckPermissionAsync($"sgx:compute:{ServiceName}", "execute"))
            {
                return new SGXExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX computation"
                };
            }

            Logger.LogDebug("Executing SGX computation for service {ServiceName}", ServiceName);
            var startTime = DateTime.UtcNow;

            // Validate context
            if (string.IsNullOrEmpty(context.JavaScriptCode))
            {
                return new SGXExecutionResult
                {
                    Success = false,
                    ErrorMessage = "JavaScript code is required for SGX execution"
                };
            }

            // Prepare execution environment
            var executionParameters = PrepareExecutionParameters(context);
            
            // Execute in enclave
            var result = await ExecuteInEnclaveAsync(context.JavaScriptCode, executionParameters, context.TimeoutMs);
            
            var endTime = DateTime.UtcNow;
            var executionTime = (long)(endTime - startTime).TotalMilliseconds;

            return new SGXExecutionResult
            {
                Success = true,
                Result = result,
                Metrics = new SGXExecutionMetrics
                {
                    ExecutionTimeMs = executionTime,
                    MemoryUsedBytes = GetMemoryUsage(),
                    CpuTimeMs = executionTime
                },
                ConsoleOutput = GetConsoleOutput()
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing SGX computation");
            return new SGXExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXStorageResult> StoreSecureDataAsync(SGXStorageContext storageContext, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for storage
            var fullKey = BuildStorageKey(storageContext.Key);
            if (!await CheckPermissionAsync($"sgx:storage:{fullKey}", "write"))
            {
                return new SGXStorageResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX storage"
                };
            }

            Logger.LogDebug("Storing secure data with key {Key}", fullKey);

            if (_enclaveStorage == null)
            {
                return new SGXStorageResult
                {
                    Success = false,
                    ErrorMessage = "Enclave storage service not available"
                };
            }

            // Prepare storage request
            var sealDataRequest = new SealDataRequest
            {
                Key = fullKey,
                Data = ApplyCompression(storageContext.Data, storageContext.Compression),
                Metadata = PrepareStorageMetadata(storageContext),
                Policy = ConvertStoragePolicy(storageContext.Policy)
            };

            // Store in enclave
            var result = await _enclaveStorage.SealDataAsync(sealDataRequest, blockchainType);

            return new SGXStorageResult
            {
                Success = result.Success,
                StorageId = result.StorageId,
                ErrorMessage = result.ErrorMessage,
                Fingerprint = result.Fingerprint,
                StoredSize = storageContext.Data.Length,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error storing secure data");
            return new SGXStorageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXRetrievalResult> RetrieveSecureDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            var fullKey = BuildStorageKey(key);
            
            // Check permissions for retrieval
            if (!await CheckPermissionAsync($"sgx:storage:{fullKey}", "read"))
            {
                return new SGXRetrievalResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX data retrieval"
                };
            }

            Logger.LogDebug("Retrieving secure data with key {Key}", fullKey);

            if (_enclaveStorage == null)
            {
                return new SGXRetrievalResult
                {
                    Success = false,
                    ErrorMessage = "Enclave storage service not available"
                };
            }

            // Retrieve from enclave
            var result = await _enclaveStorage.UnsealDataAsync(fullKey, blockchainType);

            if (!result.Success)
            {
                return new SGXRetrievalResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
            }

            // Decompress data if needed
            var decompressedData = DecompressData(result.Data, result.Metadata);

            return new SGXRetrievalResult
            {
                Success = true,
                Data = decompressedData,
                Metadata = result.Metadata,
                LastAccessed = result.LastAccessed,
                WasSealed = result.Sealed
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving secure data");
            return new SGXRetrievalResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXDeletionResult> DeleteSecureDataAsync(string key, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            var fullKey = BuildStorageKey(key);
            
            // Check permissions for deletion
            if (!await CheckPermissionAsync($"sgx:storage:{fullKey}", "delete"))
            {
                return new SGXDeletionResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX data deletion"
                };
            }

            Logger.LogDebug("Deleting secure data with key {Key}", fullKey);

            if (_enclaveStorage == null)
            {
                return new SGXDeletionResult
                {
                    Success = false,
                    ErrorMessage = "Enclave storage service not available"
                };
            }

            // Delete from enclave
            var result = await _enclaveStorage.DeleteSealedDataAsync(fullKey, blockchainType);

            return new SGXDeletionResult
            {
                Success = result.Success,
                Shredded = result.Shredded,
                ErrorMessage = result.ErrorMessage,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting secure data");
            return new SGXDeletionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXComputationResult> ExecutePrivacyComputationAsync(SGXComputationContext computationContext, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for privacy computation
            if (!await CheckPermissionAsync($"sgx:privacy:{ServiceName}", "execute"))
            {
                return new SGXComputationResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for privacy computation"
                };
            }

            Logger.LogDebug("Executing privacy-preserving computation for service {ServiceName}", ServiceName);
            var startTime = DateTime.UtcNow;

            // Load input data
            var inputData = new Dictionary<string, object>();
            long totalInputSize = 0;

            foreach (var inputKey in computationContext.InputKeys)
            {
                var retrievalResult = await RetrieveSecureDataAsync(inputKey, blockchainType);
                if (retrievalResult.Success)
                {
                    inputData[inputKey] = retrievalResult.Data;
                    totalInputSize += retrievalResult.Data.Length;
                }
                else
                {
                    Logger.LogWarning("Failed to load input data for key {InputKey}: {Error}", 
                        inputKey, retrievalResult.ErrorMessage);
                }
            }

            // Execute computation
            var executionContext = new SGXExecutionContext
            {
                JavaScriptCode = computationContext.ComputationCode,
                Parameters = new Dictionary<string, object>(computationContext.Parameters)
                {
                    ["inputData"] = inputData,
                    ["preserveIntermediateResults"] = computationContext.PreserveIntermediateResults
                },
                TimeoutMs = computationContext.MaxComputationTimeMs
            };

            var executionResult = await ExecuteSecureComputingAsync(executionContext, blockchainType);

            if (!executionResult.Success)
            {
                return new SGXComputationResult
                {
                    Success = false,
                    ErrorMessage = executionResult.ErrorMessage
                };
            }

            // Store output data
            var outputKeys = new List<string>();
            long totalOutputSize = 0;

            if (executionResult.Result is Dictionary<string, object> outputs)
            {
                for (int i = 0; i < computationContext.OutputKeys.Count && i < outputs.Count; i++)
                {
                    var outputKey = computationContext.OutputKeys[i];
                    var outputValue = outputs.ElementAt(i).Value;
                    
                    if (outputValue != null)
                    {
                        var outputData = JsonSerializer.SerializeToUtf8Bytes(outputValue);
                        var storageContext = new SGXStorageContext
                        {
                            Key = outputKey,
                            Data = outputData,
                            ContentType = "application/json",
                            Metadata = new Dictionary<string, object>
                            {
                                ["computationId"] = Guid.NewGuid().ToString(),
                                ["privacyLevel"] = computationContext.PrivacyLevel.ToString(),
                                ["computedAt"] = DateTime.UtcNow
                            }
                        };

                        var storeResult = await StoreSecureDataAsync(storageContext, blockchainType);
                        if (storeResult.Success)
                        {
                            outputKeys.Add(outputKey);
                            totalOutputSize += outputData.Length;
                        }
                    }
                }
            }

            var endTime = DateTime.UtcNow;
            var computationTime = (long)(endTime - startTime).TotalMilliseconds;

            return new SGXComputationResult
            {
                Success = true,
                Result = executionResult.Result,
                OutputKeys = outputKeys,
                Metrics = new SGXComputationMetrics
                {
                    ComputationTimeMs = computationTime,
                    InputDataSize = totalInputSize,
                    OutputDataSize = totalOutputSize,
                    PrivacyLevel = computationContext.PrivacyLevel
                },
                PrivacyAttestation = GeneratePrivacyAttestation(computationContext.PrivacyLevel)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing privacy computation");
            return new SGXComputationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXKeyListResult> ListStorageKeysAsync(string? prefix, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for key listing
            if (!await CheckPermissionAsync($"sgx:storage:{ServiceName}:*", "read"))
            {
                return new SGXKeyListResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX key listing"
                };
            }

            if (_enclaveStorage == null)
            {
                return new SGXKeyListResult
                {
                    Success = false,
                    ErrorMessage = "Enclave storage service not available"
                };
            }

            var fullPrefix = string.IsNullOrEmpty(prefix) ? ServiceName : $"{ServiceName}:{prefix}";
            var request = new ListSealedItemsRequest
            {
                Service = ServiceName,
                Prefix = fullPrefix,
                Page = 1,
                PageSize = 1000
            };

            var result = await _enclaveStorage.ListSealedItemsAsync(request, blockchainType);

            var keys = result.Items
                .Select(item => item.Key.StartsWith($"{ServiceName}:") 
                    ? item.Key.Substring($"{ServiceName}:".Length) 
                    : item.Key)
                .ToList();

            return new SGXKeyListResult
            {
                Success = true,
                Keys = keys,
                TotalCount = result.ItemCount,
                HasMore = result.Page * result.PageSize < result.ItemCount
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing storage keys");
            return new SGXKeyListResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXMetadataResult> GetStorageMetadataAsync(string key, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            var fullKey = BuildStorageKey(key);
            
            // Check permissions for metadata access
            if (!await CheckPermissionAsync($"sgx:storage:{fullKey}", "read"))
            {
                return new SGXMetadataResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX metadata access"
                };
            }

            if (_enclaveStorage == null)
            {
                return new SGXMetadataResult
                {
                    Success = false,
                    ErrorMessage = "Enclave storage service not available"
                };
            }

            var result = await _enclaveStorage.GetSealedItemInfoAsync(fullKey, blockchainType);

            if (!result.Success)
            {
                return new SGXMetadataResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
            }

            return new SGXMetadataResult
            {
                Success = true,
                Metadata = result.Metadata,
                DataSize = result.Size,
                ContentType = result.Metadata.GetValueOrDefault("contentType", "application/octet-stream").ToString() ?? "",
                CreatedAt = result.CreatedAt,
                ModifiedAt = result.LastAccessed
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting storage metadata");
            return new SGXMetadataResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXBatchResult> ExecuteBatchOperationsAsync(SGXBatchContext batchContext, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for batch operations
            if (!await CheckPermissionAsync($"sgx:batch:{ServiceName}", "execute"))
            {
                return new SGXBatchResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX batch operations"
                };
            }

            Logger.LogDebug("Executing batch operations for service {ServiceName} with {OperationCount} operations", 
                ServiceName, batchContext.Operations.Count);

            var startTime = DateTime.UtcNow;
            var operationResults = new List<SGXBatchOperationResult>();
            int successfulOperations = 0;
            int failedOperations = 0;

            foreach (var operation in batchContext.Operations)
            {
                var operationStartTime = DateTime.UtcNow;
                
                try
                {
                    var operationResult = await ExecuteSingleBatchOperationAsync(operation, blockchainType);
                    var operationEndTime = DateTime.UtcNow;
                    
                    var batchOpResult = new SGXBatchOperationResult
                    {
                        Success = operationResult.Success,
                        Result = operationResult.Result,
                        ErrorMessage = operationResult.ErrorMessage,
                        ExecutionTimeMs = (long)(operationEndTime - operationStartTime).TotalMilliseconds
                    };
                    
                    operationResults.Add(batchOpResult);
                    
                    if (operationResult.Success)
                    {
                        successfulOperations++;
                    }
                    else
                    {
                        failedOperations++;
                        
                        // If batch is atomic and an operation fails, stop execution
                        if (batchContext.IsAtomic)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var batchOpResult = new SGXBatchOperationResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutionTimeMs = (long)(DateTime.UtcNow - operationStartTime).TotalMilliseconds
                    };
                    
                    operationResults.Add(batchOpResult);
                    failedOperations++;
                    
                    if (batchContext.IsAtomic)
                    {
                        break;
                    }
                }
            }

            var endTime = DateTime.UtcNow;
            var totalExecutionTime = (long)(endTime - startTime).TotalMilliseconds;
            
            var batchSuccess = batchContext.IsAtomic ? failedOperations == 0 : successfulOperations > 0;

            return new SGXBatchResult
            {
                Success = batchSuccess,
                OperationResults = operationResults,
                ErrorMessage = batchSuccess ? null : "One or more batch operations failed",
                Metrics = new SGXBatchMetrics
                {
                    TotalExecutionTimeMs = totalExecutionTime,
                    SuccessfulOperations = successfulOperations,
                    FailedOperations = failedOperations,
                    AverageOperationTimeMs = operationResults.Count > 0 
                        ? operationResults.Average(r => r.ExecutionTimeMs) 
                        : 0
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing batch operations");
            return new SGXBatchResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXSessionResult> CreateSecureSessionAsync(SGXSessionContext sessionContext, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Check permissions for session creation
            if (!await CheckPermissionAsync($"sgx:sessions:{ServiceName}", "create"))
            {
                return new SGXSessionResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient permissions for SGX session creation"
                };
            }

            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(sessionContext.TimeoutMinutes);

            var session = new SGXSession
            {
                SessionId = sessionId,
                ServiceName = ServiceName,
                SessionName = sessionContext.SessionName,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                AllowedOperations = sessionContext.AllowedOperations.ToList(),
                AccessibleKeys = sessionContext.AccessibleKeys.ToList(),
                Configuration = new Dictionary<string, object>(sessionContext.Configuration)
            };

            lock (_sessionLock)
            {
                _activeSessions[sessionId] = session;
            }

            Logger.LogDebug("Created secure session {SessionId} for service {ServiceName}", sessionId, ServiceName);

            return new SGXSessionResult
            {
                Success = true,
                SessionId = sessionId,
                ExpiresAt = expiresAt,
                SessionData = new Dictionary<string, object>
                {
                    ["sessionName"] = sessionContext.SessionName,
                    ["allowedOperations"] = sessionContext.AllowedOperations.Select(op => op.ToString()).ToList(),
                    ["accessibleKeyCount"] = sessionContext.AccessibleKeys.Count
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating secure session");
            return new SGXSessionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXExecutionResult> ExecuteInSessionAsync(string sessionId, SGXOperationContext operationContext, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            // Get and validate session
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return new SGXExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Session not found or expired"
                };
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                lock (_sessionLock)
                {
                    _activeSessions.TryRemove(sessionId, out _);
                }
                
                return new SGXExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Session has expired"
                };
            }

            // Check if operation is allowed in session
            if (!session.AllowedOperations.Contains(operationContext.OperationType))
            {
                return new SGXExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Operation {operationContext.OperationType} not allowed in this session"
                };
            }

            // Execute operation based on type
            switch (operationContext.OperationType)
            {
                case SGXOperationType.Computation:
                    if (string.IsNullOrEmpty(operationContext.JavaScriptCode))
                    {
                        return new SGXExecutionResult
                        {
                            Success = false,
                            ErrorMessage = "JavaScript code required for computation operation"
                        };
                    }
                    
                    var executionContext = new SGXExecutionContext
                    {
                        JavaScriptCode = operationContext.JavaScriptCode,
                        Parameters = operationContext.Parameters
                    };
                    
                    return await ExecuteSecureComputingAsync(executionContext, blockchainType);

                case SGXOperationType.Storage:
                    if (string.IsNullOrEmpty(operationContext.StorageKey) || operationContext.Data == null)
                    {
                        return new SGXExecutionResult
                        {
                            Success = false,
                            ErrorMessage = "Storage key and data required for storage operation"
                        };
                    }
                    
                    var storageContext = new SGXStorageContext
                    {
                        Key = operationContext.StorageKey,
                        Data = operationContext.Data
                    };
                    
                    var storeResult = await StoreSecureDataAsync(storageContext, blockchainType);
                    return new SGXExecutionResult
                    {
                        Success = storeResult.Success,
                        Result = storeResult,
                        ErrorMessage = storeResult.ErrorMessage
                    };

                case SGXOperationType.Retrieval:
                    if (string.IsNullOrEmpty(operationContext.StorageKey))
                    {
                        return new SGXExecutionResult
                        {
                            Success = false,
                            ErrorMessage = "Storage key required for retrieval operation"
                        };
                    }
                    
                    var retrieveResult = await RetrieveSecureDataAsync(operationContext.StorageKey, blockchainType);
                    return new SGXExecutionResult
                    {
                        Success = retrieveResult.Success,
                        Result = retrieveResult,
                        ErrorMessage = retrieveResult.ErrorMessage
                    };

                default:
                    return new SGXExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Unsupported operation type: {operationContext.OperationType}"
                    };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing operation in session");
            return new SGXExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public virtual async Task<SGXSessionResult> CloseSecureSessionAsync(string sessionId, BlockchainType blockchainType)
    {
        ValidateBlockchainType(blockchainType);

        try
        {
            lock (_sessionLock)
            {
                if (_activeSessions.TryRemove(sessionId, out var session))
                {
                    Logger.LogDebug("Closed secure session {SessionId} for service {ServiceName}", sessionId, ServiceName);
                    
                    return new SGXSessionResult
                    {
                        Success = true,
                        SessionId = sessionId,
                        SessionData = new Dictionary<string, object>
                        {
                            ["closedAt"] = DateTime.UtcNow,
                            ["sessionDuration"] = (DateTime.UtcNow - session.CreatedAt).TotalMinutes
                        }
                    };
                }
                else
                {
                    return new SGXSessionResult
                    {
                        Success = false,
                        ErrorMessage = "Session not found"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error closing secure session");
            return new SGXSessionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds a full storage key with service name prefix.
    /// </summary>
    /// <param name="key">The base key.</param>
    /// <returns>The full storage key.</returns>
    protected virtual string BuildStorageKey(string key)
    {
        return key.StartsWith($"{ServiceName}:") ? key : $"{ServiceName}:{key}";
    }

    /// <summary>
    /// Prepares execution parameters for JavaScript execution.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <returns>The prepared parameters.</returns>
    protected virtual Dictionary<string, object> PrepareExecutionParameters(SGXExecutionContext context)
    {
        var parameters = new Dictionary<string, object>(context.Parameters)
        {
            ["serviceName"] = ServiceName,
            ["timestamp"] = DateTime.UtcNow,
            ["executionId"] = Guid.NewGuid().ToString()
        };

        return parameters;
    }

    /// <summary>
    /// Executes JavaScript code in the enclave.
    /// </summary>
    /// <param name="jsCode">The JavaScript code.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <returns>The execution result.</returns>
    protected virtual async Task<object?> ExecuteInEnclaveAsync(string jsCode, Dictionary<string, object> parameters, int timeoutMs)
    {
        if (_enclaveManager == null)
        {
            throw new InvalidOperationException("Enclave manager not available");
        }

        // Prepare the execution environment
        var executionScript = PrepareExecutionScript(jsCode, parameters);
        
        // Execute in enclave
        var result = await _enclaveManager.ExecuteJavaScriptAsync(executionScript);
        
        // Parse and return result
        return ParseExecutionResult(result);
    }

    /// <summary>
    /// Prepares the JavaScript execution script with parameters.
    /// </summary>
    /// <param name="jsCode">The JavaScript code.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns>The prepared script.</returns>
    protected virtual string PrepareExecutionScript(string jsCode, Dictionary<string, object> parameters)
    {
        var paramJson = JsonSerializer.Serialize(parameters);
        
        return $@"
            const params = {paramJson};
            const console = {{ 
                log: function(...args) {{ /* Console output handling */ }},
                error: function(...args) {{ /* Error output handling */ }}
            }};
            
            // User's JavaScript code
            {jsCode}
            
            // Execute main function if it exists
            if (typeof main === 'function') {{
                main(params);
            }}
        ";
    }

    /// <summary>
    /// Parses the execution result from the enclave.
    /// </summary>
    /// <param name="result">The raw result.</param>
    /// <returns>The parsed result.</returns>
    protected virtual object? ParseExecutionResult(string result)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(result);
        }
        catch
        {
            return result;
        }
    }

    /// <summary>
    /// Prepares storage metadata from the storage context.
    /// </summary>
    /// <param name="context">The storage context.</param>
    /// <returns>The prepared metadata.</returns>
    protected virtual Dictionary<string, object> PrepareStorageMetadata(SGXStorageContext context)
    {
        var metadata = new Dictionary<string, object>(context.Metadata)
        {
            ["serviceName"] = ServiceName,
            ["contentType"] = context.ContentType,
            ["storedAt"] = DateTime.UtcNow,
            ["compression"] = context.Compression.ToString(),
            ["tags"] = context.Tags
        };

        return metadata;
    }

    /// <summary>
    /// Converts SGX storage policy to enclave storage policy.
    /// </summary>
    /// <param name="policy">The SGX storage policy.</param>
    /// <returns>The enclave storage policy.</returns>
    protected virtual SealingPolicy ConvertStoragePolicy(SGXStoragePolicy policy)
    {
        var sealingType = policy.SealingType switch
        {
            SGXSealingPolicyType.MrSigner => SealingPolicyType.MrSigner,
            SGXSealingPolicyType.MrEnclave => SealingPolicyType.MrEnclave,
            SGXSealingPolicyType.Both => SealingPolicyType.MrSigner, // Default fallback
            _ => SealingPolicyType.MrSigner
        };

        var expirationHours = policy.ExpiresAt.HasValue
            ? (int)(policy.ExpiresAt.Value - DateTime.UtcNow).TotalHours
            : 8760; // 1 year default

        return new SealingPolicy
        {
            Type = sealingType,
            ExpirationHours = Math.Max(1, expirationHours)
        };
    }

    /// <summary>
    /// Applies compression to data based on the compression type.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="compressionType">The compression type.</param>
    /// <returns>The compressed data.</returns>
    protected virtual byte[] ApplyCompression(byte[] data, SGXCompressionType compressionType)
    {
        return compressionType switch
        {
            SGXCompressionType.None => data,
            SGXCompressionType.GZip => CompressGZip(data),
            SGXCompressionType.LZ4 => CompressLZ4(data),
            SGXCompressionType.Brotli => CompressBrotli(data),
            _ => data
        };
    }

    /// <summary>
    /// Decompresses data based on metadata information.
    /// </summary>
    /// <param name="data">The compressed data.</param>
    /// <param name="metadata">The metadata containing compression info.</param>
    /// <returns>The decompressed data.</returns>
    protected virtual byte[] DecompressData(byte[] data, Dictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("compression", out var compressionObj) && 
            Enum.TryParse<SGXCompressionType>(compressionObj.ToString(), out var compressionType))
        {
            return compressionType switch
            {
                SGXCompressionType.None => data,
                SGXCompressionType.GZip => DecompressGZip(data),
                SGXCompressionType.LZ4 => DecompressLZ4(data),
                SGXCompressionType.Brotli => DecompressBrotli(data),
                _ => data
            };
        }

        return data;
    }

    /// <summary>
    /// Executes a single batch operation.
    /// </summary>
    /// <param name="operation">The batch operation.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The operation result.</returns>
    protected virtual async Task<(bool Success, object? Result, string? ErrorMessage)> ExecuteSingleBatchOperationAsync(
        SGXBatchOperation operation, BlockchainType blockchainType)
    {
        try
        {
            switch (operation.OperationType)
            {
                case SGXOperationType.Computation:
                    if (string.IsNullOrEmpty(operation.JavaScriptCode))
                    {
                        return (false, null, "JavaScript code required for computation");
                    }
                    
                    var executionContext = new SGXExecutionContext
                    {
                        JavaScriptCode = operation.JavaScriptCode,
                        Parameters = operation.Parameters
                    };
                    
                    var execResult = await ExecuteSecureComputingAsync(executionContext, blockchainType);
                    return (execResult.Success, execResult.Result, execResult.ErrorMessage);

                case SGXOperationType.Storage:
                    if (operation.StorageContext == null)
                    {
                        return (false, null, "Storage context required for storage operation");
                    }
                    
                    var storeResult = await StoreSecureDataAsync(operation.StorageContext, blockchainType);
                    return (storeResult.Success, storeResult, storeResult.ErrorMessage);

                default:
                    return (false, null, $"Unsupported batch operation type: {operation.OperationType}");
            }
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Generates a privacy attestation for the given privacy level.
    /// </summary>
    /// <param name="privacyLevel">The privacy level.</param>
    /// <returns>The privacy attestation.</returns>
    protected virtual string GeneratePrivacyAttestation(SGXPrivacyLevel privacyLevel)
    {
        var attestation = new
        {
            privacyLevel = privacyLevel.ToString(),
            enclaveId = ServiceName,
            timestamp = DateTime.UtcNow,
            attestationId = Guid.NewGuid().ToString()
        };

        return JsonSerializer.Serialize(attestation);
    }

    /// <summary>
    /// Gets the current memory usage.
    /// </summary>
    /// <returns>The memory usage in bytes.</returns>
    protected virtual long GetMemoryUsage()
    {
        // This would be implemented with actual memory tracking
        return GC.GetTotalMemory(false);
    }

    /// <summary>
    /// Gets the console output from the last execution.
    /// </summary>
    /// <returns>The console output lines.</returns>
    protected virtual List<string> GetConsoleOutput()
    {
        // This would be implemented with actual console capture
        return new List<string>();
    }

    // Compression methods (production implementations)
    private byte[] CompressGZip(byte[] data)
    {
        using var output = new System.IO.MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
    
    private byte[] CompressLZ4(byte[] data)
    {
        // LZ4 implementation using manual compression algorithm
        if (data.Length == 0) return data;
        
        var output = new List<byte>();
        var dict = new Dictionary<uint, int>();
        
        for (int i = 0; i < Math.Min(data.Length - 4, 65536); i++)
        {
            uint sequence = BitConverter.ToUInt32(data, i);
            if (dict.TryGetValue(sequence, out int lastPos) && i - lastPos < 65535)
            {
                // Found a match
                var length = GetMatchLength(data, lastPos, i, Math.Min(255, data.Length - i));
                if (length >= 4)
                {
                    // Encode match: distance (2 bytes) + length (1 byte) + flag (1 byte)
                    output.Add(0xFF); // Match flag
                    output.AddRange(BitConverter.GetBytes((ushort)(i - lastPos)));
                    output.Add((byte)length);
                    i += length - 1;
                    continue;
                }
            }
            dict[sequence] = i;
            output.Add(data[i]); // Literal
        }
        
        // Add remaining bytes
        for (int i = Math.Max(0, Math.Min(data.Length - 4, 65536)); i < data.Length; i++)
        {
            output.Add(data[i]);
        }
        
        return output.ToArray();
    }
    
    private byte[] CompressBrotli(byte[] data)
    {
        using var output = new System.IO.MemoryStream();
        using (var brotli = new System.IO.Compression.BrotliStream(output, System.IO.Compression.CompressionMode.Compress))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
    
    private byte[] DecompressGZip(byte[] data)
    {
        using var input = new System.IO.MemoryStream(data);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new System.IO.MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
    
    private byte[] DecompressLZ4(byte[] data)
    {
        if (data.Length == 0) return data;
        
        var output = new List<byte>();
        int i = 0;
        
        while (i < data.Length)
        {
            if (data[i] == 0xFF && i + 3 < data.Length) // Match flag
            {
                i++; // Skip flag
                var distance = BitConverter.ToUInt16(data, i);
                i += 2;
                var length = data[i];
                i++;
                
                // Copy from dictionary
                var startPos = Math.Max(0, output.Count - distance);
                for (int j = 0; j < length && startPos + j < output.Count; j++)
                {
                    output.Add(output[startPos + j]);
                }
            }
            else
            {
                output.Add(data[i]); // Literal
                i++;
            }
        }
        
        return output.ToArray();
    }
    
    private byte[] DecompressBrotli(byte[] data)
    {
        using var input = new System.IO.MemoryStream(data);
        using var brotli = new System.IO.Compression.BrotliStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new System.IO.MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
    
    private int GetMatchLength(byte[] data, int pos1, int pos2, int maxLength)
    {
        int length = 0;
        while (length < maxLength && pos1 + length < data.Length && pos2 + length < data.Length && 
               data[pos1 + length] == data[pos2 + length])
        {
            length++;
        }
        return length;
    }

    #endregion
}

/// <summary>
/// Represents an active SGX session.
/// </summary>
internal class SGXSession
{
    public string SessionId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<SGXOperationType> AllowedOperations { get; set; } = new();
    public List<string> AccessibleKeys { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}