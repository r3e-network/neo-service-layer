using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.ProofOfReserve.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for proof of reserve operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proof-of-reserve")]
[Authorize]
[Tags("Proof of Reserve")]
public class ProofOfReserveController : BaseApiController
{
    private readonly IProofOfReserveService _proofOfReserveService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveController"/> class.
    /// </summary>
    /// <param name="proofOfReserveService">The proof of reserve service.</param>
    /// <param name="logger">The logger.</param>
    public ProofOfReserveController(IProofOfReserveService proofOfReserveService, ILogger<ProofOfReserveController> logger)
        : base(logger)
    {
        _proofOfReserveService = proofOfReserveService ?? throw new ArgumentNullException(nameof(proofOfReserveService));
    }

    /// <summary>
    /// Generates a proof of reserve attestation.
    /// </summary>
    /// <param name="request">The proof generation request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The generated proof.</returns>
    /// <response code="200">Proof generated successfully.</response>
    /// <response code="400">Invalid proof parameters.</response>
    [HttpPost("{blockchainType}/generate")]
    [Authorize(Roles = "Admin,Auditor")]
    [ProducesResponseType(typeof(ApiResponse<ProofOfReserveResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GenerateProof(
        [FromBody] ProofOfReserveRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.GenerateProofAsync(request, blockchain);

            Logger.LogInformation("Generated proof of reserve {ProofId} for {AssetType}",
                result.ProofId, request.AssetType);

            return Ok(CreateResponse(result, "Proof of reserve generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "generating proof of reserve");
        }
    }

    /// <summary>
    /// Verifies a proof of reserve.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    /// <response code="400">Invalid verification parameters.</response>
    [HttpPost("{blockchainType}/verify")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> VerifyProof(
        [FromBody] VerifyProofRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.VerifyProofAsync(request, blockchain);

            Logger.LogInformation("Verified proof {ProofId}: Valid={IsValid}",
                request.ProofId, result.IsValid);

            return Ok(CreateResponse(result, "Proof verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "verifying proof");
        }
    }

    /// <summary>
    /// Gets proof details by ID.
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The proof details.</returns>
    /// <response code="200">Proof retrieved successfully.</response>
    /// <response code="404">Proof not found.</response>
    [HttpGet("{blockchainType}/{proofId}")]
    [ProducesResponseType(typeof(ApiResponse<ProofDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetProof(
        [FromRoute] string proofId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.GetProofAsync(proofId, blockchain);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Proof not found: {proofId}"));
            }

            return Ok(CreateResponse(result, "Proof retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving proof");
        }
    }

    /// <summary>
    /// Gets proofs for a specific asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <param name="limit">Maximum number of proofs to return.</param>
    /// <returns>List of proofs.</returns>
    /// <response code="200">Proofs retrieved successfully.</response>
    [HttpGet("{blockchainType}/asset/{assetId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProofSummary>>), 200)]
    public async Task<IActionResult> GetProofsByAsset(
        [FromRoute] string assetId,
        [FromRoute] string blockchainType,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.GetProofsByAssetAsync(assetId, blockchain, limit);

            return Ok(CreateResponse(result, "Proofs retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving proofs by asset");
        }
    }

    /// <summary>
    /// Publishes a proof on-chain.
    /// </summary>
    /// <param name="request">The publish request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The publication result.</returns>
    /// <response code="200">Proof published successfully.</response>
    /// <response code="404">Proof not found.</response>
    [HttpPost("{blockchainType}/publish")]
    [Authorize(Roles = "Admin,Publisher")]
    [ProducesResponseType(typeof(ApiResponse<PublishResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> PublishProof(
        [FromBody] PublishProofRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.PublishProofAsync(request, blockchain);

            Logger.LogInformation("Published proof {ProofId} on-chain: TxHash={TransactionHash}",
                request.ProofId, result.TransactionHash);

            return Ok(CreateResponse(result, "Proof published successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "publishing proof");
        }
    }

    /// <summary>
    /// Gets aggregated reserve statistics.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>Reserve statistics.</returns>
    /// <response code="200">Statistics retrieved successfully.</response>
    [HttpGet("{blockchainType}/statistics")]
    [ProducesResponseType(typeof(ApiResponse<ReserveStatistics>), 200)]
    public async Task<IActionResult> GetStatistics(
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.GetStatisticsAsync(blockchain);

            return Ok(CreateResponse(result, "Statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving statistics");
        }
    }

    /// <summary>
    /// Schedules periodic proof generation.
    /// </summary>
    /// <param name="request">The schedule request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The schedule details.</returns>
    /// <response code="200">Schedule created successfully.</response>
    [HttpPost("{blockchainType}/schedule")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResult>), 200)]
    public async Task<IActionResult> ScheduleProofGeneration(
        [FromBody] ScheduleProofRequest request,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.ScheduleProofGenerationAsync(request, blockchain);

            Logger.LogInformation("Scheduled proof generation {ScheduleId} for {AssetType}",
                result.ScheduleId, request.AssetType);

            return Ok(CreateResponse(result, "Proof generation scheduled successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "scheduling proof generation");
        }
    }
}