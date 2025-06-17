using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.CrossChain.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for cross-chain interoperability operations.
/// </summary>
[Tags("Cross-Chain")]
public class CrossChainController : BaseApiController
{
    private readonly ICrossChainService _crossChainService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossChainController"/> class.
    /// </summary>
    /// <param name="crossChainService">The cross-chain service.</param>
    /// <param name="logger">The logger.</param>
    public CrossChainController(
        ICrossChainService crossChainService,
        ILogger<CrossChainController> logger) : base(logger)
    {
        _crossChainService = crossChainService;
    }

    /// <summary>
    /// Sends a cross-chain message.
    /// </summary>
    /// <param name="request">The cross-chain message request.</param>
    /// <param name="sourceBlockchain">The source blockchain (NeoN3 or NeoX).</param>
    /// <param name="targetBlockchain">The target blockchain (NeoN3 or NeoX).</param>
    /// <returns>The message ID.</returns>
    /// <response code="200">Message sent successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Message sending failed.</response>
    [HttpPost("send-message/{sourceBlockchain}/{targetBlockchain}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SendMessage(
        [FromBody] CrossChainMessageRequest request,
        [FromRoute] string sourceBlockchain,
        [FromRoute] string targetBlockchain)
    {
        try
        {
            if (!IsValidBlockchainType(sourceBlockchain) || !IsValidBlockchainType(targetBlockchain))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain types: {sourceBlockchain} -> {targetBlockchain}"));
            }

            var source = ParseBlockchainType(sourceBlockchain);
            var target = ParseBlockchainType(targetBlockchain);
            var messageId = await _crossChainService.SendMessageAsync(request, source, target);

            Logger.LogInformation("Sent cross-chain message {MessageId} from {SourceBlockchain} to {TargetBlockchain} by user {UserId}",
                messageId, sourceBlockchain, targetBlockchain, GetCurrentUserId());

            return Ok(CreateResponse(messageId, "Cross-chain message sent successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SendMessage");
        }
    }

    /// <summary>
    /// Transfers tokens across blockchains.
    /// </summary>
    /// <param name="request">The cross-chain transfer request.</param>
    /// <param name="sourceBlockchain">The source blockchain (NeoN3 or NeoX).</param>
    /// <param name="targetBlockchain">The target blockchain (NeoN3 or NeoX).</param>
    /// <returns>The transfer ID.</returns>
    /// <response code="200">Transfer initiated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Transfer failed.</response>
    [HttpPost("transfer-tokens/{sourceBlockchain}/{targetBlockchain}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> TransferTokens(
        [FromBody] CrossChainTransferRequest request,
        [FromRoute] string sourceBlockchain,
        [FromRoute] string targetBlockchain)
    {
        try
        {
            if (!IsValidBlockchainType(sourceBlockchain) || !IsValidBlockchainType(targetBlockchain))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain types: {sourceBlockchain} -> {targetBlockchain}"));
            }

            var source = ParseBlockchainType(sourceBlockchain);
            var target = ParseBlockchainType(targetBlockchain);
            var transferId = await _crossChainService.TransferTokensAsync(request, source, target);

            Logger.LogInformation("Initiated cross-chain transfer {TransferId} from {SourceBlockchain} to {TargetBlockchain} by user {UserId}",
                transferId, sourceBlockchain, targetBlockchain, GetCurrentUserId());

            return Ok(CreateResponse(transferId, "Cross-chain transfer initiated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "TransferTokens");
        }
    }

    /// <summary>
    /// Executes a remote contract call across blockchains.
    /// </summary>
    /// <param name="request">The remote call request.</param>
    /// <param name="sourceBlockchain">The source blockchain (NeoN3 or NeoX).</param>
    /// <param name="targetBlockchain">The target blockchain (NeoN3 or NeoX).</param>
    /// <returns>The call ID.</returns>
    /// <response code="200">Remote call executed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Remote call failed.</response>
    [HttpPost("execute-remote-call/{sourceBlockchain}/{targetBlockchain}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteRemoteCall(
        [FromBody] RemoteCallRequest request,
        [FromRoute] string sourceBlockchain,
        [FromRoute] string targetBlockchain)
    {
        try
        {
            if (!IsValidBlockchainType(sourceBlockchain) || !IsValidBlockchainType(targetBlockchain))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain types: {sourceBlockchain} -> {targetBlockchain}"));
            }

            var source = ParseBlockchainType(sourceBlockchain);
            var target = ParseBlockchainType(targetBlockchain);
            var callId = await _crossChainService.ExecuteRemoteCallAsync(request, source, target);

            Logger.LogInformation("Executed remote call {CallId} from {SourceBlockchain} to {TargetBlockchain} by user {UserId}",
                callId, sourceBlockchain, targetBlockchain, GetCurrentUserId());

            return Ok(CreateResponse(callId, "Remote call executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteRemoteCall");
        }
    }

    /// <summary>
    /// Gets the status of a cross-chain message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The message status.</returns>
    /// <response code="200">Message status retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Message not found.</response>
    [HttpGet("message-status/{messageId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<CrossChainMessageStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetMessageStatus(
        [FromRoute] string messageId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var status = await _crossChainService.GetMessageStatusAsync(messageId, blockchain);

            Logger.LogInformation("Retrieved message status for {MessageId} on {BlockchainType} by user {UserId}",
                messageId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(status, "Message status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetMessageStatus");
        }
    }

    /// <summary>
    /// Gets pending messages for a destination chain.
    /// </summary>
    /// <param name="blockchainType">The destination blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The pending messages.</returns>
    /// <response code="200">Pending messages retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("pending-messages/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CrossChainMessage>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetPendingMessages([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var messages = await _crossChainService.GetPendingMessagesAsync(blockchain);

            Logger.LogInformation("Retrieved {MessageCount} pending messages for {BlockchainType} by user {UserId}",
                messages.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(messages, "Pending messages retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetPendingMessages");
        }
    }

    /// <summary>
    /// Verifies a cross-chain message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="proof">The verification proof.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Message verified successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("verify-message/{messageId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> VerifyMessage(
        [FromRoute] string messageId,
        [FromBody] string proof,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var isValid = await _crossChainService.VerifyMessageAsync(messageId, proof, blockchain);

            Logger.LogInformation("Verified message {MessageId} on {BlockchainType}: {IsValid} by user {UserId}",
                messageId, blockchainType, isValid, GetCurrentUserId());

            return Ok(CreateResponse(isValid, "Message verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "VerifyMessage");
        }
    }

    /// <summary>
    /// Gets the optimal route between two blockchains.
    /// </summary>
    /// <param name="sourceBlockchain">The source blockchain (NeoN3 or NeoX).</param>
    /// <param name="targetBlockchain">The target blockchain (NeoN3 or NeoX).</param>
    /// <returns>The optimal route.</returns>
    /// <response code="200">Optimal route retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("optimal-route/{sourceBlockchain}/{targetBlockchain}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<CrossChainRoute>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetOptimalRoute(
        [FromRoute] string sourceBlockchain,
        [FromRoute] string targetBlockchain)
    {
        try
        {
            if (!IsValidBlockchainType(sourceBlockchain) || !IsValidBlockchainType(targetBlockchain))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain types: {sourceBlockchain} -> {targetBlockchain}"));
            }

            var source = ParseBlockchainType(sourceBlockchain);
            var target = ParseBlockchainType(targetBlockchain);
            var route = await _crossChainService.GetOptimalRouteAsync(source, target);

            Logger.LogInformation("Retrieved optimal route from {SourceBlockchain} to {TargetBlockchain} by user {UserId}",
                sourceBlockchain, targetBlockchain, GetCurrentUserId());

            return Ok(CreateResponse(route, "Optimal route retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetOptimalRoute");
        }
    }

    /// <summary>
    /// Estimates fees for a cross-chain operation.
    /// </summary>
    /// <param name="operation">The cross-chain operation.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The estimated fee.</returns>
    /// <response code="200">Fee estimated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("estimate-fees/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> EstimateFees(
        [FromBody] CrossChainOperation operation,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var fee = await _crossChainService.EstimateFeesAsync(operation, blockchain);

            Logger.LogInformation("Estimated fee {Fee} for operation on {BlockchainType} by user {UserId}",
                fee, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(fee, "Fee estimated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "EstimateFees");
        }
    }

    /// <summary>
    /// Gets supported blockchains for cross-chain operations.
    /// </summary>
    /// <returns>The supported chains.</returns>
    /// <response code="200">Supported chains retrieved successfully.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("supported-chains")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SupportedChain>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetSupportedChains()
    {
        try
        {
            var chains = await _crossChainService.GetSupportedChainsAsync();

            Logger.LogInformation("Retrieved {ChainCount} supported chains by user {UserId}",
                chains.Count(), GetCurrentUserId());

            return Ok(CreateResponse(chains, "Supported chains retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetSupportedChains");
        }
    }

    /// <summary>
    /// Registers a token mapping for cross-chain operations.
    /// </summary>
    /// <param name="mapping">The token mapping.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The registration result.</returns>
    /// <response code="200">Token mapping registered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Registration failed.</response>
    [HttpPost("register-token-mapping/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RegisterTokenMapping(
        [FromBody] TokenMapping mapping,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _crossChainService.RegisterTokenMappingAsync(mapping, blockchain);

            Logger.LogInformation("Registered token mapping for {TokenSymbol} on {BlockchainType} by user {UserId}",
                mapping.TokenSymbol, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Token mapping registered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RegisterTokenMapping");
        }
    }

    /// <summary>
    /// Executes a cross-chain contract call.
    /// </summary>
    /// <param name="request">The contract call request.</param>
    /// <param name="sourceBlockchain">The source blockchain (NeoN3 or NeoX).</param>
    /// <param name="targetBlockchain">The target blockchain (NeoN3 or NeoX).</param>
    /// <returns>The execution result.</returns>
    /// <response code="200">Contract call executed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Contract call failed.</response>
    [HttpPost("execute-contract-call/{sourceBlockchain}/{targetBlockchain}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<CrossChainExecutionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteContractCall(
        [FromBody] CrossChainContractCallRequest request,
        [FromRoute] string sourceBlockchain,
        [FromRoute] string targetBlockchain)
    {
        try
        {
            if (!IsValidBlockchainType(sourceBlockchain) || !IsValidBlockchainType(targetBlockchain))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain types: {sourceBlockchain} -> {targetBlockchain}"));
            }

            var source = ParseBlockchainType(sourceBlockchain);
            var target = ParseBlockchainType(targetBlockchain);
            var result = await _crossChainService.ExecuteContractCallAsync(request, source, target);

            Logger.LogInformation("Executed cross-chain contract call {ExecutionId} from {SourceBlockchain} to {TargetBlockchain} by user {UserId}",
                result.ExecutionId, sourceBlockchain, targetBlockchain, GetCurrentUserId());

            return Ok(CreateResponse(result, "Contract call executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteContractCall");
        }
    }

    /// <summary>
    /// Gets transaction history for an address.
    /// </summary>
    /// <param name="address">The address to get history for.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The transaction history.</returns>
    /// <response code="200">Transaction history retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("transaction-history/{address}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CrossChainTransaction>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetTransactionHistory(
        [FromRoute] string address,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var history = await _crossChainService.GetTransactionHistoryAsync(address, blockchain);

            Logger.LogInformation("Retrieved {TransactionCount} transactions for address {Address} on {BlockchainType} by user {UserId}",
                history.Count(), address, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(history, "Transaction history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetTransactionHistory");
        }
    }
} 