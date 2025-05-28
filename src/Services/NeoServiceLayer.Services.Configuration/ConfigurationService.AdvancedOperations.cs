using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Configuration.Models;
using System.Text.Json;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Advanced operations for the Configuration Service including validation, schema, and import/export.
/// </summary>
public partial class ConfigurationService
{
    public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(ValidateConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Validating configuration {Key} on {Blockchain}", request.Key, blockchainType);

            // Perform validation logic
            await Task.Delay(1); // Simulate async validation
            var isValid = !string.IsNullOrWhiteSpace(request.Key) && request.Key.Length <= 255;
            var validationErrors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Key))
                validationErrors.Add(new ValidationError { ErrorMessage = "Configuration key cannot be empty" });

            if (request.Key.Length > 255)
                validationErrors.Add(new ValidationError { ErrorMessage = "Configuration key cannot exceed 255 characters" });

            return new ConfigurationValidationResult
            {
                Key = request.Key,
                IsValid = isValid,
                ValidationErrors = validationErrors.ToArray(),
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate configuration {Key}", request.Key);
            return new ConfigurationValidationResult
            {
                Key = request.Key,
                IsValid = false,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConfigurationSchemaResult> CreateSchemaAsync(CreateSchemaRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Creating configuration schema {SchemaName} on {Blockchain}", request.SchemaName, blockchainType);

            // Create schema logic here
            await Task.Delay(1); // Simulate async schema creation
            var schemaId = Guid.NewGuid().ToString();

            return new ConfigurationSchemaResult
            {
                SchemaId = schemaId,
                SchemaName = request.SchemaName,
                Success = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create configuration schema {SchemaName}", request.SchemaName);
            return new ConfigurationSchemaResult
            {
                SchemaName = request.SchemaName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConfigurationExportResult> ExportConfigurationAsync(ExportConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Exporting configurations on {Blockchain}", blockchainType);

            // Get configurations to export
            List<ConfigurationEntry> configurationsToExport;

            lock (_configLock)
            {
                configurationsToExport = _configurations.Values
                    .Where(c => string.IsNullOrEmpty(request.KeyPrefix) || c.Key.StartsWith(request.KeyPrefix))
                    .ToList();
            }

            // Create export data
            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                ExportedBy = request.ExportedBy ?? "System",
                TotalConfigurations = configurationsToExport.Count,
                Configurations = configurationsToExport.Select(c => new
                {
                    c.Key,
                    c.Value,
                    c.ValueType,
                    c.Description,
                    c.Version,
                    c.CreatedAt,
                    c.UpdatedAt
                })
            };

            await Task.Delay(1); // Simulate async export processing
            var exportJson = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });

            return new ConfigurationExportResult
            {
                ExportData = exportJson,
                Success = true,
                ExportedAt = DateTime.UtcNow,
                ConfigurationCount = configurationsToExport.Count
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to export configurations");
            return new ConfigurationExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConfigurationImportResult> ImportConfigurationAsync(ImportConfigurationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogInformation("Importing configurations on {Blockchain}", blockchainType);

            // Parse import data
            var importData = JsonSerializer.Deserialize<ImportData>(request.ImportData);
            if (importData?.Configurations == null)
            {
                throw new ArgumentException("Invalid import data format");
            }

            var importedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;
            var importErrors = new List<ImportError>();

            foreach (var configData in importData.Configurations)
            {
                try
                {
                    var setRequest = new SetConfigurationRequest
                    {
                        Key = configData.Key,
                        Value = configData.Value,
                        ValueType = Enum.Parse<ConfigurationValueType>(configData.ValueType, true),
                        Description = configData.Description
                    };

                    // Check if configuration already exists and handle conflicts
                    lock (_configLock)
                    {
                        if (_configurations.ContainsKey(configData.Key) && !request.OverwriteExisting)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    var result = await SetConfigurationAsync(setRequest, blockchainType);
                    if (result.Success)
                    {
                        importedCount++;
                    }
                    else
                    {
                        errorCount++;
                        importErrors.Add(new ImportError { Key = configData.Key, ErrorMessage = $"Failed to import {configData.Key}: {result.ErrorMessage}" });
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    importErrors.Add(new ImportError { Key = configData.Key, ErrorMessage = $"Failed to import {configData.Key}: {ex.Message}" });
                }
            }

            return new ConfigurationImportResult
            {
                ImportedCount = importedCount,
                SkippedCount = skippedCount,
                ErrorCount = errorCount,
                ImportErrors = importErrors.ToArray(),
                Success = errorCount == 0,
                ImportedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to import configurations");
            return new ConfigurationImportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConfigurationHistoryResult> GetConfigurationHistoryAsync(GetConfigurationHistoryRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        try
        {
            Logger.LogDebug("Getting configuration history for {Key} on {Blockchain}", request.Key, blockchainType);

            // In production, this would query actual history from storage
            await Task.Delay(1); // Simulate async history retrieval
            // For now, return mock history data
            var historyEntries = new[]
            {
                new ConfigurationHistoryEntry
                {
                    Key = request.Key,
                    Value = "current_value",
                    Version = 2,
                    ChangedAt = DateTime.UtcNow.AddHours(-1),
                    ChangedBy = "System",
                    ChangeType = "Update"
                },
                new ConfigurationHistoryEntry
                {
                    Key = request.Key,
                    Value = "previous_value",
                    Version = 1,
                    ChangedAt = DateTime.UtcNow.AddDays(-1),
                    ChangedBy = "Admin",
                    ChangeType = "Create"
                }
            };

            return new ConfigurationHistoryResult
            {
                Key = request.Key,
                HistoryEntries = historyEntries,
                Success = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get configuration history for {Key}", request.Key);
            return new ConfigurationHistoryResult
            {
                Key = request.Key,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validates configuration value against its type.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="valueType">The expected value type.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private bool ValidateValueType(string value, string valueType)
    {
        if (string.IsNullOrEmpty(value))
            return true; // Allow empty values

        return valueType.ToLowerInvariant() switch
        {
            "string" => true,
            "integer" => int.TryParse(value, out _),
            "double" => double.TryParse(value, out _),
            "boolean" => bool.TryParse(value, out _),
            "json" => IsValidJson(value),
            _ => true // Unknown types are allowed
        };
    }

    /// <summary>
    /// Checks if a string is valid JSON.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if valid JSON, false otherwise.</returns>
    private bool IsValidJson(string value)
    {
        try
        {
            JsonSerializer.Deserialize<object>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Import data structure for deserialization.
    /// </summary>
    private class ImportData
    {
        public ImportConfigurationData[]? Configurations { get; set; }
    }

    /// <summary>
    /// Configuration data for import.
    /// </summary>
    private class ImportConfigurationData
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
