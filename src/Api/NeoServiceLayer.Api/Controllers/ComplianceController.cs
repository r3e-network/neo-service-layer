using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Services.Compliance;
using NeoServiceLayer.Services.Compliance.Models;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// API controller for compliance and regulatory operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Compliance")]
public class ComplianceController : BaseApiController
{
    private readonly IComplianceService _complianceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="complianceService">The compliance service.</param>
    public ComplianceController(ILogger<ComplianceController> logger, IComplianceService complianceService)
        : base(logger)
    {
        _complianceService = complianceService;
    }

    /// <summary>
    /// Performs AML (Anti-Money Laundering) checks on a transaction.
    /// </summary>
    /// <param name="request">The AML check request.</param>
    /// <returns>The AML check results.</returns>
    [HttpPost("aml-check")]
    [Authorize(Roles = "Admin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ApiResponse<AmlCheckResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> PerformAmlCheck([FromBody] AmlCheckRequest request)
    {
        try
        {
            Logger.LogInformation("Performing AML check for user {UserId}", GetCurrentUserId());
            
            var result = await _complianceService.PerformAmlCheckAsync(request);
            return Ok(CreateResponse(result, "AML check completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "PerformAmlCheck");
        }
    }

    /// <summary>
    /// Performs KYC (Know Your Customer) verification.
    /// </summary>
    /// <param name="request">The KYC verification request.</param>
    /// <returns>The KYC verification results.</returns>
    [HttpPost("kyc-verification")]
    [Authorize(Roles = "Admin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ApiResponse<KycVerificationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> PerformKycVerification([FromBody] KycVerificationRequest request)
    {
        try
        {
            Logger.LogInformation("Performing KYC verification for user {UserId}", GetCurrentUserId());
            
            var result = await _complianceService.PerformKycVerificationAsync(request);
            return Ok(CreateResponse(result, "KYC verification completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "PerformKycVerification");
        }
    }

    /// <summary>
    /// Generates a compliance report.
    /// </summary>
    /// <param name="request">The report generation request.</param>
    /// <returns>The generated compliance report.</returns>
    [HttpPost("reports")]
    [Authorize(Roles = "Admin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceReportResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequest request)
    {
        try
        {
            Logger.LogInformation("Generating compliance report for user {UserId}", GetCurrentUserId());
            
            var result = await _complianceService.GenerateComplianceReportAsync(request);
            return Ok(CreateResponse(result, "Compliance report generated successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GenerateComplianceReport");
        }
    }

    /// <summary>
    /// Gets the compliance status of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="entityType">The type of entity (address, account, etc.).</param>
    /// <returns>The compliance status.</returns>
    [HttpGet("status/{entityId}")]
    [Authorize(Roles = "Admin,ComplianceOfficer,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceStatusResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetComplianceStatus(string entityId, [FromQuery] string entityType = "address")
    {
        try
        {
            var result = await _complianceService.GetComplianceStatusAsync(entityId, entityType);
            return Ok(CreateResponse(result, "Compliance status retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetComplianceStatus");
        }
    }

    /// <summary>
    /// Submits a Suspicious Activity Report (SAR).
    /// </summary>
    /// <param name="request">The SAR submission request.</param>
    /// <returns>The SAR submission result.</returns>
    [HttpPost("sar")]
    [Authorize(Roles = "Admin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ApiResponse<SarSubmissionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> SubmitSuspiciousActivityReport([FromBody] SarSubmissionRequest request)
    {
        try
        {
            Logger.LogInformation("Submitting SAR for user {UserId}", GetCurrentUserId());
            
            var result = await _complianceService.SubmitSuspiciousActivityReportAsync(request);
            return Ok(CreateResponse(result, "Suspicious Activity Report submitted successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "SubmitSuspiciousActivityReport");
        }
    }

    /// <summary>
    /// Gets available compliance jurisdictions and their requirements.
    /// </summary>
    /// <returns>The list of supported jurisdictions.</returns>
    [HttpGet("jurisdictions")]
    [Authorize(Roles = "Admin,ComplianceOfficer,ServiceUser")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ComplianceJurisdiction>>), 200)]
    public async Task<IActionResult> GetComplianceJurisdictions()
    {
        try
        {
            var result = await _complianceService.GetSupportedJurisdictionsAsync();
            return Ok(CreateResponse(result, "Compliance jurisdictions retrieved successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "GetComplianceJurisdictions");
        }
    }
}