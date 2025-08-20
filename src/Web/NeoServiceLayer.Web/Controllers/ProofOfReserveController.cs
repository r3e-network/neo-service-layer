using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.ProofOfReserve.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// API controller for proof of reserve operations.
/// </summary>
[Tags("Proof of Reserve")]
public class ProofOfReserveController : BaseApiController
{
    private readonly IProofOfReserveService _proofOfReserveService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofOfReserveController"/> class.
    /// </summary>
    /// <param name="proofOfReserveService">The proof of reserve service.</param>
    /// <param name="logger">The logger.</param>
    public ProofOfReserveController(
        IProofOfReserveService proofOfReserveService,
        ILogger<ProofOfReserveController> logger) : base(logger)
    {
        _proofOfReserveService = proofOfReserveService;
    }

    /// <summary>
    /// Registers an asset for proof of reserve monitoring.
    /// </summary>
    /// <param name="registration">The asset registration request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The asset ID.</returns>
    /// <response code="200">Asset registered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Asset registration failed.</response>
    [HttpPost("register-asset/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> RegisterAsset(
        [FromBody] AssetRegistrationRequest registration,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var assetId = await _proofOfReserveService.RegisterAssetAsync(registration, blockchain);

            Logger.LogInformation("Registered asset {AssetId} for proof of reserve on {BlockchainType} by user {UserId}",
                assetId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(assetId, "Asset registered for proof of reserve successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "RegisterAsset");
        }
    }

    /// <summary>
    /// Updates reserve data for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="data">The reserve update request.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The update result.</returns>
    /// <response code="200">Reserve data updated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    /// <response code="500">Update failed.</response>
    [HttpPut("update-reserve/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> UpdateReserveData(
        [FromRoute] string assetId,
        [FromBody] ReserveUpdateRequest data,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.UpdateReserveDataAsync(assetId, data, blockchain);

            Logger.LogInformation("Updated reserve data for asset {AssetId} on {BlockchainType} by user {UserId}",
                assetId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Reserve data updated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "UpdateReserveData");
        }
    }

    /// <summary>
    /// Generates a proof of reserve for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The proof of reserve.</returns>
    /// <response code="200">Proof generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    /// <response code="500">Proof generation failed.</response>
    [HttpPost("generate-proof/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<NeoServiceLayer.Services.ProofOfReserve.Models.ProofOfReserve>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateProof(
        [FromRoute] string assetId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var proof = await _proofOfReserveService.GenerateProofAsync(assetId, blockchain);

            Logger.LogInformation("Generated proof of reserve for asset {AssetId} on {BlockchainType} by user {UserId}",
                assetId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(proof, "Proof of reserve generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateProof");
        }
    }

    /// <summary>
    /// Verifies a proof of reserve.
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Proof verified successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Proof not found.</response>
    [HttpPost("verify-proof/{proofId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> VerifyProof(
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
            var isValid = await _proofOfReserveService.VerifyProofAsync(proofId, blockchain);

            Logger.LogInformation("Verified proof {ProofId} on {BlockchainType}: {IsValid} by user {UserId}",
                proofId, blockchainType, isValid, GetCurrentUserId());

            return Ok(CreateResponse(isValid, "Proof verification completed"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "VerifyProof");
        }
    }

    /// <summary>
    /// Gets the reserve status for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The reserve status information.</returns>
    /// <response code="200">Reserve status retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    [HttpGet("reserve-status/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ReserveStatusInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetReserveStatus(
        [FromRoute] string assetId,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var status = await _proofOfReserveService.GetReserveStatusAsync(assetId, blockchain);

            Logger.LogInformation("Retrieved reserve status for asset {AssetId} on {BlockchainType} by user {UserId}",
                assetId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(status, "Reserve status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetReserveStatus");
        }
    }

    /// <summary>
    /// Gets all registered assets for proof of reserve monitoring.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The list of registered assets.</returns>
    /// <response code="200">Registered assets retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("registered-assets/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NeoServiceLayer.Services.ProofOfReserve.Models.MonitoredAsset>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetRegisteredAssets([FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var assets = await _proofOfReserveService.GetRegisteredAssetsAsync(blockchain);

            Logger.LogInformation("Retrieved {AssetCount} registered assets on {BlockchainType} by user {UserId}",
                assets.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(assets, "Registered assets retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetRegisteredAssets");
        }
    }

    /// <summary>
    /// Gets the reserve history for an asset within a date range.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The reserve history snapshots.</returns>
    /// <response code="200">Reserve history retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    [HttpGet("reserve-history/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<NeoServiceLayer.Services.ProofOfReserve.Models.ReserveSnapshot[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetReserveHistory(
        [FromRoute] string assetId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (from >= to)
            {
                return BadRequest(CreateErrorResponse("From date must be earlier than to date"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var history = await _proofOfReserveService.GetReserveHistoryAsync(assetId, from, to, blockchain);

            Logger.LogInformation("Retrieved reserve history for asset {AssetId} from {From} to {To} on {BlockchainType} by user {UserId}",
                assetId, from, to, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(history, "Reserve history retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetReserveHistory");
        }
    }

    /// <summary>
    /// Sets an alert threshold for an asset.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The threshold setting result.</returns>
    /// <response code="200">Alert threshold set successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    /// <response code="500">Threshold setting failed.</response>
    [HttpPost("set-alert-threshold/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SetAlertThreshold(
        [FromRoute] string assetId,
        [FromBody] decimal threshold,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (threshold < 0)
            {
                return BadRequest(CreateErrorResponse("Threshold must be non-negative"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var result = await _proofOfReserveService.SetAlertThresholdAsync(assetId, threshold, blockchain);

            Logger.LogInformation("Set alert threshold {Threshold} for asset {AssetId} on {BlockchainType} by user {UserId}",
                threshold, assetId, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(result, "Alert threshold set successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SetAlertThreshold");
        }
    }

    /// <summary>
    /// Gets active reserve alerts.
    /// </summary>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The active reserve alerts.</returns>
    /// <response code="200">Active alerts retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpGet("active-alerts/{blockchainType}")]
    [Authorize(Roles = "Admin,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReserveAlert>>), 200)]
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
            var alerts = await _proofOfReserveService.GetActiveAlertsAsync(blockchain);

            Logger.LogInformation("Retrieved {AlertCount} active reserve alerts on {BlockchainType} by user {UserId}",
                alerts.Count(), blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(alerts, "Active reserve alerts retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetActiveAlerts");
        }
    }

    /// <summary>
    /// Generates an audit report for an asset within a date range.
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="from">The start date.</param>
    /// <param name="to">The end date.</param>
    /// <param name="blockchainType">The blockchain type (NeoN3 or NeoX).</param>
    /// <returns>The audit report.</returns>
    /// <response code="200">Audit report generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Asset not found.</response>
    /// <response code="500">Report generation failed.</response>
    [HttpPost("generate-audit-report/{assetId}/{blockchainType}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AuditReport>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateAuditReport(
        [FromRoute] string assetId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromRoute] string blockchainType)
    {
        try
        {
            if (!IsValidBlockchainType(blockchainType))
            {
                return BadRequest(CreateErrorResponse($"Invalid blockchain type: {blockchainType}"));
            }

            if (from >= to)
            {
                return BadRequest(CreateErrorResponse("From date must be earlier than to date"));
            }

            var blockchain = ParseBlockchainType(blockchainType);
            var report = await _proofOfReserveService.GenerateAuditReportAsync(assetId, from, to, blockchain);

            Logger.LogInformation("Generated audit report for asset {AssetId} from {From} to {To} on {BlockchainType} by user {UserId}",
                assetId, from, to, blockchainType, GetCurrentUserId());

            return Ok(CreateResponse(report, "Audit report generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateAuditReport");
        }
    }
}
