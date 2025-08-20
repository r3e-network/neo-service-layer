using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using FairOrderingSvc = NeoServiceLayer.Advanced.FairOrdering;
using FairOrderingModels = NeoServiceLayer.Advanced.FairOrdering.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for fair transaction ordering and MEV protection operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fair-ordering")]
[Authorize]
[Tags("Fair Ordering")]
public class FairOrderingController : BaseApiController
{
    private readonly FairOrderingSvc.IFairOrderingService _fairOrderingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FairOrderingController"/> class.
    /// </summary>
    /// <param name="fairOrderingService">The fair ordering service.</param>
    /// <param name="logger">The logger.</param>
    public FairOrderingController(FairOrderingSvc.IFairOrderingService fairOrderingService, ILogger<FairOrderingController> logger)
        : base(logger)
    {
        _fairOrderingService = fairOrderingService ?? throw new ArgumentNullException(nameof(fairOrderingService));
    }

    /// <summary>
    /// Creates a new ordering pool for fair transaction processing.
    /// </summary>
    /// <param name="config">The ordering pool configuration.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created pool ID.</returns>
    /// <response code="200">Ordering pool created successfully.</response>
    /// <response code="400">Invalid pool configuration.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPost("pools/{blockchainType}")]
    [Authorize(Roles = "Admin,PoolManager")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateOrderingPool(
        [FromBody] FairOrderingModels.OrderingPoolConfig config,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var poolId = await _fairOrderingService.CreateOrderingPoolAsync(config, blockchain);

            Logger.LogInformation("Ordering pool created successfully with ID: {PoolId} on {Blockchain}",
                poolId, blockchainType);

            return Ok(CreateResponse(poolId, "Ordering pool created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating ordering pool");
        }
    }

    /// <summary>
    /// Submits a transaction for fair ordering protection.
    /// </summary>
    /// <param name="request">The fair transaction request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The transaction ID.</returns>
    /// <response code="200">Transaction submitted successfully.</response>
    /// <response code="400">Invalid transaction request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpPost("transactions/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Trader")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 429)]
    public async Task<IActionResult> SubmitFairTransaction(
        [FromBody] FairOrderingModels.FairTransactionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var transactionId = await _fairOrderingService.SubmitFairTransactionAsync(request, blockchain);

            Logger.LogInformation("Fair transaction submitted successfully with ID: {TransactionId} on {Blockchain}",
                transactionId, blockchainType);

            return Ok(CreateResponse(transactionId, "Transaction submitted for fair ordering"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "submitting fair transaction");
        }
    }

    /// <summary>
    /// Analyzes the fairness risk of a transaction.
    /// </summary>
    /// <param name="request">The transaction analysis request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The fairness analysis result.</returns>
    /// <response code="200">Analysis completed successfully.</response>
    /// <response code="400">Invalid analysis request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("analyze/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Trader,Analyst")]
    [ProducesResponseType(typeof(ApiResponse<FairnessAnalysisResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeFairnessRisk(
        [FromBody] FairOrderingModels.TransactionAnalysisRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _fairOrderingService.AnalyzeFairnessRiskAsync(request, blockchain);

            Logger.LogInformation("Fairness analysis completed for transaction: Risk level {RiskLevel}, MEV {EstimatedMev:F4}",
                result.RiskLevel, result.EstimatedMEV);

            return Ok(CreateResponse(result, "Fairness analysis completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "analyzing fairness risk");
        }
    }

    /// <summary>
    /// Submits a transaction to an ordering pool.
    /// </summary>
    /// <param name="submission">The transaction submission.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The submission ID.</returns>
    /// <response code="200">Transaction submitted successfully.</response>
    /// <response code="400">Invalid submission request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Pool not found.</response>
    [HttpPost("submit/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Trader")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SubmitTransaction(
        [FromBody] NeoServiceLayer.Advanced.FairOrdering.Models.TransactionSubmission submission,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var submissionId = await _fairOrderingService.SubmitTransactionAsync(submission, blockchain);

            Logger.LogInformation("Transaction submitted to ordering pool with ID: {SubmissionId} on {Blockchain}",
                submissionId, blockchainType);

            return Ok(CreateResponse(submissionId, "Transaction submitted to ordering pool"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "submitting transaction to pool");
        }
    }

    /// <summary>
    /// Gets fairness metrics for an ordering pool.
    /// </summary>
    /// <param name="poolId">The pool ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The fairness metrics.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Pool not found.</response>
    [HttpGet("pools/{poolId}/metrics/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Analyst")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetFairnessMetrics(
        [FromRoute] string poolId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var metrics = await _fairOrderingService.GetFairnessMetricsAsync(poolId, blockchain);

            Logger.LogInformation("Fairness metrics retrieved for pool {PoolId} on {Blockchain}",
                poolId, blockchainType);

            return Ok(CreateResponse(metrics, "Fairness metrics retrieved successfully"));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateErrorResponse($"Pool not found: {poolId}"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving fairness metrics");
        }
    }

    /// <summary>
    /// Lists all active ordering pools.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>List of ordering pools.</returns>
    /// <response code="200">Pools retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("pools/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Analyst")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetOrderingPools(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var pools = await _fairOrderingService.GetOrderingPoolsAsync(blockchain);

            Logger.LogInformation("Retrieved {PoolCount} ordering pools for {Blockchain}",
                pools.Count(), blockchainType);

            return Ok(CreateResponse(pools, "Ordering pools retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving ordering pools");
        }
    }

    /// <summary>
    /// Updates the configuration of an ordering pool.
    /// </summary>
    /// <param name="poolId">The pool ID.</param>
    /// <param name="config">The updated pool configuration.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Pool updated successfully.</response>
    /// <response code="400">Invalid configuration.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Pool not found.</response>
    [HttpPut("pools/{poolId}/{blockchainType}")]
    [Authorize(Roles = "Admin,PoolManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdatePoolConfig(
        [FromRoute] string poolId,
        [FromBody] FairOrderingModels.OrderingPoolConfig config,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var success = await _fairOrderingService.UpdatePoolConfigAsync(poolId, config, blockchain);

            if (!success)
            {
                return NotFound(CreateErrorResponse($"Pool not found: {poolId}"));
            }

            Logger.LogInformation("Pool configuration updated successfully for {PoolId} on {Blockchain}",
                poolId, blockchainType);

            return Ok(CreateResponse(success, "Pool configuration updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating pool configuration");
        }
    }

    /// <summary>
    /// Analyzes MEV risk for a transaction.
    /// </summary>
    /// <param name="request">The MEV analysis request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The MEV protection result.</returns>
    /// <response code="200">MEV analysis completed successfully.</response>
    /// <response code="400">Invalid analysis request.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("mev-analysis/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Trader,Analyst")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeMevRisk(
        [FromBody] NeoServiceLayer.Advanced.FairOrdering.Models.MevAnalysisRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _fairOrderingService.AnalyzeMevRiskAsync(request, blockchain);

            Logger.LogInformation("MEV analysis completed for transaction {TransactionHash}: Risk score {RiskScore:F3}",
                request.TransactionHash, result.MevRiskScore);

            return Ok(CreateResponse(result, "MEV analysis completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "analyzing MEV risk");
        }
    }

    /// <summary>
    /// Gets the ordering result for a specific transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The ordering result.</returns>
    /// <response code="200">Ordering result retrieved successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Transaction not found.</response>
    [HttpGet("transactions/{transactionId}/result/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser,Trader")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetOrderingResult(
        [FromRoute] string transactionId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _fairOrderingService.GetOrderingResultAsync(transactionId, blockchain);

            Logger.LogInformation("Retrieved ordering result for transaction {TransactionId}: Status={Status}",
                transactionId, result.Status);

            return Ok(CreateResponse(result, "Ordering result retrieved successfully"));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(CreateErrorResponse($"Transaction not found: {transactionId}"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving ordering result");
        }
    }

    /// <summary>
    /// Gets health status of the fair ordering service.
    /// </summary>
    /// <returns>Service health information.</returns>
    /// <response code="200">Health status retrieved successfully.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("health")]
    [Authorize(Roles = "Admin,ServiceUser,Monitor")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var health = await _fairOrderingService.GetHealthAsync();

            var healthInfo = new
            {
                Status = health.ToString(),
                IsRunning = _fairOrderingService.IsRunning,
                ServiceName = _fairOrderingService.Name,
                Version = _fairOrderingService.Version,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(CreateResponse(healthInfo, "Health status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving health status");
        }
    }
}
