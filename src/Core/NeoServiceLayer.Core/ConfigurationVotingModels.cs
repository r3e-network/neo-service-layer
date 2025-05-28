namespace NeoServiceLayer.Core;

// Configuration Service Models
public class ConfigurationSetRequest
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConfigurationGetRequest
{
    public string Key { get; set; } = string.Empty;
    public bool DecryptValue { get; set; } = true;
    public string? DefaultValue { get; set; }
}

public class ConfigurationItem
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConfigurationValue
{
    public object Value { get; set; } = new();
    public ConfigurationDataType DataType { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
}

public class ConfigurationInfo
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConfigurationDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public object DefaultValue { get; set; } = new();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class ConfigurationListRequest
{
    public string? KeyPattern { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IncludeValues { get; set; } = true;
    public bool IncludeEncrypted { get; set; } = false;
    public int PageSize { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
}

public class ConfigurationSubscriptionRequest
{
    public string KeyPattern { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string CallbackUrl { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConfigurationSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string KeyPattern { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string CallbackUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Voting Service Models
public class VotingStrategyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public VotingStrategyType StrategyType { get; set; }
    public VotingRules Rules { get; set; } = new();
    public string[] PreferredCandidates { get; set; } = Array.Empty<string>();
    public string[] FallbackCandidates { get; set; } = Array.Empty<string>();
    public bool AutoExecute { get; set; }
    public TimeSpan ExecutionInterval { get; set; } = TimeSpan.FromHours(24);
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class VotingStrategyUpdate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public VotingRules? Rules { get; set; }
    public string[]? PreferredCandidates { get; set; }
    public string[]? FallbackCandidates { get; set; }
    public bool? AutoExecute { get; set; }
    public TimeSpan? ExecutionInterval { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class VotingStrategy
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerAddress { get; set; } = string.Empty;
    public VotingStrategyType StrategyType { get; set; }
    public VotingRules Rules { get; set; } = new();
    public string[] PreferredCandidates { get; set; } = Array.Empty<string>();
    public string[] FallbackCandidates { get; set; } = Array.Empty<string>();
    public bool AutoExecute { get; set; }
    public TimeSpan ExecutionInterval { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastExecuted { get; set; }
    public DateTime? NextExecution { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class VotingRules
{
    public bool OnlyActiveNodes { get; set; } = true;
    public bool OnlyConsensusNodes { get; set; } = true;
    public int MaxCandidates { get; set; } = 21;
    public double MinUptimePercentage { get; set; } = 95.0;
    public bool VoteForBestProfit { get; set; }
    public bool UseConditionalVoting { get; set; }
    public VotingPriority Priority { get; set; } = VotingPriority.Stability;
    public Dictionary<string, object> CustomRules { get; set; } = new();
}

public class CandidateInfo
{
    public string Address { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsConsensusNode { get; set; }
    public int Rank { get; set; }
    public long VotesReceived { get; set; }
    public double UptimePercentage { get; set; }
    public decimal ExpectedReward { get; set; }
    public decimal CommissionRate { get; set; }
    public DateTime LastActiveTime { get; set; }
    public CandidateMetrics Metrics { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

public class CandidateMetrics
{
    public long BlocksProduced { get; set; }
    public long BlocksMissed { get; set; }
    public double PerformanceScore { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public decimal TotalRewardsDistributed { get; set; }
    public int VoterCount { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

public class VotingResult
{
    public string ExecutionId { get; set; } = string.Empty;
    public string StrategyId { get; set; } = string.Empty;
    public string VoterAddress { get; set; } = string.Empty;
    public string[] SelectedCandidates { get; set; } = Array.Empty<string>();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? TransactionHash { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> ExecutionDetails { get; set; } = new();
}

public class VotingPreferences
{
    public VotingPriority Priority { get; set; } = VotingPriority.Stability;
    public bool PreferActiveNodes { get; set; } = true;
    public bool PreferConsensusNodes { get; set; } = true;
    public double MinUptimePercentage { get; set; } = 95.0;
    public bool ConsiderProfitability { get; set; } = false;
    public string[] PreferredCandidates { get; set; } = Array.Empty<string>();
    public string[] ExcludedCandidates { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> CustomPreferences { get; set; } = new();
}

public class VotingRecommendation
{
    public string[] RecommendedCandidates { get; set; } = Array.Empty<string>();
    public string RecommendationReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AnalysisDetails { get; set; } = new();
}

// Configuration Enums
public enum ConfigurationDataType
{
    String,
    Integer,
    Decimal,
    Boolean,
    Json,
    Binary,
    DateTime,
    Array
}

// Voting Enums
public enum VotingStrategyType
{
    Manual,
    Automatic,
    Conditional,
    ProfitOptimized,
    StabilityFocused,
    Custom
}

public enum VotingPriority
{
    Stability,
    Performance,
    Profitability,
    Decentralization,
    Custom
}
