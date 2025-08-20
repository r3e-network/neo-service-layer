using NeoServiceLayer.Services.Oracle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Services.Oracle.Models;

/// <summary>
/// Represents a data source for the Oracle service.
/// </summary>
public class DataSource
{
    /// <summary>
    /// Gets or sets the data source identifier.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source URL.
    /// </summary>
    [Required]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source type.
    /// </summary>
    public DataSourceType Type { get; set; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationConfig Authentication { get; set; } = new();

    /// <summary>
    /// Gets or sets the update interval in seconds.
    /// </summary>
    public int UpdateIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether the data source is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the last accessed timestamp.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Gets or sets the data source health information.
    /// </summary>
    public DataSourceHealth Health { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the blockchain type this data source is associated with.
    /// </summary>
    public NeoServiceLayer.Core.BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the access count for this data source.
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Gets or sets whether the data source is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the update frequency in seconds.
    /// </summary>
    public int UpdateFrequencySeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the data format type.
    /// </summary>
    public string DataFormat { get; set; } = "JSON";

    /// <summary>
    /// Gets or sets the extraction path for data parsing.
    /// </summary>
    public string ExtractionPath { get; set; } = "$";

    /// <summary>
    /// Gets or sets custom headers for HTTP requests.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets tags for categorizing the data source.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents rate limiting configuration for a data source.
/// </summary>
public class RateLimit
{
    /// <summary>
    /// Gets or sets the maximum requests per period.
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets the period in seconds.
    /// </summary>
    public int PeriodSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the current request count.
    /// </summary>
    public int CurrentCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets when the period resets.
    /// </summary>
    public DateTime PeriodResetAt { get; set; } = DateTime.UtcNow.AddSeconds(60);
}

// Note: DataSourceType and AuthenticationType enums are defined in OracleSupportingTypes.cs
