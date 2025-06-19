using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for blockchain and node health monitoring operations.
/// </summary>
[Tags("Health Monitoring")]
public class HealthController : BaseApiController
{
    private readonly IHealthService _healthService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="healthService">The health service.</param>
    /// <param name="logger">The logger.</param>
    public HealthController(
        IHealthService healthService,
        ILogger<HealthController> logger) : base(logger)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Gets the health status of a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The node health report.</returns>
    /// <response code="200">Node health retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Node not found.</response>
    [HttpGet("node/{nodeAddress}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<NodeHealthReport>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetNodeHealth(
        [FromRoute] string nodeAddress,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var healthReport = await _healthService.GetNodeHealthAsync(nodeAddress, blockchain);

            if (healthReport == null)
            {
                return NotFound(CreateErrorResponse($"Node {nodeAddress} not found or not monitored"));
            }

            Logger.LogInformation("Retrieved health for node {NodeAddress} on {BlockchainType} by user {UserId}",
                nodeAddress, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(healthReport, "Node health retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetNodeHealth");
        }
    }

    /// <summary>
    /// Gets the health status of all monitored nodes.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The health reports for all nodes.</returns>
    /// <response code="200">All nodes health retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("nodes/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NodeHealthReport>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetAllNodesHealth([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var healthReports = await _healthService.GetAllNodesHealthAsync(blockchain);

            Logger.LogInformation("Retrieved health for {NodeCount} nodes on {BlockchainType} by user {UserId}",
                healthReports.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(healthReports, "All nodes health retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetAllNodesHealth");
        }
    }

    /// <summary>
    /// Gets the consensus health status for the blockchain network.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The consensus health report.</returns>
    /// <response code="200">Consensus health retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("consensus/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ConsensusHealthReport>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetConsensusHealth([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var consensusHealth = await _healthService.GetConsensusHealthAsync(blockchain);

            Logger.LogInformation("Retrieved consensus health on {BlockchainType} by user {UserId}",
                blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(consensusHealth, "Consensus health retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetConsensusHealth");
        }
    }

    /// <summary>
    /// Registers a node for health monitoring.
    /// </summary>
    /// <param name="request">The node registration request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The registration result.</returns>
    /// <response code="200">Node registered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Registration failed.</response>
    [HttpPost("register-node/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RegisterNodeForMonitoring(
        [FromBody] NodeRegistrationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _healthService.RegisterNodeForMonitoringAsync(request, blockchain);

            Logger.LogInformation("Registered node {NodeAddress} for monitoring on {BlockchainType} by user {UserId}",
                request.NodeAddress, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Node registered for monitoring successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RegisterNodeForMonitoring");
        }
    }

    /// <summary>
    /// Unregisters a node from health monitoring.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The unregistration result.</returns>
    /// <response code="200">Node unregistered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Node not found.</response>
    [HttpDelete("unregister-node/{nodeAddress}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UnregisterNode(
        [FromRoute] string nodeAddress,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _healthService.UnregisterNodeAsync(nodeAddress, blockchain);

            Logger.LogInformation("Unregistered node {NodeAddress} from monitoring on {BlockchainType} by user {UserId}",
                nodeAddress, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Node unregistered from monitoring successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UnregisterNode");
        }
    }

    /// <summary>
    /// Gets active health alerts for the blockchain network.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The active health alerts.</returns>
    /// <response code="200">Active alerts retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("alerts/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<HealthAlert>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetActiveAlerts([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var alerts = await _healthService.GetActiveAlertsAsync(blockchain);

            Logger.LogInformation("Retrieved {AlertCount} active health alerts on {BlockchainType} by user {UserId}",
                alerts.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(alerts, "Active health alerts retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetActiveAlerts");
        }
    }

    /// <summary>
    /// Sets a health threshold for a specific node.
    /// </summary>
    /// <param name="nodeAddress">The node address.</param>
    /// <param name="threshold">The health threshold configuration.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The threshold setting result.</returns>
    /// <response code="200">Health threshold set successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Node not found.</response>
    /// <response code="500">Threshold setting failed.</response>
    [HttpPost("set-threshold/{nodeAddress}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SetHealthThreshold(
        [FromRoute] string nodeAddress,
        [FromBody] HealthThreshold threshold,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _healthService.SetHealthThresholdAsync(nodeAddress, threshold, blockchain);

            Logger.LogInformation("Set health threshold for node {NodeAddress} on {BlockchainType} by user {UserId}",
                nodeAddress, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Health threshold set successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SetHealthThreshold");
        }
    }

    /// <summary>
    /// Gets network health metrics for the blockchain.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The network health metrics.</returns>
    /// <response code="200">Network metrics retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("network-metrics/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<HealthMetrics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetNetworkMetrics([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var metrics = await _healthService.GetNetworkMetricsAsync(blockchain);

            Logger.LogInformation("Retrieved network health metrics on {BlockchainType} by user {UserId}",
                blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(metrics, "Network health metrics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetNetworkMetrics");
        }
    }
} 