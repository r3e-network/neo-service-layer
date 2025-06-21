namespace NeoServiceLayer.AI.PatternRecognition.Models;

/// <summary>
/// Represents a behavior profile for an entity.
/// </summary>
public class BehaviorProfile
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction frequency.
    /// </summary>
    public double TransactionFrequency { get; set; }

    /// <summary>
    /// Gets or sets the average transaction amount.
    /// </summary>
    public decimal AverageTransactionAmount { get; set; }

    /// <summary>
    /// Gets or sets whether there are unusual time patterns.
    /// </summary>
    public bool UnusualTimePatterns { get; set; }

    /// <summary>
    /// Gets or sets whether there are suspicious address interactions.
    /// </summary>
    public bool SuspiciousAddressInteractions { get; set; }

    /// <summary>
    /// Gets or sets the behavior characteristics.
    /// </summary>
    public BehaviorCharacteristics Characteristics { get; set; } = new();

    /// <summary>
    /// Gets or sets the normality score (0-1, higher is more normal).
    /// </summary>
    public double NormalityScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets when the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the address being analyzed.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction time patterns.
    /// </summary>
    public List<TimePattern> TransactionTimePatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the address interactions.
    /// </summary>
    public Dictionary<string, int> AddressInteractions { get; set; } = new();

    /// <summary>
    /// Gets or sets the analyzed period.
    /// </summary>
    public TimeRange AnalyzedPeriod { get; set; } = new();

    /// <summary>
    /// Gets or sets when the profile was generated.
    /// </summary>
    public DateTime ProfileGeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the profile was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the transaction patterns.
    /// </summary>
    public Dictionary<string, object> TransactionPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk tolerance.
    /// </summary>
    public double RiskTolerance { get; set; }

    /// <summary>
    /// Gets or sets the behavior metrics.
    /// </summary>
    public Dictionary<string, double> BehaviorMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the typical time pattern.
    /// </summary>
    public string TypicalTimePattern { get; set; } = string.Empty;
}

/// <summary>
/// Represents behavior characteristics.
/// </summary>
public class BehaviorCharacteristics
{
    /// <summary>
    /// Gets or sets whether the entity is a high frequency trader.
    /// </summary>
    public bool IsHighFrequencyTrader { get; set; }

    /// <summary>
    /// Gets or sets whether the entity has regular patterns.
    /// </summary>
    public bool HasRegularPattern { get; set; }

    /// <summary>
    /// Gets or sets whether the entity shows mixing behavior.
    /// </summary>
    public bool ShowsMixingBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether the entity has unusual geographic patterns.
    /// </summary>
    public bool HasUnusualGeographicPatterns { get; set; }

    /// <summary>
    /// Gets or sets the activity level.
    /// </summary>
    public ActivityLevel ActivityLevel { get; set; }

    /// <summary>
    /// Gets or sets the transaction patterns.
    /// </summary>
    public List<TransactionPattern> TransactionPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the time patterns.
    /// </summary>
    public List<TimePattern> TimePatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the network patterns.
    /// </summary>
    public List<NetworkPattern> NetworkPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets additional characteristics.
    /// </summary>
    public Dictionary<string, object> AdditionalCharacteristics { get; set; } = new();
}

/// <summary>
/// Represents a transaction pattern.
/// </summary>
public class TransactionPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the frequency of this pattern.
    /// </summary>
    public double Frequency { get; set; }

    /// <summary>
    /// Gets or sets the confidence in this pattern.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the pattern parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets when the pattern was first observed.
    /// </summary>
    public DateTime FirstObserved { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the pattern was last observed.
    /// </summary>
    public DateTime LastObserved { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a time pattern.
/// </summary>
public class TimePattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time period type.
    /// </summary>
    public TimePeriodType PeriodType { get; set; }

    /// <summary>
    /// Gets or sets the activity distribution.
    /// </summary>
    public Dictionary<string, double> ActivityDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets the peak activity times.
    /// </summary>
    public List<TimeSpan> PeakActivityTimes { get; set; } = new();

    /// <summary>
    /// Gets or sets the regularity score (0-1).
    /// </summary>
    public double RegularityScore { get; set; }

    /// <summary>
    /// Gets or sets the confidence in this pattern.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the suspicious hours.
    /// </summary>
    public int[] SuspiciousHours { get; set; } = Array.Empty<int>();
}

/// <summary>
/// Represents a network pattern.
/// </summary>
public class NetworkPattern
{
    /// <summary>
    /// Gets or sets the pattern ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the network type.
    /// </summary>
    public NetworkType NetworkType { get; set; }

    /// <summary>
    /// Gets or sets the connected addresses.
    /// </summary>
    public List<string> ConnectedAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets the connection strength scores.
    /// </summary>
    public Dictionary<string, double> ConnectionStrengths { get; set; } = new();

    /// <summary>
    /// Gets or sets the centrality measures.
    /// </summary>
    public Dictionary<string, double> CentralityMeasures { get; set; } = new();

    /// <summary>
    /// Gets or sets the clustering coefficient.
    /// </summary>
    public double ClusteringCoefficient { get; set; }

    /// <summary>
    /// Gets or sets the confidence in this pattern.
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Represents activity levels.
/// </summary>
public enum ActivityLevel
{
    /// <summary>
    /// Very low activity.
    /// </summary>
    VeryLow,

    /// <summary>
    /// Low activity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium activity.
    /// </summary>
    Medium,

    /// <summary>
    /// High activity.
    /// </summary>
    High,

    /// <summary>
    /// Very high activity.
    /// </summary>
    VeryHigh
}

/// <summary>
/// Represents time period types.
/// </summary>
public enum TimePeriodType
{
    /// <summary>
    /// Hourly patterns.
    /// </summary>
    Hourly,

    /// <summary>
    /// Daily patterns.
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly patterns.
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly patterns.
    /// </summary>
    Monthly,

    /// <summary>
    /// Seasonal patterns.
    /// </summary>
    Seasonal
}

/// <summary>
/// Represents network types.
/// </summary>
public enum NetworkType
{
    /// <summary>
    /// Direct connections.
    /// </summary>
    Direct,

    /// <summary>
    /// Indirect connections.
    /// </summary>
    Indirect,

    /// <summary>
    /// Cluster connections.
    /// </summary>
    Cluster,

    /// <summary>
    /// Hub connections.
    /// </summary>
    Hub,

    /// <summary>
    /// Bridge connections.
    /// </summary>
    Bridge
}
