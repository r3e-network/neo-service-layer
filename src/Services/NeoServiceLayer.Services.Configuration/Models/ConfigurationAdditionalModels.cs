using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Configuration.Models;

/// <summary>
/// Import error for configuration import operations.
/// </summary>
public class ImportError
{
    /// <summary>
    /// Gets or sets the configuration key that caused the error.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Create schema request.
/// </summary>
public class CreateSchemaRequest
{
    /// <summary>
    /// Gets or sets the schema identifier.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string SchemaId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema definition.
    /// </summary>
    [Required]
    public object Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Configuration schema result.
/// </summary>
public class ConfigurationSchemaResult
{
    /// <summary>
    /// Gets or sets the schema identifier.
    /// </summary>
    public string SchemaId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the created schema.
    /// </summary>
    public object? Schema { get; set; }

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Export configuration request.
/// </summary>
public class ExportConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration keys to export.
    /// </summary>
    public List<string> Keys { get; set; } = new();

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Gets or sets whether to include metadata.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the key prefix filter for export.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets who is exporting the configuration.
    /// </summary>
    public string? ExportedBy { get; set; }
}

/// <summary>
/// Configuration export result.
/// </summary>
public class ConfigurationExportResult
{
    /// <summary>
    /// Gets or sets the exported data.
    /// </summary>
    public string ExportData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Gets or sets the number of exported configurations.
    /// </summary>
    public int ExportedCount { get; set; }

    /// <summary>
    /// Gets or sets when the export was processed.
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the count of configurations in the export.
    /// </summary>
    public int ConfigurationCount { get; set; }
}

/// <summary>
/// Import configuration request.
/// </summary>
public class ImportConfigurationRequest
{
    /// <summary>
    /// Gets or sets the configuration data to import.
    /// </summary>
    [Required]
    public string ImportData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration data (alias for ImportData).
    /// </summary>
    public string ConfigurationData 
    { 
        get => ImportData; 
        set => ImportData = value; 
    }

    /// <summary>
    /// Gets or sets the import format.
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Gets or sets whether to overwrite existing configurations.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Configuration import result.
/// </summary>
public class ConfigurationImportResult
{
    /// <summary>
    /// Gets or sets the number of imported configurations.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of skipped configurations.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed imports.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of import errors.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the import errors that occurred.
    /// </summary>
    public ImportError[] ImportErrors { get; set; } = Array.Empty<ImportError>();

    /// <summary>
    /// Gets or sets when the import was processed.
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Subscribe to changes request.
/// </summary>
public class SubscribeToChangesRequest
{
    /// <summary>
    /// Gets or sets the key pattern to subscribe to.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string KeyPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL for notifications.
    /// </summary>
    [Required]
    [Url]
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;
}

/// <summary>
/// Configuration subscription result.
/// </summary>
public class ConfigurationSubscriptionResult
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets the key pattern.
    /// </summary>
    public string? KeyPattern { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Get configuration history request.
/// </summary>
public class GetConfigurationHistoryRequest
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date for history.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for history.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of history entries.
    /// </summary>
    [Range(1, 1000)]
    public int MaxEntries { get; set; } = 100;

    /// <summary>
    /// Gets or sets the limit (alias for MaxEntries).
    /// </summary>
    public int Limit 
    { 
        get => MaxEntries; 
        set => MaxEntries = value; 
    }
}

/// <summary>
/// Configuration history result.
/// </summary>
public class ConfigurationHistoryResult
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration history entries.
    /// </summary>
    public ConfigurationHistoryEntry[] HistoryEntries { get; set; } = Array.Empty<ConfigurationHistoryEntry>();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total count of history entries.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Configuration history entry.
/// </summary>
public class ConfigurationHistoryEntry
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets when the configuration changed.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the change type.
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who made the change.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the change reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// Batch configuration result.
/// </summary>
public class BatchConfigurationResult
{
    /// <summary>
    /// Gets or sets the batch identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of successful updates.
    /// </summary>
    public int SuccessfulUpdates { get; set; }

    /// <summary>
    /// Gets or sets the number of failed updates.
    /// </summary>
    public int FailedUpdates { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests processed.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets when the batch was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the individual operation results.
    /// </summary>
    public List<ConfigurationSetResult> Results { get; set; } = new();
}

/// <summary>
/// Configuration statistics.
/// </summary>
public class ConfigurationStatistics
{
    /// <summary>
    /// Gets or sets the total number of configurations.
    /// </summary>
    public int TotalConfigurations { get; set; }

    /// <summary>
    /// Gets or sets the number of active configurations.
    /// </summary>
    public int ActiveConfigurations { get; set; }

    /// <summary>
    /// Gets or sets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the number of encrypted configurations.
    /// </summary>
    public int EncryptedConfigurations { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the configuration count by scope.
    /// </summary>
    public Dictionary<string, int> CountByScope { get; set; } = new();

    /// <summary>
    /// Gets or sets the configurations by type.
    /// </summary>
    public Dictionary<string, int> ConfigurationsByType { get; set; } = new();
}

/// <summary>
/// Configuration unsubscribe result.
/// </summary>
public class ConfigurationUnsubscribeResult
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the subscription was removed.
    /// </summary>
    public DateTime? RemovedAt { get; set; }
}

/// <summary>
/// Configuration subscription list result.
/// </summary>
public class ConfigurationSubscriptionListResult
{
    /// <summary>
    /// Gets or sets the list of subscriptions.
    /// </summary>
    public List<ConfigurationSubscription> Subscriptions { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total count of subscriptions.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Configuration subscription.
/// </summary>
public class ConfigurationSubscription
{
    /// <summary>
    /// Gets or sets the subscription identifier.
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key pattern.
    /// </summary>
    public string KeyPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string ConfigurationKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback URL.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscriber endpoint.
    /// </summary>
    public string SubscriberEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the subscription status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Subscription statistics.
/// </summary>
public class SubscriptionStatistics
{
    /// <summary>
    /// Gets or sets the total number of subscriptions.
    /// </summary>
    public int TotalSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the number of inactive subscriptions.
    /// </summary>
    public int InactiveSubscriptions { get; set; }

    /// <summary>
    /// Gets or sets the number of notifications sent today.
    /// </summary>
    public int NotificationsSentToday { get; set; }

    /// <summary>
    /// Gets or sets the last notification timestamp.
    /// </summary>
    public DateTime? LastNotificationTime { get; set; }

    /// <summary>
    /// Gets or sets the oldest subscription timestamp.
    /// </summary>
    public DateTime? OldestSubscription { get; set; }

    /// <summary>
    /// Gets or sets the newest subscription timestamp.
    /// </summary>
    public DateTime? NewestSubscription { get; set; }

    /// <summary>
    /// Gets or sets additional processing information.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}