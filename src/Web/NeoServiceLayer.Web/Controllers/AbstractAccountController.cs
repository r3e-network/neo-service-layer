using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.AbstractAccount.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for Abstract Account operations.
/// </summary>
[Tags("Abstract Account")]
public class AbstractAccountController : BaseApiController
{
    private readonly IAbstractAccountService _abstractAccountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractAccountController"/> class.
    /// </summary>
    /// <param name="abstractAccountService">The abstract account service.</param>
    /// <param name="logger">The logger.</param>
    public AbstractAccountController(
        IAbstractAccountService abstractAccountService,
        ILogger<AbstractAccountController> logger) : base(logger)
    {
        _abstractAccountService = abstractAccountService;
    }

    /// <summary>
    /// Creates a new abstract account.
    /// </summary>
    /// <param name="request">The account creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The created account information.</returns>
    /// <response code="200">Account created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Account creation failed.</response>
    [HttpPost("create/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<AbstractAccountResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
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

            Logger.LogInformation("Created abstract account {AccountId} for user {UserId} on {BlockchainType}",
                result.AccountId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Abstract account created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateAccount");
        }
    }

    /// <summary>
    /// Executes a transaction using an abstract account.
    /// </summary>
    /// <param name="request">The transaction execution request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The transaction execution result.</returns>
    /// <response code="200">Transaction executed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Transaction execution failed.</response>
    [HttpPost("execute-transaction/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<TransactionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteTransaction(
        [FromBody] ExecuteTransactionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.ExecuteTransactionAsync(request, blockchain);

            Logger.LogInformation("Executed transaction {TransactionHash} for account {AccountId} by user {UserId}",
                result.TransactionHash, request.AccountId, GetCurrentUserId());

            return Ok(CreateResponse(result, "Transaction executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteTransaction");
        }
    }

    /// <summary>
    /// Executes multiple transactions in a batch.
    /// </summary>
    /// <param name="request">The batch transaction request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The batch execution result.</returns>
    /// <response code="200">Batch execution completed.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Batch execution failed.</response>
    [HttpPost("execute-batch/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<BatchTransactionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteBatchTransaction(
        [FromBody] BatchTransactionRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.ExecuteBatchTransactionAsync(request, blockchain);

            Logger.LogInformation("Executed batch {BatchId} with {TransactionCount} transactions for account {AccountId}",
                result.BatchId, result.Results.Count, request.AccountId);

            return Ok(CreateResponse(result, "Batch transaction executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteBatchTransaction");
        }
    }

    /// <summary>
    /// Gets account information.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The account information.</returns>
    /// <response code="200">Account information retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet("{accountId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<AbstractAccountInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAccountInfo(
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

            return Ok(CreateResponse(result, "Account information retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetAccountInfo");
        }
    }

    /// <summary>
    /// Adds a guardian for social recovery.
    /// </summary>
    /// <param name="request">The add guardian request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The operation result.</returns>
    /// <response code="200">Guardian added successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Guardian addition failed.</response>
    [HttpPost("add-guardian/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<GuardianResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
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

            Logger.LogInformation("Added guardian {GuardianAddress} for account {AccountId}",
                request.GuardianAddress, request.AccountId);

            return Ok(CreateResponse(result, "Guardian added successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "AddGuardian");
        }
    }

    /// <summary>
    /// Creates a session key for limited-time operations.
    /// </summary>
    /// <param name="request">The session key creation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The session key result.</returns>
    /// <response code="200">Session key created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Session key creation failed.</response>
    [HttpPost("create-session-key/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<SessionKeyResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CreateSessionKey(
        [FromBody] CreateSessionKeyRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _abstractAccountService.CreateSessionKeyAsync(request, blockchain);

            Logger.LogInformation("Created session key {SessionKeyId} for account {AccountId}",
                result.SessionKeyId, request.AccountId);

            return Ok(CreateResponse(result, "Session key created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "CreateSessionKey");
        }
    }
}
