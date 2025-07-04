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
    public RiskManagementSettings RiskSettings { get; set; } = new();
    public MLSettings MachineLearning { get; set; } = new();
    public CouncilSelectionCriteria SelectionCriteria { get; set; } = new();
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
    public int ExecutionCount { get; set; }
    public RiskManagementSettings RiskSettings { get; set; } = new();
    public MLSettings MachineLearning { get; set; } = new();
    public CouncilSelectionCriteria SelectionCriteria { get; set; } = new();
    public StrategyPerformanceMetrics Performance { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class StrategyPerformanceMetrics
{
    public double SuccessRate { get; set; }
    public double AverageReturns { get; set; }
    public double RiskAdjustedReturns { get; set; }
    public int TotalExecutions { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
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
    Custom,
    RiskOptimized,
    PerformanceMaximizer,
    DiversificationFocused,
    MLDriven,
    AdaptiveHybrid
}

public enum VotingPriority
{
    Stability,
    Performance,
    Profitability,
    Decentralization,
    Custom
}

// Council Node Models
public class CouncilNodeInfo
{
    public string Address { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsConsensusNode { get; set; }
    public long VotesReceived { get; set; }
    public double UptimePercentage { get; set; }
    public double PerformanceScore { get; set; }
    public decimal CommissionRate { get; set; }
    public DateTime LastActiveTime { get; set; }
    public NodeMetrics Metrics { get; set; } = new();
    public NodeBehaviorProfile BehaviorProfile { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

public class NodeMetrics
{
    public long BlocksProduced { get; set; }
    public long BlocksMissed { get; set; }
    public double ConsensusParticipation { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public decimal TotalRewardsDistributed { get; set; }
    public int VoterCount { get; set; }
    public PerformanceTrend Trend { get; set; } = PerformanceTrend.Stable;
}

public class NodeBehaviorProfile
{
    public double ReliabilityScore { get; set; }
    public double ConsistencyScore { get; set; }
    public double RiskScore { get; set; }
    public DateTime LastAnalyzed { get; set; } = DateTime.UtcNow;
    public BehaviorTrend Trend { get; set; } = BehaviorTrend.Stable;
}

public class NodeBehaviorAnalysis
{
    public string NodeAddress { get; set; } = string.Empty;
    public TimeSpan AnalysisPeriod { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public NodeBehaviorProfile Profile { get; set; } = new();
    public IEnumerable<BehaviorAlert> Alerts { get; set; } = Array.Empty<BehaviorAlert>();
    public Dictionary<string, object> Insights { get; set; } = new();
}

public class NetworkHealthMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TotalNodes { get; set; }
    public int ActiveNodes { get; set; }
    public int ConsensusNodes { get; set; }
    public double OverallUptime { get; set; }
    public double NetworkDecentralization { get; set; }
    public double GeographicDistribution { get; set; }
    public HealthTrend Trend { get; set; } = HealthTrend.Stable;
    public IEnumerable<NetworkHealthAlert> Alerts { get; set; } = Array.Empty<NetworkHealthAlert>();
}

public class NodeMetricsUpdate
{
    public int UptimePercentage { get; set; }
    public int PerformanceScore { get; set; }
    public int BlocksProduced { get; set; }
    public int ConsensusParticipation { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Advanced Strategy Models
public class ExecutionOptions
{
    public bool DryRun { get; set; } = false;
    public bool ValidateOnly { get; set; } = false;
    public double MaxGasPrice { get; set; } = 100.0;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public class MLVotingParameters
{
    public MLModelType ModelType { get; set; } = MLModelType.EnsembleRegressor;
    public string[] Features { get; set; } = Array.Empty<string>();
    public TimeSpan TrainingPeriod { get; set; } = TimeSpan.FromDays(90);
    public double ConfidenceThreshold { get; set; } = 0.8;
}

public class RiskParameters
{
    public RiskTolerance Tolerance { get; set; } = RiskTolerance.Moderate;
    public double MaxSlashingRisk { get; set; } = 0.05;
    public double MaxConcentrationRisk { get; set; } = 0.3;
    public double MaxCorrelationRisk { get; set; } = 0.7;
}

public class DiversificationParameters
{
    public DiversificationStrategy Strategy { get; set; } = DiversificationStrategy.Geographic;
    public int TargetNodeCount { get; set; } = 21;
    public double MaxConcentration { get; set; } = 0.2;
}

public class PerformanceParameters
{
    public TimeSpan EvaluationPeriod { get; set; } = TimeSpan.FromDays(30);
    public double MinPerformanceThreshold { get; set; } = 80.0;
    public bool IncludeTrendAnalysis { get; set; } = true;
}

public class SchedulingOptions
{
    public ScheduleType Type { get; set; } = ScheduleType.Recurring;
    public DateTime? StartTime { get; set; }
    public TimeSpan? Interval { get; set; }
    public string? CronExpression { get; set; }
    public int? MaxExecutions { get; set; }
}

public class ScheduledExecution
{
    public string Id { get; set; } = string.Empty;
    public string StrategyId { get; set; } = string.Empty;
    public SchedulingOptions Options { get; set; } = new();
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
}

public class StrategyPerformanceAnalytics
{
    public string StrategyId { get; set; } = string.Empty;
    public TimeSpan Period { get; set; }
    public int ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageReturns { get; set; }
    public double RiskAdjustedReturns { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class RiskAssessment
{
    public string StrategyId { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    public double OverallRiskScore { get; set; }
    public double SlashingRisk { get; set; }
    public double ConcentrationRisk { get; set; }
    public double VolatilityRisk { get; set; }
    public IEnumerable<RiskFactor> RiskFactors { get; set; } = Array.Empty<RiskFactor>();
}

public class VotingAlert
{
    public string Id { get; set; } = string.Empty;
    public VotingAlertType Type { get; set; }
    public VotingAlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Details { get; set; } = new();
}

// Supporting Types
public class RiskManagementSettings
{
    public double MaxSlashingRisk { get; set; } = 0.05;
    public double MaxConcentrationRisk { get; set; } = 0.3;
    public bool EnableRiskMonitoring { get; set; } = true;
}

public class MLSettings
{
    public bool EnableML { get; set; } = false;
    public MLModelType ModelType { get; set; } = MLModelType.EnsembleRegressor;
    public double ConfidenceThreshold { get; set; } = 0.8;
}

public class CouncilSelectionCriteria
{
    public int MaxCandidates { get; set; } = 21;
    public int MinCandidates { get; set; } = 7;
    public double MinPerformanceScore { get; set; } = 80.0;
    public double MinUptime { get; set; } = 95.0;
}

public class BehaviorAlert
{
    public string NodeAddress { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class NetworkHealthAlert
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public VotingAlertSeverity Severity { get; set; }
}

public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
}

// Additional Enums
public enum PerformanceTrend
{
    Improving,
    Stable,
    Declining
}

public enum BehaviorTrend
{
    Improving,
    Stable,
    Declining,
    Volatile
}

public enum HealthTrend
{
    Improving,
    Stable,
    Declining,
    Critical
}

public enum MLModelType
{
    LinearRegression,
    RandomForest,
    GradientBoosting,
    NeuralNetwork,
    EnsembleRegressor
}

public enum RiskTolerance
{
    Conservative,
    Moderate,
    Aggressive
}

public enum DiversificationStrategy
{
    Geographic,
    Organizational,
    Technical,
    Performance,
    Hybrid
}

public enum ScheduleType
{
    OneTime,
    Recurring,
    ConditionalTrigger
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum VotingAlertType
{
    Performance,
    Risk,
    Opportunity,
    System
}

public enum VotingAlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}
