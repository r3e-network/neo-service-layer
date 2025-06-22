using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration;

/// <summary>
/// Interface for the Configuration Service that provides dynamic configuration management.
/// </summary>
public interface IConfigurationService : IService
{
    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <param name="request">The configuration get request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration value.</returns>
    Task<ConfigurationResult> GetConfigurationAsync(GetConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="request">The configuration set request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration set result.</returns>
    Task<ConfigurationSetResult> SetConfigurationAsync(SetConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    /// <param name="request">The configuration delete request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration delete result.</returns>
    Task<ConfigurationDeleteResult> DeleteConfigurationAsync(DeleteConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Lists configuration keys and values.
    /// </summary>
    /// <param name="request">The configuration list request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration list result.</returns>
    Task<ConfigurationListResult> ListConfigurationsAsync(ListConfigurationsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Validates configuration values.
    /// </summary>
    /// <param name="request">The configuration validation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The validation result.</returns>
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(ValidateConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates a configuration schema.
    /// </summary>
    /// <param name="request">The schema creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The schema creation result.</returns>
    Task<ConfigurationSchemaResult> CreateSchemaAsync(CreateSchemaRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Exports configuration data.
    /// </summary>
    /// <param name="request">The configuration export request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The export result.</returns>
    Task<ConfigurationExportResult> ExportConfigurationAsync(ExportConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Imports configuration data.
    /// </summary>
    /// <param name="request">The configuration import request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The import result.</returns>
    Task<ConfigurationImportResult> ImportConfigurationAsync(ImportConfigurationRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Subscribes to configuration changes.
    /// </summary>
    /// <param name="request">The configuration subscription request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The subscription result.</returns>
    Task<ConfigurationSubscriptionResult> SubscribeToChangesAsync(SubscribeToChangesRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets configuration change history.
    /// </summary>
    /// <param name="request">The configuration history request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The configuration history.</returns>
    Task<ConfigurationHistoryResult> GetConfigurationHistoryAsync(GetConfigurationHistoryRequest request, BlockchainType blockchainType);
}


