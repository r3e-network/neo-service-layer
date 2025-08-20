using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for High-Performance Verifiable Compute operations.
/// </summary>
[Tags("Compute")]
public class ComputeController : BaseApiController
{
    private readonly IComputeService _computeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeController"/> class.
    /// </summary>
    /// <param name="computeService">The compute service.</param>
    /// <param name="logger">The logger.</param>
    public ComputeController(
        IComputeService computeService,
        ILogger<ComputeController> logger) : base(logger)
    {
        _computeService = computeService;
    }

    /// <summary>
    /// Executes a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="parameters">The computation parameters.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation result.</returns>
    /// <response code="200">Computation executed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Computation not found.</response>
    /// <response code="500">Computation execution failed.</response>
    [HttpPost("execute/{computationId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ComputationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ExecuteComputation(
        [FromRoute] string computationId,
        [FromBody] Dictionary<string, string> parameters,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.ExecuteComputationAsync(computationId, parameters, blockchain);

            Logger.LogInformation("Executed computation {ComputationId} for user {UserId} on {BlockchainType}",
                computationId, GetCurrentUserId(), blockchainType);

            return Ok(CreateResponse(result, "Computation executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ExecuteComputation");
        }
    }

    /// <summary>
    /// Gets the status of a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation status.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Computation not found.</response>
    [HttpGet("status/{computationId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<Core.ComputationStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetComputationStatus(
        [FromRoute] string computationId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var status = await _computeService.GetComputationStatusAsync(computationId, blockchain);

            return Ok(CreateResponse(status, "Computation status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetComputationStatus");
        }
    }

    /// <summary>
    /// Registers a new computation.
    /// </summary>
    /// <param name="request">The computation registration request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The registration result.</returns>
    /// <response code="200">Computation registered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Registration failed.</response>
    [HttpPost("register/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RegisterComputation(
        [FromBody] RegisterComputationRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.RegisterComputationAsync(
                request.ComputationId,
                request.ComputationCode,
                request.ComputationType,
                request.Description,
                blockchain);

            Logger.LogInformation("Registered computation {ComputationId} by user {UserId}",
                request.ComputationId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Computation registered successfully" : "Failed to register computation"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RegisterComputation");
        }
    }

    /// <summary>
    /// Unregisters a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The unregistration result.</returns>
    /// <response code="200">Computation unregistered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Computation not found.</response>
    [HttpDelete("{computationId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UnregisterComputation(
        [FromRoute] string computationId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.UnregisterComputationAsync(computationId, blockchain);

            Logger.LogInformation("Unregistered computation {ComputationId} by user {UserId}",
                computationId, GetCurrentUserId());

            return Ok(CreateResponse(result, result ? "Computation unregistered successfully" : "Failed to unregister computation"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UnregisterComputation");
        }
    }

    /// <summary>
    /// Lists registered computations.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">The number of computations to skip (default: 0).</param>
    /// <param name="take">The number of computations to take (default: 20, max: 100).</param>
    /// <returns>The list of computations.</returns>
    /// <response code="200">Computations retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("list/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ComputationMetadata>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> ListComputations(
        [FromRoute] string blockchainType,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (skip < 0)
            {
                return BadRequest(CreateErrorResponse("Skip must be non-negative"));
            }

            if (take < 1 || take > 100)
            {
                return BadRequest(CreateErrorResponse("Take must be between 1 and 100"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var computations = await _computeService.ListComputationsAsync(skip, take, blockchain);

            return Ok(CreateResponse(computations, "Computations retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "ListComputations");
        }
    }

    /// <summary>
    /// Gets computation metadata.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation metadata.</returns>
    /// <response code="200">Metadata retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Computation not found.</response>
    [HttpGet("metadata/{computationId}/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ComputationMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetComputationMetadata(
        [FromRoute] string computationId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var metadata = await _computeService.GetComputationMetadataAsync(computationId, blockchain);

            return Ok(CreateResponse(metadata, "Computation metadata retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetComputationMetadata");
        }
    }

    /// <summary>
    /// Verifies a computation result.
    /// </summary>
    /// <param name="result">The computation result to verify.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost("verify/{blockchainType}")]
    [Authorize(Roles = "Admin,KeyManager,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> VerifyComputationResult(
        [FromBody] ComputationResult result,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var isValid = await _computeService.VerifyComputationResultAsync(result, blockchain);

            Logger.LogInformation("Verified computation result {ResultId} for computation {ComputationId}: {IsValid}",
                result.ResultId, result.ComputationId, isValid);

            return Ok(CreateResponse(isValid, isValid ? "Result verification passed" : "Result verification failed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "VerifyComputationResult");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for registering a new computation.
/// </summary>
public class RegisterComputationRequest
{
    /// <summary>
    /// Gets or sets the computation ID.
    /// </summary>
    public string ComputationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation code.
    /// </summary>
    public string ComputationCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation type.
    /// </summary>
    public string ComputationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computation description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

#endregion
