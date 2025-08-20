using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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

        if (!IsEnclaveInitialized)
        {
            throw new InvalidOperationException("Enclave is not initialized.");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            try
            {
                _settingConfiguration(Logger, request.Key, blockchainType, null);

                // Validate configuration within the enclave
                await ValidateConfigurationInEnclaveAsync(request);

                var entry = new ConfigurationEntry
                {
                    Key = request.Key,
                    Value = request.Value,
                    ValueType = (Models.ConfigurationValueType)request.ValueType,
                    Description = request.Description,
                    EncryptValue = request.EncryptValue || IsSensitiveConfiguration(request.Key),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1,
                    BlockchainType = blockchainType.ToString()
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

                // Persist configuration securely in the enclave
                await PersistConfigurationAsync(entry);

                // Notify subscribers
                await NotifySubscribersAsync(request.Key, entry);

                _configurationSetSuccessfully(Logger, request.Key, entry.Version, null);

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
                _configurationSetFailed(Logger, request.Key, ex);
                return new ConfigurationSetResult
                {
                    Key = request.Key,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
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
            _gettingConfiguration(Logger, request.Key, blockchainType, null);

            await Task.Delay(1); // Simulate async configuration retrieval
            lock (_configLock)
            {
                if (_configurations.TryGetValue(request.Key, out var entry))
                {
                    return new ConfigurationResult
                    {
                        Key = entry.Key,
                        Value = ConvertValueToCorrectType(entry.Value, entry.ValueType),
                        ValueType = (ConfigurationValueType)entry.ValueType,
                        Version = entry.Version,
                        LastModified = entry.UpdatedAt,
                        Found = true,
                        Success = true
                    };
                }
            }

            _configurationNotFound(Logger, request.Key, blockchainType, null);

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
            _getConfigurationFailed(Logger, request.Key, ex);

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
            _deletingConfiguration(Logger, request.Key, blockchainType, null);

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

                _configurationDeletedSuccessfully(Logger, request.Key, null);
                return new ConfigurationDeleteResult
                {
                    Key = request.Key,
                    Success = true,
                    Timestamp = DateTime.UtcNow
                };
            }
            else
            {
                _configurationNotFoundForDeletion(Logger, request.Key, null);
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
            _deleteConfigurationFailed(Logger, request.Key, ex);
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
            _listingConfigurations(Logger, request.KeyPrefix ?? "all", blockchainType, null);

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
            _listConfigurationsFailed(Logger, ex);
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
            _batchConfigurationProcessing(Logger, batchId, requestList.Count, null);

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
            _batchConfigurationFailed(Logger, batchId, ex);

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
            _gettingConfigurationsByPattern(Logger, pattern, blockchainType, null);

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
            _getConfigurationsByPatternFailed(Logger, pattern, ex);
            return new ConfigurationListResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates configuration data within the enclave.
    /// </summary>
    /// <param name="request">The configuration set request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ValidateConfigurationInEnclaveAsync(SetConfigurationRequest request)
    {
        try
        {
            // Validate configuration key format
            if (string.IsNullOrWhiteSpace(request.Key) || request.Key.Length > 255)
            {
                throw new ArgumentException("Configuration key must be between 1 and 255 characters");
            }

            // Validate configuration value based on type
            var validationScript = $@"
                validateConfigurationValue('{request.Key}', '{request.Value}', '{request.ValueType}')
            ";

            var validationResult = await _enclaveManager.ExecuteJavaScriptAsync(validationScript);

            if (validationResult?.ToString() != "true")
            {
                throw new ArgumentException($"Configuration value validation failed for key {request.Key}");
            }

            _configurationValidationSuccess(Logger, request.Key, null);
        }
        catch (Exception ex)
        {
            _configurationValidationFailed(Logger, request.Key, ex);
            throw;
        }
    }

    /// <summary>
    /// Encrypts a configuration value within the enclave.
    /// </summary>
    /// <param name="value">The value to encrypt.</param>
    /// <returns>The encrypted value.</returns>
    private async Task<string> EncryptConfigurationValueAsync(string value)
    {
        try
        {
            var encryptionScript = $"encryptConfigurationValue('{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))}')";
            var encryptedResult = await _enclaveManager.ExecuteJavaScriptAsync(encryptionScript);

            return encryptedResult?.ToString() ?? throw new InvalidOperationException("Encryption failed");
        }
        catch (Exception ex)
        {
            _configurationEncryptionFailed(Logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Determines if a configuration key contains sensitive data.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the configuration is considered sensitive.</returns>
    private static bool IsSensitiveConfiguration(string key)
    {
        var sensitiveKeywords = new[]
        {
            "password", "secret", "key", "token", "credential", "private",
            "connection", "database", "api", "auth", "certificate", "cert"
        };

        var lowerKey = key.ToLowerInvariant();
        return sensitiveKeywords.Any(keyword => lowerKey.Contains(keyword));
    }

    /// <summary>
    /// Converts a value to the correct type based on the specified ValueType.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="valueType">The target value type.</param>
    /// <returns>The converted value.</returns>
    private object? ConvertValueToCorrectType(object? value, Models.ConfigurationValueType valueType)
    {
        if (value == null) return null;

        try
        {
            return valueType switch
            {
                Models.ConfigurationValueType.String => value.ToString(),
                Models.ConfigurationValueType.Integer => Convert.ToInt32(value),
                Models.ConfigurationValueType.Boolean => Convert.ToBoolean(value),
                Models.ConfigurationValueType.Double => Convert.ToDouble(value),
                Models.ConfigurationValueType.DateTime => Convert.ToDateTime(value),
                Models.ConfigurationValueType.JsonObject => value.ToString(),
                _ => value
            };
        }
        catch (Exception ex)
        {
            _configurationConversionWarning(Logger, value, valueType, ex);
            return value;
        }
    }
}
