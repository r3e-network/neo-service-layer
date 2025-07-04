using NeoServiceLayer.Core;

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
    Task<VotingResult> ExecuteVotingAsync(string strategyId, string voterAddress, ExecutionOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Gets the result of a voting execution with detailed metrics.
    /// </summary>
    Task<VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType);

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
}
