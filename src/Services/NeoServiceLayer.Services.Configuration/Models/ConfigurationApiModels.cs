using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Configuration.Models
{
    /// <summary>
    /// Represents a configuration item with metadata
    /// </summary>
    public class ConfigurationItem
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool IsSecret { get; set; }
        public bool IsReadOnly { get; set; }
        public DateTime LastModified { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of a configuration update operation
    /// </summary>
    public class ConfigurationUpdateResult
    {
        public bool Success { get; set; }
        public string Key { get; set; } = string.Empty;
        public object PreviousValue { get; set; } = null!;
        public object NewValue { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Result of a configuration backup operation
    /// </summary>
    public class ConfigurationBackupResult
    {
        public bool Success { get; set; }
        public string BackupId { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public long SizeBytes { get; set; }
        public string Format { get; set; } = "json";
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a configuration restore operation
    /// </summary>
    public class ConfigurationRestoreResult
    {
        public bool Success { get; set; }
        public string BackupId { get; set; } = string.Empty;
        public DateTime RestoredAt { get; set; }
        public string RestoredBy { get; set; } = string.Empty;
        public int ItemsRestored { get; set; }
        public int ItemsSkipped { get; set; }
        public int ItemsFailed { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }
}