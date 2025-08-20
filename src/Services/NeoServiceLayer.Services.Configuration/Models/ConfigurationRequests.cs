using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.Configuration.Models
{
    /// <summary>
    /// Request to update a configuration item
    /// </summary>
    public class ConfigurationUpdateRequest
    {
        [Required]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public object Value { get; set; } = null!;
        
        public string? Description { get; set; }
        
        // Compatibility property
        public string? Comment => Description;
        
        public Dictionary<string, object>? Metadata { get; set; }
        
        public bool? IsSecret { get; set; }
        
        public bool? IsReadOnly { get; set; }
    }

    /// <summary>
    /// Request to restore configuration from backup
    /// </summary>
    public class ConfigurationRestoreRequest
    {
        [Required]
        public string BackupId { get; set; } = string.Empty;
        
        public bool OverwriteExisting { get; set; } = true;
        
        public List<string>? IncludeKeys { get; set; }
        
        public List<string>? ExcludeKeys { get; set; }
        
        public bool ValidateBeforeRestore { get; set; } = true;
    }
}