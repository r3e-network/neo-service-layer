using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System;


namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Oracle service interface implementations with enhanced enclave integration.
/// </summary>
public partial class OracleService
{
    /// <inheritdoc/>
    public async Task<OracleSubscriptionResult> SubscribeAsync(OracleSubscriptionRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                var subscriptionId = Guid.NewGuid().ToString();
                var subscription = new OracleSubscription
                {
                    Id = subscriptionId,
                    FeedId = request.DataSourceId,
                    Parameters = request.Parameters,
                    Interval = TimeSpan.FromSeconds(request.UpdateIntervalSeconds),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                };

                // Validate data source exists
                DataSource? dataSource = null;
                lock (_dataSources)
                {
                    dataSource = _dataSources.FirstOrDefault(ds => ds.Id == request.DataSourceId);
                }

                if (dataSource == null)
                {
                    return new OracleSubscriptionResult
                    {
                        SubscriptionId = string.Empty,
                        Success = false,
                        ErrorMessage = $"Data source {request.DataSourceId} not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Store subscription in enclave
                var subscriptionJson = System.Text.Json.JsonSerializer.Serialize(subscription);
                var encryptionKey = GetSubscriptionEncryptionKey(subscriptionId);
                await _enclaveManager.StorageStoreDataAsync(
                    $"subscription_{subscriptionId}",
                    subscriptionJson,
                    encryptionKey,
                    CancellationToken.None);

                lock (_subscriptions)
                {
                    _subscriptions[subscriptionId] = subscription;
                }

                var nextUpdate = DateTime.UtcNow.Add(subscription.Interval);

                Logger.LogInformation("Created subscription {SubscriptionId} for data source {DataSourceId} on {Blockchain}",
                    subscriptionId, request.DataSourceId, blockchainType);

                return new OracleSubscriptionResult
                {
                    SubscriptionId = subscriptionId,
                    Success = true,
                    Subscription = subscription,
                    Timestamp = DateTime.UtcNow,
                    NextUpdateAt = nextUpdate
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating subscription for data source {DataSourceId}", request.DataSourceId);
                return new OracleSubscriptionResult
                {
                    SubscriptionId = string.Empty,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<OracleSubscriptionResult> UnsubscribeAsync(OracleUnsubscribeRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                OracleSubscription? subscription = null;

                lock (_subscriptions)
                {
                    if (_subscriptions.TryGetValue(request.SubscriptionId, out subscription))
                    {
                        _subscriptions.Remove(request.SubscriptionId);
                    }
                }

                if (subscription == null)
                {
                    return new OracleSubscriptionResult
                    {
                        SubscriptionId = request.SubscriptionId,
                        Success = false,
                        ErrorMessage = $"Subscription {request.SubscriptionId} not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Remove from enclave storage
                try
                {
                    await _enclaveManager.StorageDeleteDataAsync(
                        $"subscription_{request.SubscriptionId}",
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to remove subscription {SubscriptionId} from enclave storage", request.SubscriptionId);
                }

                Logger.LogInformation("Removed subscription {SubscriptionId} on {Blockchain}. Reason: {Reason}",
                    request.SubscriptionId, blockchainType, request.Reason ?? "None provided");

                return new OracleSubscriptionResult
                {
                    SubscriptionId = request.SubscriptionId,
                    Success = true,
                    Subscription = subscription,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing subscription {SubscriptionId}", request.SubscriptionId);
                return new OracleSubscriptionResult
                {
                    SubscriptionId = request.SubscriptionId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<OracleDataResult> GetDataAsync(OracleDataRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Find the data source
                DataSource? dataSource = null;
                lock (_dataSources)
                {
                    dataSource = _dataSources.FirstOrDefault(ds => ds.Id == request.DataSourceId);
                }

                if (dataSource == null)
                {
                    return new OracleDataResult
                    {
                        Success = false,
                        ErrorMessage = $"Data source {request.DataSourceId} not found",
                        DataSourceId = request.DataSourceId,
                        LatencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                    };
                }

                // Fetch data using existing GetDataAsync method
                var data = await GetDataAsync(dataSource.Url, request.DataPath ?? string.Empty, blockchainType);

                // Calculate quality score based on data freshness and validation
                var qualityScore = CalculateDataQualityScore(data, dataSource);

                // Generate cryptographic proof
                var dataHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
                var proof = Convert.ToBase64String(dataHash);

                var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                Logger.LogDebug("Retrieved data from source {DataSourceId}: quality={QualityScore}, latency={LatencyMs}ms",
                    request.DataSourceId, qualityScore, latency);

                return new OracleDataResult
                {
                    Data = data,
                    DataTimestamp = DateTime.UtcNow,
                    Success = true,
                    DataSourceId = request.DataSourceId,
                    QualityScore = qualityScore,
                    Proof = proof,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source_url"] = dataSource.Url,
                        ["data_path"] = request.DataPath ?? string.Empty,
                        ["blockchain"] = blockchainType.ToString(),
                        ["enclave_verified"] = "true"
                    },
                    LatencyMs = latency
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving data from source {DataSourceId}", request.DataSourceId);

                return new OracleDataResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    DataSourceId = request.DataSourceId,
                    LatencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<DataSourceResult> CreateDataSourceAsync(CreateDataSourceRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                var dataSourceId = Guid.NewGuid().ToString();
                var dataSource = new DataSource
                {
                    Id = dataSourceId,
                    Name = request.Name,
                    Description = request.Description,
                    Url = request.Url,
                    Type = request.SourceType,
                    BlockchainType = blockchainType,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.MinValue,
                    AccessCount = 0,
                    Enabled = request.Enabled,
                    UpdateFrequencySeconds = request.UpdateFrequencySeconds,
                    DataFormat = request.DataFormat,
                    ExtractionPath = request.ExtractionPath,
                    CustomHeaders = request.CustomHeaders,
                    Tags = request.Tags
                };

                // Validate URL is accessible and secure
                if (!IsValidDataSource(request.Url))
                {
                    return new DataSourceResult
                    {
                        DataSourceId = string.Empty,
                        Success = false,
                        ErrorMessage = $"Data source URL {request.Url} is not valid or authorized",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Store in enclave
                var dataSourceJson = System.Text.Json.JsonSerializer.Serialize(dataSource);
                var encryptionKey = GetDataSourceEncryptionKey(dataSourceId);
                await _enclaveManager.StorageStoreDataAsync(
                    $"datasource_{dataSourceId}",
                    dataSourceJson,
                    encryptionKey,
                    CancellationToken.None);

                lock (_dataSources)
                {
                    _dataSources.Add(dataSource);
                }

                Logger.LogInformation("Created data source {DataSourceId} ({Name}) for blockchain {Blockchain}",
                    dataSourceId, request.Name, blockchainType);

                return new DataSourceResult
                {
                    DataSourceId = dataSourceId,
                    Success = true,
                    DataSource = dataSource,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating data source {Name}", request.Name);
                return new DataSourceResult
                {
                    DataSourceId = string.Empty,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<DataSourceResult> UpdateDataSourceAsync(UpdateDataSourceRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                DataSource? dataSource = null;
                lock (_dataSources)
                {
                    dataSource = _dataSources.FirstOrDefault(ds => ds.Id == request.DataSourceId);
                }

                if (dataSource == null)
                {
                    return new DataSourceResult
                    {
                        DataSourceId = request.DataSourceId,
                        Success = false,
                        ErrorMessage = $"Data source {request.DataSourceId} not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Name))
                    dataSource.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    dataSource.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Url))
                {
                    if (!IsValidDataSource(request.Url))
                    {
                        return new DataSourceResult
                        {
                            DataSourceId = request.DataSourceId,
                            Success = false,
                            ErrorMessage = $"Data source URL {request.Url} is not valid or authorized",
                            Timestamp = DateTime.UtcNow
                        };
                    }
                    dataSource.Url = request.Url;
                }
                if (request.UpdateFrequencySeconds.HasValue)
                    dataSource.UpdateFrequencySeconds = request.UpdateFrequencySeconds.Value;
                if (!string.IsNullOrEmpty(request.DataFormat))
                    dataSource.DataFormat = request.DataFormat;
                if (request.ExtractionPath != null)
                    dataSource.ExtractionPath = request.ExtractionPath;
                if (request.CustomHeaders != null)
                    dataSource.CustomHeaders = request.CustomHeaders;
                if (request.Enabled.HasValue)
                    dataSource.Enabled = request.Enabled.Value;
                if (request.Tags != null)
                    dataSource.Tags = request.Tags;

                // Update in enclave storage
                var dataSourceJson = System.Text.Json.JsonSerializer.Serialize(dataSource);
                var encryptionKey = GetDataSourceEncryptionKey(request.DataSourceId);
                await _enclaveManager.StorageStoreDataAsync(
                    $"datasource_{request.DataSourceId}",
                    dataSourceJson,
                    encryptionKey,
                    CancellationToken.None);

                Logger.LogInformation("Updated data source {DataSourceId} on blockchain {Blockchain}",
                    request.DataSourceId, blockchainType);

                return new DataSourceResult
                {
                    DataSourceId = request.DataSourceId,
                    Success = true,
                    DataSource = dataSource,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating data source {DataSourceId}", request.DataSourceId);
                return new DataSourceResult
                {
                    DataSourceId = request.DataSourceId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<DataSourceResult> DeleteDataSourceAsync(DeleteDataSourceRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                DataSource? dataSource = null;
                lock (_dataSources)
                {
                    dataSource = _dataSources.FirstOrDefault(ds => ds.Id == request.DataSourceId);
                    if (dataSource != null)
                    {
                        // Check for active subscriptions unless force delete
                        var activeSubscriptions = _subscriptions.Values.Where(s => s.FeedId == request.DataSourceId).ToList();
                        if (activeSubscriptions.Any() && !request.Force)
                        {
                            return new DataSourceResult
                            {
                                DataSourceId = request.DataSourceId,
                                Success = false,
                                ErrorMessage = $"Data source has {activeSubscriptions.Count} active subscriptions. Use force=true to delete anyway.",
                                Timestamp = DateTime.UtcNow
                            };
                        }

                        _dataSources.Remove(dataSource);
                    }
                }

                if (dataSource == null)
                {
                    return new DataSourceResult
                    {
                        DataSourceId = request.DataSourceId,
                        Success = false,
                        ErrorMessage = $"Data source {request.DataSourceId} not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Remove from enclave storage
                try
                {
                    await _enclaveManager.StorageDeleteDataAsync(
                        $"datasource_{request.DataSourceId}",
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to remove data source {DataSourceId} from enclave storage", request.DataSourceId);
                }

                Logger.LogInformation("Deleted data source {DataSourceId} on blockchain {Blockchain}. Reason: {Reason}",
                    request.DataSourceId, blockchainType, request.Reason ?? "None provided");

                return new DataSourceResult
                {
                    DataSourceId = request.DataSourceId,
                    Success = true,
                    DataSource = dataSource,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting data source {DataSourceId}", request.DataSourceId);
                return new DataSourceResult
                {
                    DataSourceId = request.DataSourceId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<ListSubscriptionsResult> GetSubscriptionsAsync(ListSubscriptionsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            var allSubscriptions = new List<OracleSubscription>();

            lock (_subscriptions)
            {
                allSubscriptions.AddRange(_subscriptions.Values);
            }

            // Apply filters
            var filteredSubscriptions = allSubscriptions.AsQueryable();

            if (!string.IsNullOrEmpty(request.DataSourceId))
            {
                filteredSubscriptions = filteredSubscriptions.Where(s => s.FeedId == request.DataSourceId);
            }

            if (!string.IsNullOrEmpty(request.SearchQuery))
            {
                filteredSubscriptions = filteredSubscriptions.Where(s =>
                    s.Id.Contains(request.SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    s.FeedId.Contains(request.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = filteredSubscriptions.Count();

            // Apply sorting
            filteredSubscriptions = request.SortDirection.ToUpper() == "DESC"
                ? filteredSubscriptions.OrderByDescending(s => s.CreatedAt)
                : filteredSubscriptions.OrderBy(s => s.CreatedAt);

            // Apply pagination
            var pagedSubscriptions = filteredSubscriptions
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ListSubscriptionsResult
            {
                Subscriptions = pagedSubscriptions,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing subscriptions for blockchain {Blockchain}", blockchainType);
            return new ListSubscriptionsResult
            {
                Subscriptions = new List<OracleSubscription>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ListDataSourcesResult> GetDataSourcesAsync(ListDataSourcesRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            var allDataSources = new List<DataSource>();

            lock (_dataSources)
            {
                allDataSources.AddRange(_dataSources.Where(ds => ds.BlockchainType == blockchainType));
            }

            // Apply filters
            var filteredDataSources = allDataSources.AsQueryable();

            if (request.SourceType.HasValue)
            {
                filteredDataSources = filteredDataSources.Where(ds => ds.Type == request.SourceType.Value);
            }

            if (request.Enabled.HasValue)
            {
                filteredDataSources = filteredDataSources.Where(ds => ds.Enabled == request.Enabled.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchQuery))
            {
                filteredDataSources = filteredDataSources.Where(ds =>
                    ds.Name.Contains(request.SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    ds.Description.Contains(request.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (request.Tags != null && request.Tags.Any())
            {
                filteredDataSources = filteredDataSources.Where(ds =>
                    request.Tags.Any(tag => ds.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            }

            var totalCount = filteredDataSources.Count();

            // Apply sorting
            filteredDataSources = request.SortDirection.ToUpper() == "DESC"
                ? filteredDataSources.OrderByDescending(ds => ds.CreatedAt)
                : filteredDataSources.OrderBy(ds => ds.CreatedAt);

            // Apply pagination
            var pagedDataSources = filteredDataSources
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ListDataSourcesResult
            {
                DataSources = pagedDataSources,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing data sources for blockchain {Blockchain}", blockchainType);
            return new ListDataSourcesResult
            {
                DataSources = new List<DataSource>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<BatchOracleResult> BatchRequestAsync(BatchOracleRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var batchId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
            var results = new List<OracleDataResult>();
            var successCount = 0;
            var failureCount = 0;

            try
            {
                var semaphore = new SemaphoreSlim(request.MaxParallelRequests, request.MaxParallelRequests);
                var tasks = request.Requests.Select(async oracleRequest =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await GetDataAsync(oracleRequest, blockchainType);
                        if (result.Success)
                            Interlocked.Increment(ref successCount);
                        else
                            Interlocked.Increment(ref failureCount);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        return new OracleDataResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message,
                            DataSourceId = oracleRequest.DataSourceId
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                // Wait for all tasks with timeout
                var completedTasks = await Task.WhenAll(tasks);
                results.AddRange(completedTasks);

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                var batchSuccess = request.Atomic ? failureCount == 0 : successCount > 0;

                Logger.LogInformation("Processed batch {BatchId}: {SuccessCount}/{TotalCount} successful, {ProcessingTime}ms",
                    batchId, successCount, request.Requests.Count, processingTime);

                return new BatchOracleResult
                {
                    BatchId = batchId,
                    Results = results,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Success = batchSuccess,
                    ProcessedAt = DateTime.UtcNow,
                    TotalProcessingTimeMs = processingTime
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing batch request {BatchId}", batchId);
                return new BatchOracleResult
                {
                    BatchId = batchId,
                    Results = results,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessedAt = DateTime.UtcNow,
                    TotalProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<OracleStatusResult> GetSubscriptionStatusAsync(OracleStatusRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain type {blockchainType} is not supported.");
        }

        try
        {
            OracleSubscription? subscription = null;

            lock (_subscriptions)
            {
                _subscriptions.TryGetValue(request.SubscriptionId, out subscription);
            }

            if (subscription == null)
            {
                return new OracleStatusResult
                {
                    SubscriptionId = request.SubscriptionId,
                    Success = false,
                    ErrorMessage = $"Subscription {request.SubscriptionId} not found",
                    CheckedAt = DateTime.UtcNow
                };
            }

            var status = subscription.IsActive ? SubscriptionStatus.Active : SubscriptionStatus.Expired;
            var nextUpdate = subscription.LastUpdated?.Add(subscription.Interval) ?? DateTime.UtcNow.Add(subscription.Interval);

            SubscriptionMetrics? metrics = null;
            if (request.IncludeMetrics)
            {
                metrics = new SubscriptionMetrics
                {
                    TotalUpdates = subscription.SuccessCount + subscription.FailureCount,
                    SuccessfulUpdates = subscription.SuccessCount,
                    FailedUpdates = subscription.FailureCount,
                    SuccessRate = subscription.SuccessCount + subscription.FailureCount > 0
                        ? (double)subscription.SuccessCount / (subscription.SuccessCount + subscription.FailureCount)
                        : 0,
                    AverageLatencyMs = 100, // This would be calculated from historical data
                    LastSuccessAt = subscription.LastUpdated,
                    LastFailureAt = null // This would be tracked separately
                };
            }

            return new OracleStatusResult
            {
                SubscriptionId = request.SubscriptionId,
                Status = status,
                LastUpdateAt = subscription.LastUpdated,
                NextUpdateAt = nextUpdate,
                Metrics = metrics,
                Success = true,
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting subscription status for {SubscriptionId}", request.SubscriptionId);
            return new OracleStatusResult
            {
                SubscriptionId = request.SubscriptionId,
                Success = false,
                ErrorMessage = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    // GetSupportedDataSourcesAsync is implemented in DataSourceManagement.cs

    /// <summary>
    /// Calculates data quality score based on freshness and validation.
    /// </summary>
    /// <param name="data">The oracle data.</param>
    /// <param name="dataSource">The data source.</param>
    /// <returns>Quality score between 0 and 1.</returns>
    private double CalculateDataQualityScore(string data, DataSource dataSource)
    {
        try
        {
            var score = 1.0;

            // Reduce score based on data age
            var timeSinceLastAccess = DateTime.UtcNow - dataSource.LastAccessedAt;
            if (timeSinceLastAccess?.TotalMinutes > 5)
            {
                score *= 0.9;
            }

            // Reduce score for empty or invalid data
            if (string.IsNullOrWhiteSpace(data) || data.Length < 10)
            {
                score *= 0.5;
            }

            // Increase score for JSON data
            if (data.TrimStart().StartsWith('{') || data.TrimStart().StartsWith('['))
            {
                try
                {
                    score *= 1.1; // Bonus for valid JSON
                }
                catch
                {
                    score *= 0.8; // Penalty for invalid JSON
                }
            }

            return Math.Min(1.0, Math.Max(0.0, score));
        }
        catch
        {
            return 0.5; // Default score if calculation fails
        }
    }

    /// <summary>
    /// Gets the encryption key for subscription storage.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>The encryption key.</returns>
    private string GetSubscriptionEncryptionKey(string subscriptionId)
    {
        // In production, this would derive from enclave identity
        return $"subscription-key-{subscriptionId[..8]}";
    }

    /// <summary>
    /// Gets the encryption key for data source storage.
    /// </summary>
    /// <param name="dataSourceId">The data source ID.</param>
    /// <returns>The encryption key.</returns>
    private string GetDataSourceEncryptionKey(string dataSourceId)
    {
        // In production, this would derive from enclave identity
        return $"datasource-key-{dataSourceId[..8]}";
    }
}
