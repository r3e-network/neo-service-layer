using NeoServiceLayer.Core;
using NeoServiceLayer.RPC.Server.Attributes;

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
    [JsonRpcMethod("voting.createstrategy", Description = "Creates a new voting strategy with advanced options")]
    Task<string> CreateVotingStrategyAsync(VotingStrategyRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Executes voting using a specific strategy with enhanced options.
    /// </summary>
    [JsonRpcMethod("voting.executevoting", Description = "Executes voting using a specific strategy")]
    Task<VotingResult> ExecuteVotingAsync(string strategyId, string voterAddress, ExecutionOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Gets the result of a voting execution with detailed metrics.
    /// </summary>
    [JsonRpcMethod("voting.getvotingresult", Description = "Gets the result of a voting execution with detailed metrics")]
    Task<VotingResult> GetVotingResultAsync(string executionId, BlockchainType blockchainType);

    /// <summary>
    /// Gets voting strategies for an owner.
    /// </summary>
    [JsonRpcMethod("voting.getstrategies", Description = "Gets voting strategies for an owner")]
    Task<IEnumerable<VotingStrategy>> GetVotingStrategiesAsync(string ownerAddress, BlockchainType blockchainType);

    /// <summary>
    /// Updates an existing voting strategy.
    /// </summary>
    [JsonRpcMethod("voting.updatestrategy", Description = "Updates an existing voting strategy")]
    Task<bool> UpdateVotingStrategyAsync(string strategyId, VotingStrategyUpdate update, BlockchainType blockchainType);

    /// <summary>
    /// Deletes a voting strategy.
    /// </summary>
    [JsonRpcMethod("voting.deletestrategy", Description = "Deletes a voting strategy")]
    Task<bool> DeleteVotingStrategyAsync(string strategyId, BlockchainType blockchainType);

    // Council Node Monitoring
    /// <summary>
    /// Gets detailed information about council nodes including performance metrics.
    /// </summary>
    [JsonRpcMethod("voting.getcouncilnodes", Description = "Gets detailed information about council nodes")]
    Task<IEnumerable<CouncilNodeInfo>> GetCouncilNodesAsync(BlockchainType blockchainType);

    /// <summary>
    /// Analyzes council node behavior over a specified period.
    /// </summary>
    [JsonRpcMethod("voting.analyzenodebehavior", Description = "Analyzes council node behavior over a specified period")]
    Task<NodeBehaviorAnalysis> AnalyzeNodeBehaviorAsync(string nodeAddress, TimeSpan period, BlockchainType blockchainType);

    /// <summary>
    /// Gets network health metrics for the council.
    /// </summary>
    [JsonRpcMethod("voting.getnetworkhealth", Description = "Gets network health metrics for the council")]
    Task<NetworkHealthMetrics> GetNetworkHealthAsync(BlockchainType blockchainType);

    /// <summary>
    /// Updates node metrics from blockchain data.
    /// </summary>
    Task<bool> UpdateNodeMetricsAsync(string nodeAddress, NodeMetricsUpdate metrics, BlockchainType blockchainType);

    // Advanced Voting Strategies
    /// <summary>
    /// Gets ML-based voting recommendations.
    /// </summary>
    [JsonRpcMethod("voting.getmlrecommendation", Description = "Gets ML-based voting recommendations")]
    Task<VotingRecommendation> GetMLRecommendationAsync(MLVotingParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets risk-adjusted voting recommendations.
    /// </summary>
    [JsonRpcMethod("voting.getriskadjustedrecommendation", Description = "Gets risk-adjusted voting recommendations")]
    Task<VotingRecommendation> GetRiskAdjustedRecommendationAsync(RiskParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets diversification-focused voting recommendations.
    /// </summary>
    [JsonRpcMethod("voting.getdiversificationrecommendation", Description = "Gets diversification-focused voting recommendations")]
    Task<VotingRecommendation> GetDiversificationRecommendationAsync(DiversificationParameters parameters, BlockchainType blockchainType);

    /// <summary>
    /// Gets performance-based voting recommendations.
    /// </summary>
    [JsonRpcMethod("voting.getperformancerecommendation", Description = "Gets performance-based voting recommendations")]
    Task<VotingRecommendation> GetPerformanceRecommendationAsync(PerformanceParameters parameters, BlockchainType blockchainType);

    // Automation and Scheduling
    /// <summary>
    /// Schedules automated voting execution.
    /// </summary>
    [JsonRpcMethod("voting.schedulevotingexecution", Description = "Schedules automated voting execution")]
    Task<string> ScheduleVotingExecutionAsync(string strategyId, SchedulingOptions options, BlockchainType blockchainType);

    /// <summary>
    /// Cancels scheduled voting execution.
    /// </summary>
    [JsonRpcMethod("voting.cancelscheduledexecution", Description = "Cancels scheduled voting execution")]
    Task<bool> CancelScheduledExecutionAsync(string scheduleId, BlockchainType blockchainType);

    /// <summary>
    /// Gets scheduled executions for a strategy.
    /// </summary>
    [JsonRpcMethod("voting.getscheduledexecutions", Description = "Gets scheduled executions for a strategy")]
    Task<IEnumerable<ScheduledExecution>> GetScheduledExecutionsAsync(string strategyId, BlockchainType blockchainType);

    // Analytics and Monitoring
    /// <summary>
    /// Gets performance analytics for a voting strategy.
    /// </summary>
    [JsonRpcMethod("voting.getstrategyperformance", Description = "Gets performance analytics for a voting strategy")]
    Task<StrategyPerformanceAnalytics> GetStrategyPerformanceAsync(string strategyId, TimeSpan period, BlockchainType blockchainType);

    /// <summary>
    /// Assesses risk for a voting strategy.
    /// </summary>
    [JsonRpcMethod("voting.assessstrategyrisk", Description = "Assesses risk for a voting strategy")]
    Task<RiskAssessment> AssessStrategyRiskAsync(string strategyId, BlockchainType blockchainType);

    /// <summary>
    /// Gets real-time alerts for voting and node monitoring.
    /// </summary>
    [JsonRpcMethod("voting.getactivealerts", Description = "Gets real-time alerts for voting and node monitoring")]
    Task<IEnumerable<VotingAlert>> GetActiveAlertsAsync(BlockchainType blockchainType);
}
