using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Configuration management operations for the Configuration Service.
/// </summary>
public partial class ConfigurationService
{
    /// <inheritdoc/>
    public async Task<ConfigurationSetResult> SetConfigurationAsync(SetConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Setting configuration {Key} on {Blockchain}", request.Key, blockchainType);

            // Validate configuration
            await ValidateConfigurationAsync(request);

            var entry = new ConfigurationEntry
            {
                Key = request.Key,
                Value = request.Value,
                ValueType = (Models.ConfigurationValueType)request.ValueType,
                Description = request.Description,
                EncryptValue = request.EncryptValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1,
                BlockchainType = blockchainType
            };

            lock (_configLock)
            {
                if (_configurations.TryGetValue(request.Key, out var existing))
                {
                    entry.Version = existing.Version + 1;
                    entry.CreatedAt = existing.CreatedAt;
                }

                _configurations[request.Key] = entry;
            }

            // Persist configuration
            await PersistConfigurationAsync(entry);

            // Notify subscribers
            await NotifySubscribersAsync(request.Key, entry);

            Logger.LogInformation("Configuration {Key} set successfully with version {Version}",
                request.Key, entry.Version);

            return new ConfigurationSetResult
            {
                Key = request.Key,
                Success = true,
                NewVersion = entry.Version,
                Timestamp = entry.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set configuration {Key}", request.Key);

            return new ConfigurationSetResult
            {
                Key = request.Key,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ConfigurationResult> GetConfigurationAsync(GetConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Getting configuration {Key} on {Blockchain}", request.Key, blockchainType);

            await Task.Delay(1); // Simulate async configuration retrieval
            lock (_configLock)
            {
                if (_configurations.TryGetValue(request.Key, out var entry))
                {
                    return new ConfigurationResult
                    {
                        Key = entry.Key,
                        Value = entry.Value,
                        ValueType = (ConfigurationValueType)entry.ValueType,
                        Version = entry.Version,
                        LastModified = entry.UpdatedAt,
                        Found = true,
                        Success = true
                    };
                }
            }

            Logger.LogWarning("Configuration {Key} not found on {Blockchain}", request.Key, blockchainType);

            return new ConfigurationResult
            {
                Key = request.Key,
                Value = request.DefaultValue,
                Found = false,
                Success = false,
                ErrorMessage = "Configuration not found"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get configuration {Key}", request.Key);

            return new ConfigurationResult
            {
                Key = request.Key,
                Found = false,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ConfigurationDeleteResult> DeleteConfigurationAsync(DeleteConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Deleting configuration {Key} on {Blockchain}", request.Key, blockchainType);

            ConfigurationEntry? removedEntry = null;

            lock (_configLock)
            {
                if (_configurations.TryGetValue(request.Key, out removedEntry))
                {
                    _configurations.Remove(request.Key);
                }
            }

            if (removedEntry != null)
            {
                // Remove from persistent storage
                await RemoveConfigurationFromStorageAsync(request.Key);

                // Notify subscribers
                await NotifySubscribersOfDeletionAsync(request.Key);

                Logger.LogInformation("Configuration {Key} deleted successfully", request.Key);
                return new ConfigurationDeleteResult
                {
                    Key = request.Key,
                    Success = true,
                    Timestamp = DateTime.UtcNow
                };
            }
            else
            {
                Logger.LogWarning("Configuration {Key} not found for deletion", request.Key);
                return new ConfigurationDeleteResult
                {
                    Key = request.Key,
                    Success = false,
                    ErrorMessage = "Configuration not found"
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete configuration {Key}", request.Key);
            return new ConfigurationDeleteResult
            {
                Key = request.Key,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ConfigurationListResult> ListConfigurationsAsync(ListConfigurationsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Listing configurations with prefix {Prefix} on {Blockchain}",
                request.KeyPrefix ?? "all", blockchainType);

            await Task.Delay(1); // Simulate async configuration listing
            List<ConfigurationEntry> configurations;

            lock (_configLock)
            {
                configurations = _configurations.Values
                    .Where(c => string.IsNullOrEmpty(request.KeyPrefix) || c.Key.StartsWith(request.KeyPrefix))
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToList();
            }

            return new ConfigurationListResult
            {
                Configurations = configurations.ToArray(),
                TotalCount = configurations.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to list configurations");
            return new ConfigurationListResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Updates multiple configurations in a batch operation.
    /// </summary>
    /// <param name="requests">The configuration update requests.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Batch update result.</returns>
    public async Task<BatchConfigurationResult> SetConfigurationsBatchAsync(IEnumerable<SetConfigurationRequest> requests, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        var batchId = Guid.NewGuid().ToString();
        var requestList = requests.ToList();

        try
        {
            Logger.LogInformation("Processing batch configuration update {BatchId} with {RequestCount} requests",
                batchId, requestList.Count);

            var results = new List<ConfigurationSetResult>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var request in requestList)
            {
                try
                {
                    var result = await SetConfigurationAsync(request, blockchainType);
                    results.Add(result);

                    if (result.Success)
                        successCount++;
                    else
                        failureCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new ConfigurationSetResult
                    {
                        Key = request.Key,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    failureCount++;
                }
            }

            return new BatchConfigurationResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                SuccessfulUpdates = successCount,
                FailedUpdates = failureCount,
                Results = results.Select(r => new Models.ConfigurationSetResult
                {
                    Key = r.Key,
                    Success = r.Success,
                    ErrorMessage = r.ErrorMessage,
                    NewVersion = r.NewVersion
                }).ToList(),
                ProcessedAt = DateTime.UtcNow,
                Success = failureCount == 0
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process batch configuration update {BatchId}", batchId);

            return new BatchConfigurationResult
            {
                BatchId = batchId,
                TotalRequests = requestList.Count,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets configuration by pattern matching.
    /// </summary>
    /// <param name="pattern">The key pattern to match.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Matching configurations.</returns>
    public async Task<ConfigurationListResult> GetConfigurationsByPatternAsync(string pattern, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Getting configurations by pattern {Pattern} on {Blockchain}", pattern, blockchainType);

            await Task.Delay(1); // Simulate async pattern matching
            List<ConfigurationEntry> matchingConfigs;

            lock (_configLock)
            {
                // Simple pattern matching - in production, this could use regex or more sophisticated matching
                matchingConfigs = _configurations.Values
                    .Where(c => c.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return new ConfigurationListResult
            {
                Configurations = matchingConfigs.ToArray(),
                TotalCount = matchingConfigs.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get configurations by pattern {Pattern}", pattern);
            return new ConfigurationListResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
