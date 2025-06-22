using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Voting.Models;

/// <summary>
/// Response model for proposal operations.
/// </summary>
public class ProposalResult
{
    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the proposal details.
    /// </summary>
    public Proposal? Proposal { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Response model for vote casting operations.
/// </summary>
public class VoteResult
{
    /// <summary>
    /// Gets or sets the vote identifier.
    /// </summary>
    public string VoteId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the vote was successfully cast.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the vote failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the vote details.
    /// </summary>
    public Vote? Vote { get; set; }

    /// <summary>
    /// Gets or sets the vote timestamp.
    /// </summary>
    public DateTime VoteTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the remaining voting power after this vote.
    /// </summary>
    public double RemainingVotingPower { get; set; }
}

/// <summary>
/// Response model for getting proposals with pagination.
/// </summary>
public class GetProposalsResult
{
    /// <summary>
    /// Gets or sets the list of proposals.
    /// </summary>
    public List<Proposal> Proposals { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of proposals.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for voting results.
/// </summary>
public class VotingResultsResult
{
    /// <summary>
    /// Gets or sets the proposal identifier.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voting results.
    /// </summary>
    public VotingResults Results { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the results timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response model for delegation operations.
/// </summary>
public class DelegationResult
{
    /// <summary>
    /// Gets or sets the delegation identifier.
    /// </summary>
    public string DelegationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the delegation details.
    /// </summary>
    public VoteDelegation? Delegation { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Response model for candidate operations.
/// </summary>
public class CandidateResult
{
    /// <summary>
    /// Gets or sets the candidate identifier.
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the candidate details.
    /// </summary>
    public Candidate? Candidate { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction hash if applicable.
    /// </summary>
    public string? TransactionHash { get; set; }
}

/// <summary>
/// Response model for getting candidates with pagination.
/// </summary>
public class GetCandidatesResult
{
    /// <summary>
    /// Gets or sets the list of candidates.
    /// </summary>
    public List<Candidate> Candidates { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of candidates.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets whether there are more pages.
    /// </summary>
    public bool HasMorePages => (PageNumber * PageSize) < TotalCount;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for voting statistics.
/// </summary>
public class VotingStatisticsResult
{
    /// <summary>
    /// Gets or sets general voting statistics.
    /// </summary>
    public VotingStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets candidate statistics.
    /// </summary>
    public List<CandidateStatistics>? CandidateStats { get; set; }

    /// <summary>
    /// Gets or sets proposal statistics.
    /// </summary>
    public List<ProposalStatistics>? ProposalStats { get; set; }

    /// <summary>
    /// Gets or sets voting power distribution.
    /// </summary>
    public VotingPowerDistribution? PowerDistribution { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the statistics timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response model for voting strategy execution.
/// </summary>
public class VotingStrategyResult
{
    /// <summary>
    /// Gets or sets the strategy execution identifier.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the strategy execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of votes cast by the strategy.
    /// </summary>
    public List<VoteResult> VotesCast { get; set; } = new();

    /// <summary>
    /// Gets or sets the total voting power used.
    /// </summary>
    public double TotalVotingPowerUsed { get; set; }

    /// <summary>
    /// Gets or sets the strategy execution summary.
    /// </summary>
    public string ExecutionSummary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutionTimestamp { get; set; }

    /// <summary>
    /// Gets or sets execution metrics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}
