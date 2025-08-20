using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for abstract account (ERC-4337) operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/abstract-account")]
[Authorize]
[Tags("Abstract Account")]
public class AbstractAccountController : BaseApiController
{
    private readonly IAbstractAccountService _abstractAccountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractAccountController"/> class.
    /// </summary>
    /// <param name="abstractAccountService">The abstract account service.</param>
    /// <param name="logger">The logger.</param>
    public AbstractAccountController(IAbstractAccountService abstractAccountService, ILogger<AbstractAccountController> logger)
        : base(logger)
    {
        _abstractAccountService = abstractAccountService ?? throw new ArgumentNullException(nameof(abstractAccountService));
    }

    /// <summary>
    /// Creates a new abstract account.
    /// </summary>
    /// <param name="request">The account creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created account details.</returns>
    /// <response code="200">Account created successfully.</response>
    /// <response code="400">Invalid account parameters.</response>
    [HttpPost("{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<AccountCreationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.CreateAccountAsync(request, blockchain);

            Logger.LogInformation("Created abstract account {AccountId} on {Blockchain}",
                result.AccountId, blockchainType);

            return Ok(CreateResponse(result, "Abstract account created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "creating abstract account");
        }
    }

    /// <summary>
    /// Gets abstract account details.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The account details.</returns>
    /// <response code="200">Account details retrieved successfully.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet("{blockchainType}/{accountId}")]
    [ProducesResponseType(typeof(ApiResponse<AbstractAccountInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAccount(
        [FromRoute] string accountId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.GetAccountInfoAsync(accountId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Account not found: {accountId}"));
            }

            return Ok(CreateResponse(result, "Account details retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving account details");
        }
    }

    /// <summary>
    /// Executes a user operation through the abstract account.
    /// </summary>
    /// <param name="request">The user operation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The operation result.</returns>
    /// <response code="200">Operation executed successfully.</response>
    /// <response code="400">Invalid operation parameters.</response>
    [HttpPost("{blockchainType}/execute")]
    [ProducesResponseType(typeof(ApiResponse<TransactionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExecuteUserOperation(
        [FromBody] UserOperationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var executeRequest = new ExecuteTransactionRequest
            {
                AccountId = request.Sender,
                Data = request.CallData,
                GasLimit = long.TryParse(request.MaxFeePerGas, out var gasLimit) ? gasLimit : 21000
            };
            var result = await _abstractAccountService.ExecuteTransactionAsync(executeRequest, blockchain);

            Logger.LogInformation("Executed transaction {TransactionHash} for account {AccountId}",
                result.TransactionHash, executeRequest.AccountId);

            return Ok(CreateResponse(result, "User operation executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "executing user operation");
        }
    }

    /// <summary>
    /// Adds a guardian to an abstract account.
    /// </summary>
    /// <param name="request">The guardian addition request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Success indicator.</returns>
    /// <response code="200">Guardian added successfully.</response>
    /// <response code="404">Account not found.</response>
    [HttpPost("{blockchainType}/guardians")]
    [Authorize(Roles = "Admin,AccountOwner")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AddGuardian(
        [FromBody] AddGuardianRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.AddGuardianAsync(request, blockchain);

            return Ok(CreateResponse(result, "Guardian added successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "adding guardian");
        }
    }

    /// <summary>
    /// Gets the operation history for an abstract account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="limit">Maximum number of operations to return.</param>
    /// <returns>The operation history.</returns>
    /// <response code="200">Operation history retrieved successfully.</response>
    [HttpGet("{blockchainType}/{accountId}/history")]
    [ProducesResponseType(typeof(ApiResponse<TransactionHistoryResult>), 200)]
    public async Task<IActionResult> GetOperationHistory(
        [FromRoute] string accountId,
        [FromRoute] string blockchainType,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var historyRequest = new TransactionHistoryRequest
            {
                AccountId = accountId,
                Limit = limit
            };
            var result = await _abstractAccountService.GetTransactionHistoryAsync(historyRequest, blockchain);

            return Ok(CreateResponse(result, "Operation history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving operation history");
        }
    }


}
