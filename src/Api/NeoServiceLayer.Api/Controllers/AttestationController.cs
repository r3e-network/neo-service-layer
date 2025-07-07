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
    [ProducesResponseType(typeof(ApiResponse<AttestationReport>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 503)]
    public async Task<IActionResult> GenerateAttestation(
        [FromBody] AttestationRequest request)
    {
        try
        {
            // Convert the request parameters to byte array (simplified approach)
            var userData = System.Text.Encoding.UTF8.GetBytes(request.Nonce);
            var result = await _attestationService.GenerateAttestationReportAsync(userData);

            Logger.LogInformation("Generated SGX attestation report for service {ServiceId}",
                request.ServiceId);

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
            var result = await _attestationService.VerifyAttestationAsync(request.AttestationToken);

            Logger.LogInformation("Verified SGX attestation: Valid={IsValid}", result.IsValid);

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
    [ProducesResponseType(typeof(ApiResponse<EnclaveInfo>), 200)]
    public async Task<IActionResult> GetPlatformConfiguration()
    {
        try
        {
            var result = await _attestationService.GetEnclaveInfoAsync();

            return Ok(CreateResponse(result, "Platform configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "retrieving platform configuration");
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
