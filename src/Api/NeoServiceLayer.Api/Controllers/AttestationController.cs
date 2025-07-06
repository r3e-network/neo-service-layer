using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Enclave.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Controller for SGX enclave attestation operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attestation")]
[Authorize]
[Tags("Attestation")]
public class AttestationController : BaseApiController
{
    private readonly IAttestationService _attestationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttestationController"/> class.
    /// </summary>
    /// <param name="attestationService">The attestation service.</param>
    /// <param name="logger">The logger.</param>
    public AttestationController(IAttestationService attestationService, ILogger<AttestationController> logger)
        : base(logger)
    {
        _attestationService = attestationService ?? throw new ArgumentNullException(nameof(attestationService));
    }

    /// <summary>
    /// Generates an SGX enclave attestation quote.
    /// </summary>
    /// <param name="request">The attestation request.</param>
    /// <returns>The attestation quote.</returns>
    /// <response code="200">Attestation generated successfully.</response>
    /// <response code="400">Invalid attestation parameters.</response>
    /// <response code="503">SGX hardware not available.</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ApiResponse<AttestationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 503)]
    public async Task<IActionResult> GenerateAttestation(
        [FromBody] AttestationRequest request)
    {
        try
        {
            var result = await _attestationService.GenerateAttestationAsync(request);

            Logger.LogInformation("Generated SGX attestation quote {QuoteId} for enclave {EnclaveId}",
                result.QuoteId, request.EnclaveId);

            return Ok(CreateResponse(result, "SGX attestation generated successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("SGX"))
        {
            return StatusCode(503, CreateErrorResponse("SGX hardware not available or enclave not initialized"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "generating attestation");
        }
    }

    /// <summary>
    /// Verifies an SGX attestation quote.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <returns>The verification result.</returns>
    /// <response code="200">Verification completed successfully.</response>
    /// <response code="400">Invalid quote format.</response>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<AttestationVerificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> VerifyAttestation(
        [FromBody] AttestationVerificationRequest request)
    {
        try
        {
            var result = await _attestationService.VerifyAttestationAsync(request);

            Logger.LogInformation("Verified SGX attestation: Valid={IsValid}, TrustLevel={TrustLevel}",
                result.IsValid, result.TrustLevel);

            return Ok(CreateResponse(result, "Attestation verification completed"));
        }
        catch (FormatException ex)
        {
            return BadRequest(CreateErrorResponse($"Invalid quote format: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "verifying attestation");
        }
    }

    /// <summary>
    /// Gets the platform configuration for SGX attestation.
    /// </summary>
    /// <returns>Platform configuration details.</returns>
    /// <response code="200">Platform config retrieved successfully.</response>
    [HttpGet("platform-config")]
    [ProducesResponseType(typeof(ApiResponse<PlatformConfiguration>), 200)]
    public async Task<IActionResult> GetPlatformConfiguration()
    {
        try
        {
            var result = await _attestationService.GetPlatformConfigurationAsync();

            return Ok(CreateResponse(result, "Platform configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving platform configuration");
        }
    }

    /// <summary>
    /// Gets attestation details by quote ID.
    /// </summary>
    /// <param name="quoteId">The quote ID.</param>
    /// <returns>Attestation details.</returns>
    /// <response code="200">Attestation details retrieved successfully.</response>
    /// <response code="404">Quote not found.</response>
    [HttpGet("quotes/{quoteId}")]
    [ProducesResponseType(typeof(ApiResponse<AttestationDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAttestationDetails(
        [FromRoute] string quoteId)
    {
        try
        {
            var result = await _attestationService.GetAttestationDetailsAsync(quoteId);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Attestation quote not found: {quoteId}"));
            }

            return Ok(CreateResponse(result, "Attestation details retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving attestation details");
        }
    }

    /// <summary>
    /// Lists recent attestations with filtering options.
    /// </summary>
    /// <param name="enclaveId">Filter by enclave ID.</param>
    /// <param name="status">Filter by verification status.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <returns>List of attestations.</returns>
    /// <response code="200">Attestations retrieved successfully.</response>
    [HttpGet("quotes")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AttestationSummary>>), 200)]
    public async Task<IActionResult> ListAttestations(
        [FromQuery] string? enclaveId = null,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var filter = new AttestationFilter
            {
                EnclaveId = enclaveId,
                Status = status,
                Limit = Math.Min(limit, 100) // Cap at 100
            };

            var result = await _attestationService.ListAttestationsAsync(filter);

            return Ok(CreateResponse(result, "Attestations retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving attestations");
        }
    }

    /// <summary>
    /// Revokes an attestation quote.
    /// </summary>
    /// <param name="quoteId">The quote ID to revoke.</param>
    /// <param name="reason">Reason for revocation.</param>
    /// <returns>Revocation result.</returns>
    /// <response code="200">Quote revoked successfully.</response>
    /// <response code="404">Quote not found.</response>
    [HttpPost("quotes/{quoteId}/revoke")]
    [Authorize(Roles = "Admin,Security")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RevokeAttestation(
        [FromRoute] string quoteId,
        [FromBody] RevocationRequest request)
    {
        try
        {
            var result = await _attestationService.RevokeAttestationAsync(quoteId, request.Reason);

            if (!result)
            {
                return NotFound(CreateErrorResponse($"Attestation quote not found: {quoteId}"));
            }

            Logger.LogWarning("Revoked SGX attestation {QuoteId} - Reason: {Reason}",
                quoteId, request.Reason);

            return Ok(CreateResponse(result, "Attestation revoked successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "revoking attestation");
        }
    }

    /// <summary>
    /// Gets SGX enclave measurements and metadata.
    /// </summary>
    /// <param name="enclaveId">The enclave ID.</param>
    /// <returns>Enclave measurements.</returns>
    /// <response code="200">Measurements retrieved successfully.</response>
    /// <response code="404">Enclave not found.</response>
    [HttpGet("enclaves/{enclaveId}/measurements")]
    [ProducesResponseType(typeof(ApiResponse<EnclaveMeasurements>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetEnclaveMeasurements(
        [FromRoute] string enclaveId)
    {
        try
        {
            var result = await _attestationService.GetEnclaveMeasurementsAsync(enclaveId);

            if (result == null)
            {
                return NotFound(CreateErrorResponse($"Enclave not found: {enclaveId}"));
            }

            return Ok(CreateResponse(result, "Enclave measurements retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving enclave measurements");
        }
    }

    /// <summary>
    /// Gets attestation service health and status.
    /// </summary>
    /// <returns>Service health information.</returns>
    /// <response code="200">Health status retrieved successfully.</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<AttestationServiceHealth>), 200)]
    public async Task<IActionResult> GetServiceHealth()
    {
        try
        {
            var result = await _attestationService.GetServiceHealthAsync();

            return Ok(CreateResponse(result, "Attestation service health retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving service health");
        }
    }

    /// <summary>
    /// Updates the trusted enclave policy.
    /// </summary>
    /// <param name="policy">The new enclave policy.</param>
    /// <returns>Update result.</returns>
    /// <response code="200">Policy updated successfully.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPut("policy")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> UpdateEnclavePolicy(
        [FromBody] EnclavePolicy policy)
    {
        try
        {
            var result = await _attestationService.UpdateEnclavePolicyAsync(policy);

            Logger.LogInformation("Updated SGX enclave policy - MinTrustLevel: {MinTrustLevel}",
                policy.MinimumTrustLevel);

            return Ok(CreateResponse(result, "Enclave policy updated successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, CreateErrorResponse("Insufficient permissions to update enclave policy"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "updating enclave policy");
        }
    }
}

/// <summary>
/// Request model for attestation revocation.
/// </summary>
public class RevocationRequest
{
    /// <summary>
    /// Gets or sets the reason for revocation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}