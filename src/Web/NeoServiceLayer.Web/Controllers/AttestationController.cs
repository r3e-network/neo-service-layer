using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Web.Models;

namespace NeoServiceLayer.Web.Controllers;

/// <summary>
/// Controller for attestation service operations.
/// </summary>
[Authorize]
[Tags("Attestation")]
public class AttestationController : BaseApiController
{
    private readonly IAttestationService _attestationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttestationController"/> class.
    /// </summary>
    public AttestationController(
        IAttestationService attestationService,
        ILogger<AttestationController> logger)
        : base(logger)
    {
        _attestationService = attestationService;
    }

    /// <summary>
    /// Generates an attestation report for the current enclave.
    /// </summary>
    /// <param name="request">The attestation generation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The generated attestation report.</returns>
    [HttpPost("generate/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<AttestationReport>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GenerateAttestation(
        [FromBody] GenerateAttestationRequest request,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var report = await _attestationService.GenerateAttestationReportAsync(request.UserData);

            return Ok(CreateSuccessResponse(report, "Attestation report generated successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate attestation report");
            return StatusCode(500, CreateErrorResponse("Failed to generate attestation report", ex.Message));
        }
    }

    /// <summary>
    /// Verifies an attestation report.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("verify/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<AttestationVerificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> VerifyAttestation(
        [FromBody] VerifyAttestationRequest request,
        [FromRoute] BlockchainType blockchainType)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateErrorResponse("Invalid request", GetModelStateErrors()));
            }

            var result = await _attestationService.VerifyAttestationAsync(request.AttestationReport);

            return Ok(CreateSuccessResponse(result, "Attestation verification completed"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to verify attestation");
            return StatusCode(500, CreateErrorResponse("Failed to verify attestation", ex.Message));
        }
    }

    /// <summary>
    /// Gets the current enclave status and measurements.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The enclave status.</returns>
    [HttpGet("status/{blockchainType}")]
    [ProducesResponseType(typeof(ApiResponse<EnclaveStatusResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetEnclaveStatus([FromRoute] BlockchainType blockchainType)
    {
        try
        {
            // In a real implementation, this would query the enclave status
            await Task.CompletedTask;

            var status = new EnclaveStatusResponse
            {
                EnclaveRunning = true,
                Measurements = new EnclaveMeasurements
                {
                    MrEnclave = "c29b7e7ba3ac...",
                    MrSigner = "83d719e77dea...",
                    IsvProdId = 1,
                    IsvSvn = 1
                },
                AttestationAvailable = true,
                LastAttestationTime = DateTime.UtcNow.AddHours(-1)
            };

            return Ok(CreateSuccessResponse(status, "Enclave status retrieved"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get enclave status");
            return StatusCode(500, CreateErrorResponse("Failed to get enclave status", ex.Message));
        }
    }
}

/// <summary>
/// Request to generate attestation.
/// </summary>
public class GenerateAttestationRequest
{
    /// <summary>
    /// Gets or sets the user data to include in the attestation.
    /// </summary>
    public byte[] UserData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the report type.
    /// </summary>
    public string ReportType { get; set; } = "QUOTE";

    /// <summary>
    /// Gets or sets the target info.
    /// </summary>
    public TargetInfo? TargetInfo { get; set; }
}

/// <summary>
/// Target information for attestation.
/// </summary>
public class TargetInfo
{
    /// <summary>
    /// Gets or sets the MR enclave.
    /// </summary>
    public string MrEnclave { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    public string Attributes { get; set; } = "0x03";
}

/// <summary>
/// Request to verify attestation.
/// </summary>
public class VerifyAttestationRequest
{
    /// <summary>
    /// Gets or sets the attestation report to verify.
    /// </summary>
    public string AttestationReport { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected measurement.
    /// </summary>
    public string? ExpectedMeasurement { get; set; }

    /// <summary>
    /// Gets or sets the nonce for verification.
    /// </summary>
    public string? Nonce { get; set; }
}

/// <summary>
/// Enclave status response.
/// </summary>
public class EnclaveStatusResponse
{
    /// <summary>
    /// Gets or sets whether the enclave is running.
    /// </summary>
    public bool EnclaveRunning { get; set; }

    /// <summary>
    /// Gets or sets the enclave measurements.
    /// </summary>
    public EnclaveMeasurements Measurements { get; set; } = new();

    /// <summary>
    /// Gets or sets whether attestation is available.
    /// </summary>
    public bool AttestationAvailable { get; set; }

    /// <summary>
    /// Gets or sets the last attestation time.
    /// </summary>
    public DateTime? LastAttestationTime { get; set; }
}

/// <summary>
/// Enclave measurements.
/// </summary>
public class EnclaveMeasurements
{
    /// <summary>
    /// Gets or sets the MR enclave value.
    /// </summary>
    public string MrEnclave { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MR signer value.
    /// </summary>
    public string MrSigner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISV product ID.
    /// </summary>
    public int IsvProdId { get; set; }

    /// <summary>
    /// Gets or sets the ISV SVN.
    /// </summary>
    public int IsvSvn { get; set; }
}
