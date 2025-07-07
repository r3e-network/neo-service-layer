using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Voting;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for voting and governance operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/voting")]
[Authorize]
[Tags("Voting")]
public class VotingController : BaseApiController
{
    private readonly IVotingService _votingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VotingController"/> class.
    /// </summary>
    /// <param name="votingService">The voting service.</param>
    /// <param name="logger">The logger.</param>
    public VotingController(IVotingService votingService, ILogger<VotingController> logger)
        : base(logger)
    {
        _votingService = votingService ?? throw new ArgumentNullException(nameof(votingService));
    }

    /// <summary>
    /// Creates a new voting strategy.
    /// </summary>
    /// <param name="request">The voting strategy creation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The created strategy ID.</returns>
    [HttpPost("strategies")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateVotingStrategy(
        [FromBody] VotingStrategyRequest request,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Creating voting strategy for user {UserId}", GetCurrentUserId());

            var result = await _votingService.CreateVotingStrategyAsync(request, blockchainType);
            return Ok(CreateResponse(result, "Voting strategy created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateVotingStrategy");
        }
    }

    /// <summary>
    /// Executes voting using a specific strategy.
    /// </summary>
    /// <param name="strategyId">The strategy ID.</param>
    /// <param name="voterAddress">The voter address.</param>
    /// <param name="options">The execution options.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The voting result.</returns>
    [HttpPost("strategies/{strategyId}/execute")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VotingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ExecuteVoting(
        string strategyId,
        [FromQuery] string voterAddress,
        [FromBody] ExecutionOptions options,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Executing voting strategy {StrategyId} for voter {VoterAddress}", strategyId, voterAddress);

            var result = await _votingService.ExecuteVotingAsync(strategyId, voterAddress, options, blockchainType);
            return Ok(CreateResponse(result, "Voting executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteVoting");
        }
    }

    /// <summary>
    /// Gets voting strategies for an owner.
    /// </summary>
    /// <param name="ownerAddress">The owner address.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of voting strategies.</returns>
    [HttpGet("strategies")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VotingStrategy>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetVotingStrategies(
        [FromQuery] string ownerAddress,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting voting strategies for owner {OwnerAddress}", ownerAddress);

            var result = await _votingService.GetVotingStrategiesAsync(ownerAddress, blockchainType);
            return Ok(CreateResponse(result, "Voting strategies retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetVotingStrategies");
        }
    }

    /// <summary>
    /// Updates an existing voting strategy.
    /// </summary>
    /// <param name="strategyId">The strategy ID.</param>
    /// <param name="update">The strategy update.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Success status.</returns>
    [HttpPut("strategies/{strategyId}")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> UpdateVotingStrategy(
        string strategyId,
        [FromBody] VotingStrategyUpdate update,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Updating voting strategy {StrategyId}", strategyId);

            var result = await _votingService.UpdateVotingStrategyAsync(strategyId, update, blockchainType);
            return Ok(CreateResponse(result, result ? "Voting strategy updated successfully" : "Failed to update voting strategy"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateVotingStrategy");
        }
    }

    /// <summary>
    /// Deletes a voting strategy.
    /// </summary>
    /// <param name="strategyId">The strategy ID.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>Success status.</returns>
    [HttpDelete("strategies/{strategyId}")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> DeleteVotingStrategy(
        string strategyId,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Deleting voting strategy {StrategyId}", strategyId);

            var result = await _votingService.DeleteVotingStrategyAsync(strategyId, blockchainType);
            return Ok(CreateResponse(result, result ? "Voting strategy deleted successfully" : "Failed to delete voting strategy"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "DeleteVotingStrategy");
        }
    }

    /// <summary>
    /// Gets detailed information about council nodes.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of council node information.</returns>
    [HttpGet("council-nodes")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CouncilNodeInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetCouncilNodes(
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting council nodes for blockchain {BlockchainType}", blockchainType);

            var result = await _votingService.GetCouncilNodesAsync(blockchainType);
            return Ok(CreateResponse(result, "Council nodes retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetCouncilNodes");
        }
    }

    /// <summary>
    /// Analyzes council node behavior over a specified period.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="periodHours">The analysis period in hours.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The node behavior analysis.</returns>
    [HttpGet("council-nodes/{nodeAddress}/behavior")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<NodeBehaviorAnalysis>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeNodeBehavior(
        string nodeAddress,
        [FromQuery] int periodHours = 24,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Analyzing behavior for node {NodeAddress} over {Period} hours", nodeAddress, periodHours);

            var period = TimeSpan.FromHours(periodHours);
            var result = await _votingService.AnalyzeNodeBehaviorAsync(nodeAddress, period, blockchainType);
            return Ok(CreateResponse(result, "Node behavior analysis completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AnalyzeNodeBehavior");
        }
    }

    /// <summary>
    /// Gets network health metrics for the council.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The network health metrics.</returns>
    [HttpGet("network-health")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<NetworkHealthMetrics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetNetworkHealth(
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting network health for blockchain {BlockchainType}", blockchainType);

            var result = await _votingService.GetNetworkHealthAsync(blockchainType);
            return Ok(CreateResponse(result, "Network health metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetNetworkHealth");
        }
    }

    /// <summary>
    /// Gets ML-based voting recommendations.
    /// </summary>
    /// <param name="parameters">The ML voting parameters.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The voting recommendation.</returns>
    [HttpPost("recommendations/ml")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VotingRecommendation>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetMLRecommendation(
        [FromBody] MLVotingParameters parameters,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting ML-based voting recommendation");

            var result = await _votingService.GetMLRecommendationAsync(parameters, blockchainType);
            return Ok(CreateResponse(result, "ML recommendation generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetMLRecommendation");
        }
    }

    /// <summary>
    /// Gets risk-adjusted voting recommendations.
    /// </summary>
    /// <param name="parameters">The risk parameters.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The voting recommendation.</returns>
    [HttpPost("recommendations/risk-adjusted")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VotingRecommendation>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetRiskAdjustedRecommendation(
        [FromBody] RiskParameters parameters,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting risk-adjusted voting recommendation");

            var result = await _votingService.GetRiskAdjustedRecommendationAsync(parameters, blockchainType);
            return Ok(CreateResponse(result, "Risk-adjusted recommendation generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetRiskAdjustedRecommendation");
        }
    }

    /// <summary>
    /// Schedules automated voting execution.
    /// </summary>
    /// <param name="strategyId">The strategy ID.</param>
    /// <param name="options">The scheduling options.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The schedule ID.</returns>
    [HttpPost("strategies/{strategyId}/schedule")]
    [Authorize(Roles = "Admin,GovernanceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ScheduleVotingExecution(
        string strategyId,
        [FromBody] SchedulingOptions options,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Scheduling voting execution for strategy {StrategyId}", strategyId);

            var result = await _votingService.ScheduleVotingExecutionAsync(strategyId, options, blockchainType);
            return Ok(CreateResponse(result, "Voting execution scheduled successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ScheduleVotingExecution");
        }
    }

    /// <summary>
    /// Gets performance analytics for a voting strategy.
    /// </summary>
    /// <param name="strategyId">The strategy ID.</param>
    /// <param name="periodDays">The analysis period in days.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The strategy performance analytics.</returns>
    [HttpGet("strategies/{strategyId}/performance")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<StrategyPerformanceAnalytics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetStrategyPerformance(
        string strategyId,
        [FromQuery] int periodDays = 30,
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting performance analytics for strategy {StrategyId}", strategyId);

            var period = TimeSpan.FromDays(periodDays);
            var result = await _votingService.GetStrategyPerformanceAsync(strategyId, period, blockchainType);
            return Ok(CreateResponse(result, "Strategy performance analytics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetStrategyPerformance");
        }
    }

    /// <summary>
    /// Gets real-time alerts for voting and node monitoring.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The list of active alerts.</returns>
    [HttpGet("alerts")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VotingAlert>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetActiveAlerts(
        [FromQuery] BlockchainType blockchainType = BlockchainType.NeoN3)
    {
        try
        {
            Logger.LogInformation("Getting active voting alerts");

            var result = await _votingService.GetActiveAlertsAsync(blockchainType);
            return Ok(CreateResponse(result, "Active alerts retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetActiveAlerts");
        }
    }
}
