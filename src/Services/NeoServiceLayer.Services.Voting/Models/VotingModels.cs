using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting.Models;

/// <summary>
/// Voting risk assessment result.
/// </summary>
public class VotingRiskAssessment
{
    /// <summary>
    /// Gets or sets the overall risk score (0-1).
    /// </summary>
    public double OverallRisk { get; set; }

    /// <summary>
    /// Gets or sets the concentration risk score (0-1).
    /// </summary>
    public double ConcentrationRisk { get; set; }

    /// <summary>
    /// Gets or sets the performance risk score (0-1).
    /// </summary>
    public double PerformanceRisk { get; set; }

    /// <summary>
    /// Gets or sets the reward risk score (0-1).
    /// </summary>
    public double RewardRisk { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public string[] RiskFactors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets detailed risk metrics.
    /// </summary>
    public Dictionary<string, double> DetailedRisks { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel => OverallRisk switch
    {
        >= 0.8 => RiskLevel.Critical,
        >= 0.6 => RiskLevel.High,
        >= 0.4 => RiskLevel.Medium,
        >= 0.2 => RiskLevel.Low,
        _ => RiskLevel.Minimal
    };

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a voting strategy for node selection and consensus decisions.
/// </summary>
public class VotingStrategy
{
    /// <summary>
    /// Gets or sets the unique identifier for this strategy.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the voting strategy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of voting strategy.
    /// </summary>
    public VotingStrategyType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum number of votes required.
    /// </summary>
    public int MinimumVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the voting threshold percentage.
    /// </summary>
    public double ThresholdPercentage { get; set; } = 0.51;
    
    /// <summary>
    /// Gets or sets the voting timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
    
    /// <summary>
    /// Gets or sets the weighted voting configuration.
    /// </summary>
    public Dictionary<string, double> VoterWeights { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the strategy parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when this strategy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when this strategy was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the description of the voting strategy.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this strategy is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the owner address for this strategy.
    /// </summary>
    public string OwnerAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when this strategy was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the voting rules for this strategy.
    /// </summary>
    public VotingRules Rules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the preferred candidates for conditional strategies.
    /// </summary>
    public List<string> PreferredCandidates { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the fallback candidates for conditional strategies.
    /// </summary>
    public List<string> FallbackCandidates { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the strategy type (alias for Type).
    /// </summary>
    public VotingStrategyType StrategyType 
    { 
        get => Type; 
        set => Type = value; 
    }
    
    /// <summary>
    /// Gets or sets when this strategy was last executed.
    /// </summary>
    public DateTime? LastExecuted { get; set; }
    
    /// <summary>
    /// Gets or sets the number of times this strategy has been executed.
    /// </summary>
    public int ExecutionCount { get; set; }
    
    /// <summary>
    /// Gets or sets whether this strategy should auto-execute.
    /// </summary>
    public bool AutoExecute { get; set; }
    
    /// <summary>
    /// Gets or sets when this strategy should next execute.
    /// </summary>
    public DateTime? NextExecution { get; set; }
    
    /// <summary>
    /// Gets or sets the execution interval for auto-execution.
    /// </summary>
    public TimeSpan ExecutionInterval { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Represents the type of voting strategy.
/// </summary>
public enum VotingStrategyType
{
    /// <summary>
    /// Simple majority voting.
    /// </summary>
    SimpleMajority,
    
    /// <summary>
    /// Supermajority voting (requires 2/3 or more).
    /// </summary>
    SuperMajority,
    
    /// <summary>
    /// Unanimous voting (requires all votes).
    /// </summary>
    Unanimous,
    
    /// <summary>
    /// Weighted voting based on stake or reputation.
    /// </summary>
    Weighted,
    
    /// <summary>
    /// Ranked choice voting.
    /// </summary>
    RankedChoice,
    
    /// <summary>
    /// Delegated voting through representatives.
    /// </summary>
    Delegated,
    
    /// <summary>
    /// Automatic voting based on predefined rules.
    /// </summary>
    Automatic,
    
    /// <summary>
    /// Stability-focused voting strategy.
    /// </summary>
    StabilityFocused,
    
    /// <summary>
    /// Conditional voting based on criteria.
    /// </summary>
    Conditional
}

/// <summary>
/// Represents a request to create or update a voting strategy.
/// </summary>
public class VotingStrategyRequest
{
    /// <summary>
    /// Gets or sets the name of the voting strategy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of voting strategy.
    /// </summary>
    public VotingStrategyType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum number of votes required.
    /// </summary>
    public int MinimumVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the voting threshold percentage.
    /// </summary>
    public double ThresholdPercentage { get; set; } = 0.51;
    
    /// <summary>
    /// Gets or sets the voting timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
    
    /// <summary>
    /// Gets or sets the weighted voting configuration.
    /// </summary>
    public Dictionary<string, double>? VoterWeights { get; set; }
    
    /// <summary>
    /// Gets or sets the strategy parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the preferred candidates for conditional strategies.
    /// </summary>
    public List<string>? PreferredCandidates { get; set; }

    /// <summary>
    /// Gets or sets the fallback candidates for conditional strategies.
    /// </summary>
    public List<string>? FallbackCandidates { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of candidates to select.
    /// </summary>
    public int MaxCandidates { get; set; } = 21;

    /// <summary>
    /// Gets or sets whether the strategy should be enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the voting rules for this strategy.
    /// </summary>
    public VotingRules Rules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the description of the voting strategy.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the owner address for this strategy.
    /// </summary>
    public string? OwnerAddress { get; set; }
    
    // Additional properties for compatibility
    /// <summary>
    /// Gets or sets the strategy type alias.
    /// </summary>
    public VotingStrategyType StrategyType 
    { 
        get => Type; 
        set => Type = value; 
    }
    
    /// <summary>
    /// Gets or sets when the strategy was last executed.
    /// </summary>
    public DateTime LastExecuted { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the execution count.
    /// </summary>
    public int ExecutionCount { get; set; }
    
    /// <summary>
    /// Gets or sets whether to auto-execute this strategy.
    /// </summary>
    public bool AutoExecute { get; set; }
    
    /// <summary>
    /// Gets or sets the next scheduled execution time.
    /// </summary>
    public DateTime? NextExecution { get; set; }
    
    /// <summary>
    /// Gets or sets the execution interval.
    /// </summary>
    public TimeSpan ExecutionInterval { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Represents the result of a voting process.
/// </summary>
public class VotingResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this voting result.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the proposal or motion that was voted on.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the vote passed.
    /// </summary>
    public bool Passed { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of votes cast.
    /// </summary>
    public int TotalVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of yes votes.
    /// </summary>
    public int YesVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of no votes.
    /// </summary>
    public int NoVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of abstain votes.
    /// </summary>
    public int AbstainVotes { get; set; }
    
    /// <summary>
    /// Gets or sets the final vote percentage.
    /// </summary>
    public double VotePercentage { get; set; }
    
    /// <summary>
    /// Gets or sets when the voting concluded.
    /// </summary>
    public DateTime ConcludedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets detailed voting breakdown.
    /// </summary>
    public Dictionary<string, object> VoteBreakdown { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional metadata about the voting result.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Additional properties for compatibility with execution results
    /// <summary>
    /// Gets or sets the execution identifier.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the strategy identifier.
    /// </summary>
    public string StrategyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the voter address.
    /// </summary>
    public string VoterAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when the vote was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets whether the execution was successful.
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the selected candidates.
    /// </summary>
    public string[] SelectedCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets execution details.
    /// </summary>
    public Dictionary<string, object> ExecutionDetails { get; set; } = new();
    
    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a recommendation based on voting analysis.
/// </summary>
public class VotingRecommendation
{
    /// <summary>
    /// Gets or sets the unique identifier for this recommendation.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of recommendation.
    /// </summary>
    public RecommendationType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the recommendation text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the confidence level of the recommendation.
    /// </summary>
    public double Confidence { get; set; }
    
    /// <summary>
    /// Gets or sets the rationale behind the recommendation.
    /// </summary>
    public string Rationale { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the priority level.
    /// </summary>
    public Priority Priority { get; set; }
    
    /// <summary>
    /// Gets or sets when this recommendation was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets supporting data for the recommendation.
    /// </summary>
    public Dictionary<string, object> SupportingData { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommended candidates.
    /// </summary>
    public string[] RecommendedCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the recommendation reason.
    /// </summary>
    public string RecommendationReason { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public double ConfidenceScore { get; set; }
    
    /// <summary>
    /// Gets or sets the analysis details.
    /// </summary>
    public Dictionary<string, object> AnalysisDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk assessment.
    /// </summary>
    public VotingRiskAssessment? RiskAssessment { get; set; }

    /// <summary>
    /// Gets or sets performance metrics for the recommendation.
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Represents user voting preferences.
/// </summary>
public class VotingPreferences
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the preferred voting strategy.
    /// </summary>
    public VotingStrategyType PreferredStrategy { get; set; }
    
    /// <summary>
    /// Gets or sets whether to enable automatic voting.
    /// </summary>
    public bool AutoVotingEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets notification preferences.
    /// </summary>
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    
    /// <summary>
    /// Gets or sets delegation settings.
    /// </summary>
    public DelegationSettings DelegationSettings { get; set; } = new();
    
    /// <summary>
    /// Gets or sets privacy settings for voting.
    /// </summary>
    public VotingPrivacySettings PrivacySettings { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the voting priority.
    /// </summary>
    public VotingPriority Priority { get; set; } = VotingPriority.Medium;
    
    /// <summary>
    /// Gets or sets whether to prefer consensus nodes.
    /// </summary>
    public bool PreferConsensusNodes { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the minimum uptime percentage required.
    /// </summary>
    public double MinUptimePercentage { get; set; } = 95.0;
    
    /// <summary>
    /// Gets or sets the excluded candidates.
    /// </summary>
    public string[] ExcludedCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets the preferred candidates.
    /// </summary>
    public string[] PreferredCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets whether to consider profitability.
    /// </summary>
    public bool ConsiderProfitability { get; set; } = true;
}

/// <summary>
/// Represents notification preferences for voting.
/// </summary>
public class NotificationPreferences
{
    /// <summary>
    /// Gets or sets whether to receive email notifications.
    /// </summary>
    public bool EmailNotifications { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to receive push notifications.
    /// </summary>
    public bool PushNotifications { get; set; } = true;
    
    /// <summary>
    /// Gets or sets notification frequency.
    /// </summary>
    public NotificationFrequency Frequency { get; set; } = NotificationFrequency.Immediate;
}

/// <summary>
/// Represents delegation settings for voting.
/// </summary>
public class DelegationSettings
{
    /// <summary>
    /// Gets or sets whether delegation is enabled.
    /// </summary>
    public bool DelegationEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the default delegate.
    /// </summary>
    public string? DefaultDelegate { get; set; }
    
    /// <summary>
    /// Gets or sets topic-specific delegates.
    /// </summary>
    public Dictionary<string, string> TopicDelegates { get; set; } = new();
}

/// <summary>
/// Represents privacy settings for voting.
/// </summary>
public class VotingPrivacySettings
{
    /// <summary>
    /// Gets or sets whether votes should be anonymous.
    /// </summary>
    public bool AnonymousVoting { get; set; }
    
    /// <summary>
    /// Gets or sets whether to hide voting history.
    /// </summary>
    public bool HideVotingHistory { get; set; }
    
    /// <summary>
    /// Gets or sets data retention period in days.
    /// </summary>
    public int DataRetentionDays { get; set; } = 365;
}

/// <summary>
/// Represents the type of recommendation.
/// </summary>
public enum RecommendationType
{
    /// <summary>
    /// Strategy optimization recommendation.
    /// </summary>
    StrategyOptimization,
    
    /// <summary>
    /// Participation improvement recommendation.
    /// </summary>
    ParticipationImprovement,
    
    /// <summary>
    /// Security enhancement recommendation.
    /// </summary>
    SecurityEnhancement,
    
    /// <summary>
    /// Performance improvement recommendation.
    /// </summary>
    PerformanceImprovement
}

/// <summary>
/// Represents priority levels.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium priority.
    /// </summary>
    Medium,
    
    /// <summary>
    /// High priority.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical
}

/// <summary>
/// Represents notification frequency options.
/// </summary>
public enum NotificationFrequency
{
    /// <summary>
    /// Immediate notifications.
    /// </summary>
    Immediate,
    
    /// <summary>
    /// Daily digest.
    /// </summary>
    Daily,
    
    /// <summary>
    /// Weekly digest.
    /// </summary>
    Weekly,
    
    /// <summary>
    /// Only critical notifications.
    /// </summary>
    CriticalOnly
}

/// <summary>
/// Represents risk levels.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Minimal risk.
    /// </summary>
    Minimal,
    
    /// <summary>
    /// Low risk.
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium risk.
    /// </summary>
    Medium,
    
    /// <summary>
    /// High risk.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical risk.
    /// </summary>
    Critical
}

/// <summary>
/// Execution options for voting operations.
/// </summary>
public class ExecutionOptions
{
    /// <summary>
    /// Gets or sets whether this is a dry run.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum execution time in seconds.
    /// </summary>
    public int MaxExecutionTimeSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to validate before execution.
    /// </summary>
    public bool ValidateBeforeExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets additional execution parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Voting strategy update request.
/// </summary>
public class VotingStrategyUpdate
{
    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the threshold percentage.
    /// </summary>
    public double? ThresholdPercentage { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether the strategy is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the voting strategy.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Council node information.
/// </summary>
public class CouncilNodeInfo
{
    /// <summary>
    /// Gets or sets the node public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node status.
    /// </summary>
    public NodeStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the performance metrics.
    /// </summary>
    public NodePerformanceMetrics Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the voting power.
    /// </summary>
    public decimal VotingPower { get; set; }
}

/// <summary>
/// Node behavior analysis.
/// </summary>
public class NodeBehaviorAnalysis
{
    /// <summary>
    /// Gets or sets the node address.
    /// </summary>
    public string NodeAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the behavior score.
    /// </summary>
    public double BehaviorScore { get; set; }

    /// <summary>
    /// Gets or sets the behavior patterns.
    /// </summary>
    public List<string> BehaviorPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets anomaly indicators.
    /// </summary>
    public List<string> AnomalyIndicators { get; set; } = new();
}

/// <summary>
/// Network health metrics.
/// </summary>
public class NetworkHealthMetrics
{
    /// <summary>
    /// Gets or sets the overall health score.
    /// </summary>
    public double HealthScore { get; set; }

    /// <summary>
    /// Gets or sets the number of active nodes.
    /// </summary>
    public int ActiveNodes { get; set; }

    /// <summary>
    /// Gets or sets the consensus participation rate.
    /// </summary>
    public double ConsensusParticipationRate { get; set; }

    /// <summary>
    /// Gets or sets the network latency metrics.
    /// </summary>
    public Dictionary<string, double> LatencyMetrics { get; set; } = new();
}

/// <summary>
/// Node metrics update.
/// </summary>
public class NodeMetricsUpdate
{
    /// <summary>
    /// Gets or sets the node address.
    /// </summary>
    public string NodeAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated metrics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdateTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ML voting parameters.
/// </summary>
public class MLVotingParameters
{
    /// <summary>
    /// Gets or sets the algorithm type.
    /// </summary>
    public string AlgorithmType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the training data parameters.
    /// </summary>
    public Dictionary<string, object> TrainingParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the prediction confidence threshold.
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;
}

/// <summary>
/// Risk parameters for voting.
/// </summary>
public class RiskParameters
{
    /// <summary>
    /// Gets or sets the maximum risk tolerance.
    /// </summary>
    public double MaxRiskTolerance { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the risk factors to consider.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the risk assessment parameters.
    /// </summary>
    public Dictionary<string, double> AssessmentParameters { get; set; } = new();
}

/// <summary>
/// Diversification parameters.
/// </summary>
public class DiversificationParameters
{
    /// <summary>
    /// Gets or sets the minimum number of nodes.
    /// </summary>
    public int MinimumNodes { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum concentration percentage.
    /// </summary>
    public double MaxConcentrationPercentage { get; set; } = 0.3;
    
    /// <summary>
    /// Gets or sets the maximum concentration.
    /// </summary>
    public double MaxConcentration { get; set; } = 0.3;
    
    /// <summary>
    /// Gets or sets the target node count.
    /// </summary>
    public int TargetNodeCount { get; set; } = 21;
    
    /// <summary>
    /// Gets or sets the diversification strategy.
    /// </summary>
    public string Strategy { get; set; } = "balanced";

    /// <summary>
    /// Gets or sets diversification strategies.
    /// </summary>
    public List<string> Strategies { get; set; } = new();
}

/// <summary>
/// Performance parameters.
/// </summary>
public class PerformanceParameters
{
    /// <summary>
    /// Gets or sets the minimum performance threshold.
    /// </summary>
    public double MinimumPerformanceThreshold { get; set; } = 0.8;
    
    /// <summary>
    /// Gets or sets the minimum performance threshold.
    /// </summary>
    public double MinPerformanceThreshold { get; set; } = 0.8;
    
    /// <summary>
    /// Gets or sets the evaluation period.
    /// </summary>
    public TimeSpan EvaluationPeriod { get; set; } = TimeSpan.FromDays(7);
    
    /// <summary>
    /// Gets or sets whether to include trend analysis.
    /// </summary>
    public bool IncludeTrendAnalysis { get; set; } = true;

    /// <summary>
    /// Gets or sets the performance metrics to consider.
    /// </summary>
    public List<string> MetricsToConsider { get; set; } = new();

    /// <summary>
    /// Gets or sets the weight for each metric.
    /// </summary>
    public Dictionary<string, double> MetricWeights { get; set; } = new();
}

/// <summary>
/// Node performance metrics.
/// </summary>
public class NodePerformanceMetrics
{
    /// <summary>
    /// Gets or sets the uptime percentage.
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the block production rate.
    /// </summary>
    public double BlockProductionRate { get; set; }

    /// <summary>
    /// Gets or sets additional metrics.
    /// </summary>
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Node status enumeration.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Node is active.
    /// </summary>
    Active,

    /// <summary>
    /// Node is inactive.
    /// </summary>
    Inactive,

    /// <summary>
    /// Node is under maintenance.
    /// </summary>
    Maintenance,

    /// <summary>
    /// Node is suspended.
    /// </summary>
    Suspended
}

/// <summary>
/// Scheduling options for voting operations.
/// </summary>
public class SchedulingOptions
{
    /// <summary>
    /// Gets or sets the schedule expression (cron format).
    /// </summary>
    public string ScheduleExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to retry on failure.
    /// </summary>
    public bool RetryOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Scheduled execution information.
/// </summary>
public class ScheduledExecution
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strategy ID.
    /// </summary>
    public string StrategyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduled time.
    /// </summary>
    public DateTime ScheduledTime { get; set; }

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the next execution time.
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }
}

/// <summary>
/// Strategy performance analytics.
/// </summary>
public class StrategyPerformanceAnalytics
{
    /// <summary>
    /// Gets or sets the strategy ID.
    /// </summary>
    public string StrategyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the success rate.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average execution time.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the total executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the performance metrics.
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Risk assessment result.
/// </summary>
public class RiskAssessment
{
    /// <summary>
    /// Gets or sets the overall risk score.
    /// </summary>
    public double OverallRiskScore { get; set; }

    /// <summary>
    /// Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the risk factors.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Gets or sets the recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the assessment timestamp.
    /// </summary>
    public DateTime AssessmentTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Execution status enumeration.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// Pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Failed execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Cancelled execution.
    /// </summary>
    Cancelled
}

/// <summary>
/// Voting rules configuration.
/// </summary>
public class VotingRules
{
    /// <summary>
    /// Gets or sets the minimum quorum required.
    /// </summary>
    public double MinimumQuorum { get; set; } = 0.51;
    
    /// <summary>
    /// Gets or sets the required quorum.
    /// </summary>
    public decimal RequiredQuorum { get; set; } = 0.51m;

    /// <summary>
    /// Gets or sets the approval threshold.
    /// </summary>
    public double ApprovalThreshold { get; set; } = 0.51;
    
    /// <summary>
    /// Gets or sets the super majority threshold.
    /// </summary>
    public decimal SuperMajorityThreshold { get; set; } = 66.67m;
    
    /// <summary>
    /// Gets or sets the minimum vote weight.
    /// </summary>
    public decimal MinimumVoteWeight { get; set; } = 1.0m;
    
    /// <summary>
    /// Gets or sets whether vote changes are allowed.
    /// </summary>
    public bool AllowVoteChange { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether vote withdrawal is allowed.
    /// </summary>
    public bool AllowVoteWithdrawal { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the minimum uptime percentage.
    /// </summary>
    public double MinUptimePercentage { get; set; } = 95.0;

    /// <summary>
    /// Gets or sets the voting duration.
    /// </summary>
    public TimeSpan VotingDuration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets whether delegation is allowed.
    /// </summary>
    public bool AllowDelegation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether vote changes are allowed.
    /// </summary>
    public bool AllowVoteChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets additional rule parameters.
    /// </summary>
    public Dictionary<string, object> AdditionalRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether to vote for best profit candidates.
    /// </summary>
    public bool VoteForBestProfit { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the maximum number of candidates to select.
    /// </summary>
    public int MaxCandidates { get; set; } = 21;
    
    /// <summary>
    /// Gets or sets whether to only include active nodes.
    /// </summary>
    public bool OnlyActiveNodes { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to only include consensus nodes.
    /// </summary>
    public bool OnlyConsensusNodes { get; set; } = false;
}

/// <summary>
/// Voting alert information.
/// </summary>
public class VotingAlert
{
    /// <summary>
    /// Gets or sets the alert ID.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the alert is acknowledged.
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;

    /// <summary>
    /// Gets or sets additional alert data.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Alert type enumeration.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Performance alert.
    /// </summary>
    Performance,

    /// <summary>
    /// Security alert.
    /// </summary>
    Security,

    /// <summary>
    /// System alert.
    /// </summary>
    System,

    /// <summary>
    /// Voting alert.
    /// </summary>
    Voting,

    /// <summary>
    /// Node alert.
    /// </summary>
    Node
}

/// <summary>
/// Alert severity enumeration.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Low severity.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical
}

/// <summary>
/// Voting priority enumeration.
/// </summary>
public enum VotingPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low,

    /// <summary>
    /// Medium priority.
    /// </summary>
    Medium,

    /// <summary>
    /// High priority.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical,
    
    /// <summary>
    /// Stability-focused priority.
    /// </summary>
    Stability,
    
    /// <summary>
    /// Performance-focused priority.
    /// </summary>
    Performance,
    
    /// <summary>
    /// Profitability-focused priority.
    /// </summary>
    Profitability,
    
    /// <summary>
    /// Decentralization-focused priority.
    /// </summary>
    Decentralization,
    
    /// <summary>
    /// Custom priority based on user preferences.
    /// </summary>
    Custom
}

/// <summary>
/// Request to create a voting strategy.
/// </summary>
public class CreateVotingStrategyRequest
{
    /// <summary>
    /// Gets or sets the name of the strategy.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the strategy.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the owner address.
    /// </summary>
    public string OwnerAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of voting strategy.
    /// </summary>
    public VotingStrategyType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the voting rules.
    /// </summary>
    public VotingRules Rules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request to update a voting strategy.
/// </summary>
public class UpdateVotingStrategyRequest
{
    /// <summary>
    /// Gets or sets the updated name of the strategy.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the updated description of the strategy.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the updated type of voting strategy.
    /// </summary>
    public VotingStrategyType? Type { get; set; }
    
    /// <summary>
    /// Gets or sets the updated voting rules.
    /// </summary>
    public VotingRules? Rules { get; set; }
    
    /// <summary>
    /// Gets or sets whether the strategy is active.
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets updated parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}
