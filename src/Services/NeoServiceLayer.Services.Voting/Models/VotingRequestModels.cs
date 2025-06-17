namespace NeoServiceLayer.Services.Voting.Models;

/// <summary>
/// Create proposal request.
/// </summary>
public class CreateProposalRequest
{
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
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voting end time.
    /// </summary>
    public DateTime VotingEndTime { get; set; }

    /// <summary>
    /// Gets or sets the minimum quorum required.
    /// </summary>
    public int MinimumQuorum { get; set; }
}

/// <summary>
/// Cast vote request.
/// </summary>
public class CastVoteRequest
{
    /// <summary>
    /// Gets or sets the proposal ID.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voter address.
    /// </summary>
    public string VoterAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vote choice.
    /// </summary>
    public string VoteChoice { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voting weight.
    /// </summary>
    public decimal VotingWeight { get; set; }
}

/// <summary>
/// Delegate vote request.
/// </summary>
public class DelegateVoteRequest
{
    /// <summary>
    /// Gets or sets the delegator address.
    /// </summary>
    public string DelegatorAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegate address.
    /// </summary>
    public string DelegateAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegation scope.
    /// </summary>
    public string DelegationScope { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegation duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Register candidate request.
/// </summary>
public class RegisterCandidateRequest
{
    /// <summary>
    /// Gets or sets the candidate address.
    /// </summary>
    public string CandidateAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the candidate description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform statement.
    /// </summary>
    public string PlatformStatement { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the campaign website.
    /// </summary>
    public string? Website { get; set; }
}

/// <summary>
/// Execute voting strategy request.
/// </summary>
public class ExecuteVotingStrategyRequest
{
    /// <summary>
    /// Gets or sets the strategy type.
    /// </summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposal ID.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strategy parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution priority.
    /// </summary>
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Get proposal request.
/// </summary>
public class GetProposalRequest
{
    /// <summary>
    /// Gets or sets the proposal ID.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;
}

/// <summary>
/// Get proposals request.
/// </summary>
public class GetProposalsRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Get voting results request.
/// </summary>
public class GetVotingResultsRequest
{
    /// <summary>
    /// Gets or sets the proposal ID.
    /// </summary>
    public string ProposalId { get; set; } = string.Empty;
}

/// <summary>
/// Revoke delegation request.
/// </summary>
public class RevokeDelegationRequest
{
    /// <summary>
    /// Gets or sets the delegation ID.
    /// </summary>
    public string DelegationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for revocation.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Get candidate request.
/// </summary>
public class GetCandidateRequest
{
    /// <summary>
    /// Gets or sets the candidate ID.
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;
}

/// <summary>
/// Get candidates request.
/// </summary>
public class GetCandidatesRequest
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Unregister candidate request.
/// </summary>
public class UnregisterCandidateRequest
{
    /// <summary>
    /// Gets or sets the candidate ID.
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for unregistration.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Voting statistics request.
/// </summary>
public class VotingStatisticsRequest
{
    /// <summary>
    /// Gets or sets the start date for statistics.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for statistics.
    /// </summary>
    public DateTime? EndDate { get; set; }
} 