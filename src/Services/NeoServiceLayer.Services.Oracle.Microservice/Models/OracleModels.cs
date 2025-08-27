using System.ComponentModel.DataAnnotations;

namespace Neo.Oracle.Service.Models;

public class OracleConfiguration
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Symbol { get; set; }
    public required string DataSource { get; set; }
    public string[] ApiEndpoints { get; set; } = Array.Empty<string>();
    public int UpdateIntervalSeconds { get; set; } = 60;
    public decimal DeviationThreshold { get; set; } = 0.05m; // 5%
    public int ConsensusThreshold { get; set; } = 3; // Minimum sources for consensus
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Metadata { get; set; } = "{}";
}

public class PriceFeed
{
    public Guid Id { get; set; }
    public required Guid ConfigurationId { get; set; }
    public required string Symbol { get; set; }
    public required string Source { get; set; }
    public decimal Price { get; set; }
    public decimal Volume24h { get; set; }
    public decimal Change24h { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ConfidenceScore { get; set; } = 100; // 0-100
    public string? RawData { get; set; }
    public bool IsValidated { get; set; }
    public string? ValidationErrors { get; set; }
    
    // Navigation properties
    public OracleConfiguration Configuration { get; set; } = null!;
}

public class ConsensusResult
{
    public Guid Id { get; set; }
    public required string Symbol { get; set; }
    public decimal ConsensusPrice { get; set; }
    public decimal WeightedPrice { get; set; }
    public decimal StandardDeviation { get; set; }
    public int SourceCount { get; set; }
    public DateTime ConsensusTimestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Algorithm { get; set; } = "WeightedAverage";
    public int ConfidenceLevel { get; set; } = 100;
    public string? Notes { get; set; }
    
    // Individual source data for transparency
    public List<ConsensusFeedData> SourceFeeds { get; set; } = new();
}

public class ConsensusFeedData
{
    public Guid Id { get; set; }
    public required Guid ConsensusResultId { get; set; }
    public required string Source { get; set; }
    public decimal Price { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public bool IncludedInConsensus { get; set; } = true;
    public string? ExclusionReason { get; set; }
    
    // Navigation property
    public ConsensusResult ConsensusResult { get; set; } = null!;
}

public class OracleJob
{
    public Guid Id { get; set; }
    public required string JobType { get; set; } // "PriceFeed", "DataValidation", "Consensus"
    public required Guid ConfigurationId { get; set; }
    public OracleJobStatus Status { get; set; } = OracleJobStatus.Pending;
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string Parameters { get; set; } = "{}";
    
    // Navigation property
    public OracleConfiguration Configuration { get; set; } = null!;
}

public class DataValidation
{
    public Guid Id { get; set; }
    public required Guid PriceFeedId { get; set; }
    public required string ValidationType { get; set; } // "Range", "Outlier", "Source", "Freshness"
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public decimal? ExpectedRange { get; set; }
    public decimal? ActualValue { get; set; }
    public DateTime ValidatedAt { get; set; }
    public string ValidatorVersion { get; set; } = "1.0";
    
    // Navigation property
    public PriceFeed PriceFeed { get; set; } = null!;
}

public class OracleSubscription
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string Symbol { get; set; }
    public required string CallbackUrl { get; set; }
    public decimal PriceChangeThreshold { get; set; } = 0.01m; // 1%
    public int MaxCallsPerHour { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public int NotificationCount { get; set; }
    public string? AuthToken { get; set; }
}

public class OracleMetric
{
    public Guid Id { get; set; }
    public required string MetricName { get; set; }
    public required string Symbol { get; set; }
    public decimal Value { get; set; }
    public string Unit { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Tags { get; set; }
}

public enum OracleJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Retrying = 5
}

// Request/Response DTOs
public class PriceFeedRequest
{
    [Required]
    public required string Symbol { get; set; }
    
    public string[] Sources { get; set; } = Array.Empty<string>();
    public bool IncludeConsensus { get; set; } = true;
    public DateTime? FromTime { get; set; }
    public DateTime? ToTime { get; set; }
}

public class PriceFeedResponse
{
    public required string Symbol { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ConsensusPrice { get; set; }
    public decimal Change24h { get; set; }
    public decimal Volume24h { get; set; }
    public DateTime LastUpdated { get; set; }
    public int SourceCount { get; set; }
    public decimal ConfidenceScore { get; set; }
    public List<PriceFeedSource> Sources { get; set; } = new();
}

public class PriceFeedSource
{
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public int ConfidenceScore { get; set; }
    public bool IncludedInConsensus { get; set; }
}

public class CreateConfigurationRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [Required]
    [StringLength(20)]
    public required string Symbol { get; set; }
    
    [Required]
    public required string DataSource { get; set; }
    
    [Required]
    public required string[] ApiEndpoints { get; set; }
    
    [Range(10, 3600)]
    public int UpdateIntervalSeconds { get; set; } = 60;
    
    [Range(0.001, 1.0)]
    public decimal DeviationThreshold { get; set; } = 0.05m;
    
    [Range(1, 10)]
    public int ConsensusThreshold { get; set; } = 3;
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class UpdateConfigurationRequest
{
    [StringLength(100)]
    public string? Name { get; set; }
    
    public string[]? ApiEndpoints { get; set; }
    
    [Range(10, 3600)]
    public int? UpdateIntervalSeconds { get; set; }
    
    [Range(0.001, 1.0)]
    public decimal? DeviationThreshold { get; set; }
    
    [Range(1, 10)]
    public int? ConsensusThreshold { get; set; }
    
    public bool? IsActive { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SubscriptionRequest
{
    [Required]
    [StringLength(100)]
    public required string ClientId { get; set; }
    
    [Required]
    [StringLength(20)]
    public required string Symbol { get; set; }
    
    [Required]
    [Url]
    public required string CallbackUrl { get; set; }
    
    [Range(0.001, 1.0)]
    public decimal PriceChangeThreshold { get; set; } = 0.01m;
    
    [Range(1, 1000)]
    public int MaxCallsPerHour { get; set; } = 100;
    
    public string? AuthToken { get; set; }
}

public class HistoricalDataRequest
{
    [Required]
    public required string Symbol { get; set; }
    
    [Required]
    public DateTime FromTime { get; set; }
    
    [Required]
    public DateTime ToTime { get; set; }
    
    public string[] Sources { get; set; } = Array.Empty<string>();
    public string Granularity { get; set; } = "1h"; // 1m, 5m, 15m, 1h, 4h, 1d
    public bool IncludeRaw { get; set; } = false;
}

public class HistoricalDataResponse
{
    public required string Symbol { get; set; }
    public DateTime FromTime { get; set; }
    public DateTime ToTime { get; set; }
    public string Granularity { get; set; } = "";
    public List<HistoricalDataPoint> Data { get; set; } = new();
}

public class HistoricalDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public int SourceCount { get; set; }
}