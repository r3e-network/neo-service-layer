using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting.Models;
using VotingResultDomain = NeoServiceLayer.Services.Voting.Domain.ValueObjects.VotingResult;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Interface for Voting Service operations with advanced council node monitoring and strategies.
/// </summary>
public interface IVotingService : IEnclaveService, IBlockchainService
{
    // Strategy Management
    /// <summary>
    /// Creates a new voting strategy with advanced options.
    /// </summary>
    Task<string> CreateVotingStrategyAsync(VotingStrategyRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Executes voting using a specific strategy with enhanced options.
    /// </summary>
    Task<Models.VotingResult> ExecuteVotingAsync(string strategyId, string voterAddress, ExecutionOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Gets the result of a voting execution with detailed metrics.
    /// </summary>
    Task<Models.VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType);

    /// <summary>
    /// Gets voting strategies for an owner.
    /// </summary>
    Task<IEnumerable<VotingStrategy>> GetVotingStrategiesAsync(string ownerAddress, BlockchainType blockchainType);

    /// <summary>
    /// Updates an existing voting strategy.
    /// </summary>
    Task<bool> UpdateVotingStrategyAsync(string strategyId, VotingStrategyUpdate update, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a voting strategy.
    /// </summary>
    Task<bool> DeleteVotingStrategyAsync(string strategyId, BlockchainType blockchainType);

    // Council Node Monitoring
    /// <summary>
    /// Gets detailed information about council nodes including performance metrics.
    /// </summary>
    Task<IEnumerable<CouncilNodeInfo>> GetCouncilNodesAsync(BlockchainType blockchainType);

    /// <summary>
    /// Analyzes council node behavior over a specified period.
    /// </summary>
    Task<NodeBehaviorAnalysis> AnalyzeNodeBehaviorAsync(string nodeAddress, TimeSpan period, BlockchainType blockchainType);

    /// <summary>
    /// Gets network health metrics for the council.
    /// </summary>
    Task<NetworkHealthMetrics> GetNetworkHealthAsync(BlockchainType blockchainType);

    /// <summary>
    /// Updates node metrics from blockchain data.
    /// </summary>
    Task<bool> UpdateNodeMetricsAsync(string nodeAddress, NodeMetricsUpdate metrics, BlockchainType blockchainType);

    // Advanced Voting Strategies
    /// <summary>
    /// Gets ML-based voting recommendations.
    /// </summary>
    Task<VotingRecommendation> GetMLRecommendationAsync(MLVotingParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets risk-adjusted voting recommendations.
    /// </summary>
    Task<VotingRecommendation> GetRiskAdjustedRecommendationAsync(RiskParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets diversification-focused voting recommendations.
    /// </summary>
    Task<VotingRecommendation> GetDiversificationRecommendationAsync(DiversificationParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets performance-based voting recommendations.
    /// </summary>
    Task<VotingRecommendation> GetPerformanceRecommendationAsync(PerformanceParameters parameters, BlockchainType blockchainType);

    // Automation and Scheduling
    /// <summary>
    /// Schedules automated voting execution.
    /// </summary>
    Task<string> ScheduleVotingExecutionAsync(string strategyId, SchedulingOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Cancels scheduled voting execution.
    /// </summary>
    Task<bool> CancelScheduledExecutionAsync(string scheduleId, BlockchainType blockchainType);

    /// <summary>
    /// Gets scheduled executions for a strategy.
    /// </summary>
    Task<IEnumerable<ScheduledExecution>> GetScheduledExecutionsAsync(string strategyId, BlockchainType blockchainType);

    // Analytics and Monitoring
    /// <summary>
    /// Gets performance analytics for a voting strategy.
    /// </summary>
    Task<StrategyPerformanceAnalytics> GetStrategyPerformanceAsync(string strategyId, TimeSpan period, BlockchainType blockchainType);

    /// <summary>
    /// Assesses risk for a voting strategy.
    /// </summary>
    Task<RiskAssessment> AssessStrategyRiskAsync(string strategyId, BlockchainType blockchainType);

    /// <summary>
    /// Gets real-time alerts for voting and node monitoring.
    /// </summary>
    Task<IEnumerable<VotingAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
    
    // Proposal and Voting Eligibility
    /// <summary>
    /// Checks if a user is eligible to create a proposal.
    /// </summary>
    Task<bool> IsEligibleToCreateProposalAsync(Guid userId);
    
    /// <summary>
    /// Checks if a user is eligible to vote on a proposal.
    /// </summary>
    Task<bool> IsEligibleToVoteAsync(Guid voterId, Guid proposalId);
    
    /// <summary>
    /// Gets the voting weight for a user on a specific proposal.
    /// </summary>
    Task<decimal> GetVoterWeightAsync(Guid voterId, Guid proposalId);
    
    /// <summary>
    /// Records a vote delegation.
    /// </summary>
    Task RecordDelegationAsync(Guid delegatorId, Guid delegateId, Guid proposalId, decimal weight);
    
    /// <summary>
    /// Revokes a vote delegation.
    /// </summary>
    Task RevokeDelegationAsync(Guid proposalId, Guid delegatorId, Guid delegateId);
    
    /// <summary>
    /// Records a vote invalidation.
    /// </summary>
    Task RecordVoteInvalidationAsync(Guid proposalId, Guid voterId, string reason);
    
    /// <summary>
    /// Records an audit event.
    /// </summary>
    Task RecordAuditAsync(Guid proposalId, Guid auditorId, string auditType, Dictionary<string, object> auditData);
    
    /// <summary>
    /// Adds a comment to a proposal.
    /// </summary>
    Task<Guid> AddCommentAsync(Guid proposalId, Guid authorId, string comment);
    
    /// <summary>
    /// Records a proposal view.
    /// </summary>
    Task RecordViewAsync(Guid proposalId, Guid viewerId);
    
    /// <summary>
    /// Generates a vote verification hash.
    /// </summary>
    string GenerateVoteVerificationHash(Guid proposalId, Guid voterId, string optionId, DateTime castAt);
}
