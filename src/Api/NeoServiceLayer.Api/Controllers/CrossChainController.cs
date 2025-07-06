using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.CrossChain.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for cross-chain interoperability operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cross-chain")]
[Authorize]
[Tags("Cross-Chain")]
public class CrossChainController : BaseApiController
{
    private readonly ICrossChainService _crossChainService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChainController"/> class.
    /// </summary>
    /// <param name="crossChainService">The cross-chain service.</param>
    /// <param name="logger">The logger.</param>
    public CrossChainController(ICrossChainService crossChainService, ILogger<CrossChainController> logger)
        : base(logger)
    {
        _crossChainService = crossChainService ?? throw new ArgumentNullException(nameof(crossChainService));
    }

    /// <summary>
    /// Initiates a cross-chain transfer.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <returns>The transfer result.</returns>
    /// <response code="200">Transfer initiated successfully.</response>
    /// <response code="400">Invalid transfer parameters.</response>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(ApiResponse<CrossChainTransferResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> InitiateTransfer(
        [FromBody] CrossChainTransferRequest request)
    {
        try
        {
            var sourceBlockchain = ParseBlockchainType(request.SourceBlockchain);
            var targetBlockchain = ParseBlockchainType(request.TargetBlockchain);

            var result = await _crossChainService.InitiateTransferAsync(request, sourceBlockchain);

            Logger.LogInformation("Initiated cross-chain transfer {TransferId} from {Source} to {Target}",
                result.TransferId, request.SourceBlockchain, request.TargetBlockchain);

            return Ok(CreateResponse(result, "Cross-chain transfer initiated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "initiating cross-chain transfer");
        }
    }

    /// <summary>
    /// Executes a contract call on another blockchain.
    /// </summary>
    /// <param name="request">The contract call request.</param>
    /// <returns>The execution result.</returns>
    /// <response code="200">Contract call executed successfully.</response>
    /// <response code="400">Invalid call parameters.</response>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<ContractCallResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExecuteContractCall(
        [FromBody] CrossChainContractCallRequest request)
    {
        try
        {
            var sourceBlockchain = ParseBlockchainType(request.SourceBlockchain);
            var targetBlockchain = ParseBlockchainType(request.TargetBlockchain);

            var result = await _crossChainService.ExecuteContractCallAsync(request, sourceBlockchain);

            Logger.LogInformation("Executed cross-chain contract call to {Method} on {Target}",
                request.Method, request.TargetBlockchain);

            return Ok(CreateResponse(result, "Contract call executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "executing cross-chain contract call");
        }
    }

    /// <summary>
    /// Verifies a cross-chain message.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<MessageVerificationResult>), 200)]
    public async Task<IActionResult> VerifyMessage(
        [FromBody] VerifyMessageRequest request)
    {
        try
        {
            var blockchain = ParseBlockchainType(request.SourceBlockchain);
            var result = await _crossChainService.VerifyMessageAsync(request, blockchain);

            Logger.LogInformation("Verified cross-chain message: Valid={IsValid}", result.IsValid);

            return Ok(CreateResponse(result, "Message verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "verifying cross-chain message");
        }
    }

    /// <summary>
    /// Gets the status of a cross-chain transfer.
    /// </summary>
    /// <param name="transferId">The transfer ID.</param>
    /// <returns>The transfer status.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="404">Transfer not found.</response>
    [HttpGet("transfer/{transferId}/status")]
    [ProducesResponseType(typeof(ApiResponse<TransferStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetTransferStatus(
        [FromRoute] string transferId)
    {
        try
        {
            var result = await _crossChainService.GetTransferStatusAsync(transferId);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Transfer not found: {transferId}"));
            }

            return Ok(CreateResponse(result, "Transfer status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving transfer status");
        }
    }

    /// <summary>
    /// Gets supported blockchain bridges.
    /// </summary>
    /// <returns>List of supported bridges.</returns>
    /// <response code="200">Bridges retrieved successfully.</response>
    [HttpGet("bridges")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BridgeInfo>>), 200)]
    public async Task<IActionResult> GetSupportedBridges()
    {
        try
        {
            var result = await _crossChainService.GetSupportedBridgesAsync();

            return Ok(CreateResponse(result, "Supported bridges retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving supported bridges");
        }
    }

    /// <summary>
    /// Estimates fees for a cross-chain transfer.
    /// </summary>
    /// <param name="request">The fee estimation request.</param>
    /// <returns>The fee estimate.</returns>
    /// <response code="200">Fees estimated successfully.</response>
    [HttpPost("estimate-fees")]
    [ProducesResponseType(typeof(ApiResponse<FeeEstimate>), 200)]
    public async Task<IActionResult> EstimateFees(
        [FromBody] FeeEstimationRequest request)
    {
        try
        {
            var sourceBlockchain = ParseBlockchainType(request.SourceBlockchain);
            var targetBlockchain = ParseBlockchainType(request.TargetBlockchain);

            var result = await _crossChainService.EstimateFeesAsync(request, sourceBlockchain);

            return Ok(CreateResponse(result, "Fees estimated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "estimating fees");
        }
    }

    /// <summary>
    /// Gets cross-chain transaction history.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <param name="limit">Maximum number of transactions to return.</param>
    /// <returns>Transaction history.</returns>
    /// <response code="200">History retrieved successfully.</response>
    [HttpGet("history/{address}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CrossChainTransaction>>), 200)]
    public async Task<IActionResult> GetTransactionHistory(
        [FromRoute] string address,
        [FromQuery] int limit = 50)
    {
        try
        {
            var result = await _crossChainService.GetTransactionHistoryAsync(address, limit);

            return Ok(CreateResponse(result, "Transaction history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving transaction history");
        }
    }

    /// <summary>
    /// Registers a cross-chain event listener.
    /// </summary>
    /// <param name="request">The event registration request.</param>
    /// <returns>The listener ID.</returns>
    /// <response code="200">Listener registered successfully.</response>
    [HttpPost("events/register")]
    [Authorize(Roles = "Admin,Developer")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> RegisterEventListener(
        [FromBody] EventListenerRequest request)
    {
        try
        {
            var blockchain = ParseBlockchainType(request.Blockchain);
            var listenerId = await _crossChainService.RegisterEventListenerAsync(request, blockchain);

            Logger.LogInformation("Registered cross-chain event listener {ListenerId} for {EventType}",
                listenerId, request.EventType);

            return Ok(CreateResponse(listenerId, "Event listener registered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "registering event listener");
        }
    }
}