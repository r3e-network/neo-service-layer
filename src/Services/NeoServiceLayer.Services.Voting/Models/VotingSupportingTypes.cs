using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting.Models;

/// <summary>
/// Represents a date range for filtering.
/// </summary>
public class DateRange
{
    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets whether the range is valid.
    /// </summary>
    public bool IsValid => StartDate <= EndDate;
}

/// <summary>
/// Represents a voting proposal.
/// </summary>
public class Proposal
{
    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal category.
    /// </summary>
    public ProposalCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the proposal status.
    /// </summary>
    public ProposalStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the proposer's identifier.
    /// </summary>
    public string ProposerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voting start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the voting end time.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the minimum quorum required.
    /// </summary>
    public double MinimumQuorum { get; set; }

    /// <summary>
    /// Gets or sets the approval threshold percentage.
    /// </summary>
    public double ApprovalThreshold { get; set; }

    /// <summary>
    /// Gets or sets the voting options.
    /// </summary>
    public List<VotingOption> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets whether votes are public.
    /// </summary>
    public bool IsPublicVoting { get; set; }

    /// <summary>
    /// Gets or sets whether delegation is allowed.
    /// </summary>
    public bool AllowDelegation { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the total votes cast.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Gets or sets the total voting power used.
    /// </summary>
    public double TotalVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the current participation rate.
    /// </summary>
    public double ParticipationRate { get; set; }

    /// <summary>
    /// Gets or sets the proposal metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a voting option within a proposal.
/// </summary>
public class VotingOption
{
    /// <summary>
    /// Gets or sets the option identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option text or description.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option order for display.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the vote count for this option.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the total voting power for this option.
    /// </summary>
    public double VotingPower { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total votes.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Gets or sets whether this option is the current leader.
    /// </summary>
    public bool IsLeading { get; set; }
}

/// <summary>
/// Represents a cast vote.
/// </summary>
public class Vote
{
    /// <summary>
    /// Gets or sets the vote identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected option identifier.
    /// </summary>
    public string OptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voter's identifier.
    /// </summary>
    public string VoterIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voting power used.
    /// </summary>
    public double VotingPower { get; set; }

    /// <summary>
    /// Gets or sets the vote timestamp.
    /// </summary>
    public DateTime VoteTimestamp { get; set; }

    /// <summary>
    /// Gets or sets whether this is a proxy vote.
    /// </summary>
    public bool IsProxyVote { get; set; }

    /// <summary>
    /// Gets or sets the proxy voter identifier if applicable.
    /// </summary>
    public string? ProxyVoterIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the vote signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets optional voting comment.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets additional vote metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents voting results for a proposal.
/// </summary>
public class VotingResults
{
    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the results by option.
    /// </summary>
    public List<OptionResult> OptionResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the winning option identifier.
    /// </summary>
    public string? WinningOptionId { get; set; }

    /// <summary>
    /// Gets or sets the total votes cast.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Gets or sets the total voting power used.
    /// </summary>
    public double TotalVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the participation rate.
    /// </summary>
    public double ParticipationRate { get; set; }

    /// <summary>
    /// Gets or sets whether quorum was reached.
    /// </summary>
    public bool QuorumReached { get; set; }

    /// <summary>
    /// Gets or sets whether the proposal passed.
    /// </summary>
    public bool ProposalPassed { get; set; }

    /// <summary>
    /// Gets or sets the results calculation timestamp.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Gets or sets detailed voter analysis if requested.
    /// </summary>
    public VoterAnalysis? VoterAnalysis { get; set; }

    /// <summary>
    /// Gets or sets time-series voting data if requested.
    /// </summary>
    public List<VotingTimePoint>? TimeSeries { get; set; }
}

/// <summary>
/// Represents results for a specific voting option.
/// </summary>
public class OptionResult
{
    /// <summary>
    /// Gets or sets the option identifier.
    /// </summary>
    public string OptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option text.
    /// </summary>
    public string OptionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the total voting power.
    /// </summary>
    public double VotingPower { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total votes.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Gets or sets whether this option is the winner.
    /// </summary>
    public bool IsWinner { get; set; }
}

/// <summary>
/// Represents a vote delegation.
/// </summary>
public class VoteDelegation
{
    /// <summary>
    /// Gets or sets the delegation identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegator's identifier.
    /// </summary>
    public string DelegatorIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegate's identifier.
    /// </summary>
    public string DelegateIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegated voting power.
    /// </summary>
    public double VotingPower { get; set; }

    /// <summary>
    /// Gets or sets the delegation status.
    /// </summary>
    public DelegationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the delegation creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the delegation expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets specific proposal IDs for delegation scope.
    /// </summary>
    public List<string>? ProposalIds { get; set; }

    /// <summary>
    /// Gets or sets delegation conditions.
    /// </summary>
    public Dictionary<string, string>? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the delegation signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Represents a candidate for voting positions.
/// </summary>
public class Candidate
{
    /// <summary>
    /// Gets or sets the candidate identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate's public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate's description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the candidate's website URL.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the candidate status.
    /// </summary>
    public CandidateStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the total votes received.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Gets or sets the total voting power received.
    /// </summary>
    public double TotalVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the candidate's rank.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the registration timestamp.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets the last activity timestamp.
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Gets or sets performance metrics.
    /// </summary>
    public CandidatePerformance? Performance { get; set; }

    /// <summary>
    /// Gets or sets additional candidate metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents candidate performance metrics.
/// </summary>
public class CandidatePerformance
{
    /// <summary>
    /// Gets or sets the governance participation rate.
    /// </summary>
    public double GovernanceParticipation { get; set; }

    /// <summary>
    /// Gets or sets the block production rate.
    /// </summary>
    public double BlockProductionRate { get; set; }

    /// <summary>
    /// Gets or sets the network uptime percentage.
    /// </summary>
    public double NetworkUptime { get; set; }

    /// <summary>
    /// Gets or sets the average response time.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the total rewards earned.
    /// </summary>
    public decimal TotalRewards { get; set; }

    /// <summary>
    /// Gets or sets additional performance metrics.
    /// </summary>
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Represents general voting statistics.
/// </summary>
public class VotingStatistics
{
    /// <summary>
    /// Gets or sets the total number of proposals.
    /// </summary>
    public int TotalProposals { get; set; }

    /// <summary>
    /// Gets or sets the number of active proposals.
    /// </summary>
    public int ActiveProposals { get; set; }

    /// <summary>
    /// Gets or sets the total votes cast.
    /// </summary>
    public long TotalVotesCast { get; set; }

    /// <summary>
    /// Gets or sets the average participation rate.
    /// </summary>
    public double AverageParticipationRate { get; set; }

    /// <summary>
    /// Gets or sets the total voting power in circulation.
    /// </summary>
    public double TotalVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the number of unique voters.
    /// </summary>
    public int UniqueVoters { get; set; }

    /// <summary>
    /// Gets or sets the number of active candidates.
    /// </summary>
    public int ActiveCandidates { get; set; }

    /// <summary>
    /// Gets or sets the number of active delegations.
    /// </summary>
    public int ActiveDelegations { get; set; }

    /// <summary>
    /// Gets or sets additional statistics.
    /// </summary>
    public Dictionary<string, object> AdditionalStats { get; set; } = new();
}

/// <summary>
/// Represents candidate-specific statistics.
/// </summary>
public class CandidateStatistics
{
    /// <summary>
    /// Gets or sets the candidate identifier.
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate name.
    /// </summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total votes received.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Gets or sets the vote percentage.
    /// </summary>
    public double VotePercentage { get; set; }

    /// <summary>
    /// Gets or sets the voting power received.
    /// </summary>
    public double VotingPowerReceived { get; set; }

    /// <summary>
    /// Gets or sets the candidate rank.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets performance scores.
    /// </summary>
    public Dictionary<string, double> PerformanceScores { get; set; } = new();
}

/// <summary>
/// Represents proposal-specific statistics.
/// </summary>
public class ProposalStatistics
{
    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal title.
    /// </summary>
    public string ProposalTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total votes.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Gets or sets the participation rate.
    /// </summary>
    public double ParticipationRate { get; set; }

    /// <summary>
    /// Gets or sets the approval rate.
    /// </summary>
    public double ApprovalRate { get; set; }

    /// <summary>
    /// Gets or sets the voting duration.
    /// </summary>
    public TimeSpan VotingDuration { get; set; }

    /// <summary>
    /// Gets or sets whether the proposal passed.
    /// </summary>
    public bool Passed { get; set; }
}

/// <summary>
/// Represents voting power distribution analysis.
/// </summary>
public class VotingPowerDistribution
{
    /// <summary>
    /// Gets or sets the Gini coefficient for power distribution.
    /// </summary>
    public double GiniCoefficient { get; set; }

    /// <summary>
    /// Gets or sets the concentration ratio (top 10% of voters).
    /// </summary>
    public double ConcentrationRatio { get; set; }

    /// <summary>
    /// Gets or sets power distribution buckets.
    /// </summary>
    public List<PowerBucket> PowerBuckets { get; set; } = new();

    /// <summary>
    /// Gets or sets the median voting power.
    /// </summary>
    public double MedianVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the mean voting power.
    /// </summary>
    public double MeanVotingPower { get; set; }
}

/// <summary>
/// Represents a voting power bucket for distribution analysis.
/// </summary>
public class PowerBucket
{
    /// <summary>
    /// Gets or sets the minimum power in this bucket.
    /// </summary>
    public double MinPower { get; set; }

    /// <summary>
    /// Gets or sets the maximum power in this bucket.
    /// </summary>
    public double MaxPower { get; set; }

    /// <summary>
    /// Gets or sets the number of voters in this bucket.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total voters.
    /// </summary>
    public double VoterPercentage { get; set; }

    /// <summary>
    /// Gets or sets the total power in this bucket.
    /// </summary>
    public double TotalPower { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total power.
    /// </summary>
    public double PowerPercentage { get; set; }
}

/// <summary>
/// Represents voter analysis data.
/// </summary>
public class VoterAnalysis
{
    /// <summary>
    /// Gets or sets the voter turnout by time period.
    /// </summary>
    public List<TurnoutData> TurnoutByTime { get; set; } = new();

    /// <summary>
    /// Gets or sets the voter demographics.
    /// </summary>
    public VoterDemographics Demographics { get; set; } = new();

    /// <summary>
    /// Gets or sets voting patterns.
    /// </summary>
    public VotingPatterns Patterns { get; set; } = new();
}

/// <summary>
/// Represents voter turnout data for a time period.
/// </summary>
public class TurnoutData
{
    /// <summary>
    /// Gets or sets the time period.
    /// </summary>
    public DateTime TimePeriod { get; set; }

    /// <summary>
    /// Gets or sets the cumulative vote count.
    /// </summary>
    public int CumulativeVotes { get; set; }

    /// <summary>
    /// Gets or sets the cumulative voting power.
    /// </summary>
    public double CumulativeVotingPower { get; set; }

    /// <summary>
    /// Gets or sets the turnout rate at this time.
    /// </summary>
    public double TurnoutRate { get; set; }
}

/// <summary>
/// Represents a point in time for voting data.
/// </summary>
public class VotingTimePoint
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the cumulative vote count.
    /// </summary>
    public int CumulativeVotes { get; set; }

    /// <summary>
    /// Gets or sets the cumulative voting power.
    /// </summary>
    public double CumulativeVotingPower { get; set; }

    /// <summary>
    /// Gets or sets vote counts by option.
    /// </summary>
    public Dictionary<string, int> VotesByOption { get; set; } = new();
}

/// <summary>
/// Represents voter demographics.
/// </summary>
public class VoterDemographics
{
    /// <summary>
    /// Gets or sets the total number of voters.
    /// </summary>
    public int TotalVoters { get; set; }

    /// <summary>
    /// Gets or sets the number of new voters.
    /// </summary>
    public int NewVoters { get; set; }

    /// <summary>
    /// Gets or sets the number of returning voters.
    /// </summary>
    public int ReturningVoters { get; set; }

    /// <summary>
    /// Gets or sets the number of proxy voters.
    /// </summary>
    public int ProxyVoters { get; set; }

    /// <summary>
    /// Gets or sets additional demographic data.
    /// </summary>
    public Dictionary<string, int> AdditionalData { get; set; } = new();
}

/// <summary>
/// Represents voting patterns.
/// </summary>
public class VotingPatterns
{
    /// <summary>
    /// Gets or sets the average time to vote (from proposal start).
    /// </summary>
    public TimeSpan AverageTimeToVote { get; set; }

    /// <summary>
    /// Gets or sets the most popular voting time of day.
    /// </summary>
    public TimeSpan PopularVotingTime { get; set; }

    /// <summary>
    /// Gets or sets voting frequency distribution.
    /// </summary>
    public Dictionary<string, int> FrequencyDistribution { get; set; } = new();

    /// <summary>
    /// Gets or sets correlation data between different proposals.
    /// </summary>
    public Dictionary<string, double> ProposalCorrelations { get; set; } = new();
}

// Enums

/// <summary>
/// Proposal category enumeration.
/// </summary>
public enum ProposalCategory
{
    /// <summary>
    /// General governance proposal.
    /// </summary>
    Governance,

    /// <summary>
    /// Technical protocol update.
    /// </summary>
    Technical,

    /// <summary>
    /// Economic parameter change.
    /// </summary>
    Economic,

    /// <summary>
    /// Security enhancement.
    /// </summary>
    Security,

    /// <summary>
    /// Community initiative.
    /// </summary>
    Community,

    /// <summary>
    /// Treasury allocation.
    /// </summary>
    Treasury,

    /// <summary>
    /// Emergency proposal.
    /// </summary>
    Emergency,

    /// <summary>
    /// Other category.
    /// </summary>
    Other
}

/// <summary>
/// Proposal status enumeration.
/// </summary>
public enum ProposalStatus
{
    /// <summary>
    /// Proposal is in draft state.
    /// </summary>
    Draft,

    /// <summary>
    /// Proposal is active and accepting votes.
    /// </summary>
    Active,

    /// <summary>
    /// Proposal voting has ended.
    /// </summary>
    Ended,

    /// <summary>
    /// Proposal was approved.
    /// </summary>
    Approved,

    /// <summary>
    /// Proposal was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Proposal was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Proposal is being executed.
    /// </summary>
    Executing,

    /// <summary>
    /// Proposal execution completed.
    /// </summary>
    Executed,

    /// <summary>
    /// Proposal execution failed.
    /// </summary>
    Failed
}

/// <summary>
/// Delegation status enumeration.
/// </summary>
public enum DelegationStatus
{
    /// <summary>
    /// Delegation is active.
    /// </summary>
    Active,

    /// <summary>
    /// Delegation has been revoked.
    /// </summary>
    Revoked,

    /// <summary>
    /// Delegation has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Delegation is pending activation.
    /// </summary>
    Pending,

    /// <summary>
    /// Delegation has been used.
    /// </summary>
    Used
}

/// <summary>
/// Candidate status enumeration.
/// </summary>
public enum CandidateStatus
{
    /// <summary>
    /// Candidate registration is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Candidate is active and can receive votes.
    /// </summary>
    Active,

    /// <summary>
    /// Candidate is inactive.
    /// </summary>
    Inactive,

    /// <summary>
    /// Candidate has been suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// Candidate has withdrawn.
    /// </summary>
    Withdrawn,

    /// <summary>
    /// Candidate has been disqualified.
    /// </summary>
    Disqualified
}

/// <summary>
/// Voting strategy type enumeration.
/// </summary>
public enum VotingStrategyType
{
    /// <summary>
    /// Simple majority voting strategy.
    /// </summary>
    SimpleMajority,

    /// <summary>
    /// Weighted voting based on power distribution.
    /// </summary>
    WeightedVoting,

    /// <summary>
    /// Delegated voting strategy.
    /// </summary>
    DelegatedVoting,

    /// <summary>
    /// Automatic voting based on predefined rules.
    /// </summary>
    AutomaticVoting,

    /// <summary>
    /// Risk-based voting strategy.
    /// </summary>
    RiskBased,

    /// <summary>
    /// Performance-based voting strategy.
    /// </summary>
    PerformanceBased,

    /// <summary>
    /// Custom voting strategy.
    /// </summary>
    Custom
}

/// <summary>
/// Risk level enumeration.
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
