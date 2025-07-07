using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Services.Compute.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for distributed compute operations within trusted execution environments.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compute")]
[Authorize]
[Tags("Compute")]
public class ComputeController : BaseApiController
{
    private readonly IComputeService _computeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeController"/> class.
    /// </summary>
    /// <param name="computeService">The compute service.</param>
    /// <param name="logger">The logger.</param>
    public ComputeController(IComputeService computeService, ILogger<ComputeController> logger)
        : base(logger)
    {
        _computeService = computeService ?? throw new ArgumentNullException(nameof(computeService));
    }

    /// <summary>
    /// Executes a computation in a trusted environment.
    /// </summary>
    /// <param name="request">The compute job request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation result.</returns>
    /// <response code="200">Computation executed successfully.</response>
    /// <response code="400">Invalid computation parameters.</response>
    [HttpPost("{blockchainType}/execute")]
    [ProducesResponseType(typeof(ApiResponse<ComputationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ExecuteComputation(
        [FromBody] ComputeJobRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var stringParameters = request.Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
            var result = await _computeService.ExecuteComputationAsync(request.JobId, stringParameters, blockchain);

            Logger.LogInformation("Executed computation {ComputationId} on {Blockchain}",
                request.JobId, blockchainType);

            return Ok(CreateResponse(result, "Computation executed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "executing computation");
        }
    }

    /// <summary>
    /// Gets the status of a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation status.</returns>
    /// <response code="200">Computation status retrieved successfully.</response>
    /// <response code="404">Computation not found.</response>
    [HttpGet("{blockchainType}/computations/{computationId}/status")]
    [ProducesResponseType(typeof(ApiResponse<ComputationStatus>), 200)]
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
            var result = await _computeService.GetComputationStatusAsync(computationId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Computation not found: {computationId}"));
            }

            return Ok(CreateResponse(result, "Computation status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving computation status");
        }
    }

    /// <summary>
    /// Registers a computation for future execution.
    /// </summary>
    /// <param name="request">The computation registration request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Registration result.</returns>
    /// <response code="200">Computation registered successfully.</response>
    /// <response code="400">Invalid computation parameters.</response>
    [HttpPost("{blockchainType}/register")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RegisterComputation(
        [FromBody] ComputeJobRequest request,
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
                request.JobId,
                request.Code,
                request.Language,
                $"Compute job {request.JobId}",
                blockchain);

            Logger.LogInformation("Registered computation {ComputationId} on {Blockchain}", request.JobId, blockchainType);
            return Ok(CreateResponse(result, "Computation registered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "registering computation");
        }
    }

    /// <summary>
    /// Gets metadata for a computation.
    /// </summary>
    /// <param name="computationId">The computation ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The computation metadata.</returns>
    /// <response code="200">Computation metadata retrieved successfully.</response>
    /// <response code="404">Computation not found.</response>
    [HttpGet("{blockchainType}/computations/{computationId}")]
    [ProducesResponseType(typeof(ApiResponse<ComputationMetadata>), 200)]
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
            var result = await _computeService.GetComputationMetadataAsync(computationId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Computation not found: {computationId}"));
            }

            return Ok(CreateResponse(result, "Computation metadata retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving computation metadata");
        }
    }

    /// <summary>
    /// Lists registered computations with pagination.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="skip">Number of computations to skip.</param>
    /// <param name="take">Number of computations to take.</param>
    /// <returns>List of computations.</returns>
    /// <response code="200">Computations retrieved successfully.</response>
    [HttpGet("{blockchainType}/computations")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ComputationMetadata>>), 200)]
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

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _computeService.ListComputationsAsync(skip, Math.Min(take, 100), blockchain);

            return Ok(CreateResponse(result, "Computations retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving computation list");
        }
    }

    /// <summary>
    /// Unregisters a computation.
    /// </summary>
    /// <param name="computationId">The computation ID to unregister.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Unregistration result.</returns>
    /// <response code="200">Computation unregistered successfully.</response>
    /// <response code="404">Computation not found.</response>
    [HttpDelete("{blockchainType}/computations/{computationId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
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

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Computation not found: {computationId}"));
            }

            Logger.LogInformation("Unregistered computation {ComputationId} on {Blockchain}", computationId, blockchainType);
            return Ok(CreateResponse(result, "Computation unregistered successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "unregistering computation");
        }
    }

    /// <summary>
    /// Verifies a computation result.
    /// </summary>
    /// <param name="result">The computation result to verify.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    [HttpPost("{blockchainType}/verify")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
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
            var verificationResult = await _computeService.VerifyComputationResultAsync(result, blockchain);

            return Ok(CreateResponse(verificationResult, "Computation result verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "verifying computation result");
        }
    }

}
