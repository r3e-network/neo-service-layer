using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for compliance and identity verification.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ComplianceController : ControllerBase
    {
        private readonly IComplianceService _complianceService;
        private readonly ILogger<ComplianceController> _logger;

        /// <summary>
        /// Initializes a new instance of the ComplianceController class.
        /// </summary>
        /// <param name="complianceService">The compliance service.</param>
        /// <param name="logger">The logger.</param>
        public ComplianceController(IComplianceService complianceService, ILogger<ComplianceController> logger)
        {
            _complianceService = complianceService ?? throw new ArgumentNullException(nameof(complianceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verifies an identity.
        /// </summary>
        /// <param name="request">The request to verify an identity.</param>
        /// <returns>The verification ID.</returns>
        [HttpPost("verify")]
        [HttpPost("identity")]
        [ProducesResponseType(typeof(ApiResponse<VerifyIdentityResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<VerifyIdentityResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<VerifyIdentityResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<VerifyIdentityResponse>), 500)]
        public async Task<IActionResult> VerifyIdentity([FromBody] VerifyIdentityRequest request)
        {
            try
            {
                _logger.LogInformation("Verifying identity with type {VerificationType}", request.VerificationType);

                if (string.IsNullOrEmpty(request.IdentityData))
                {
                    _logger.LogWarning("Identity data is null or empty");
                    return BadRequest(ApiResponse<VerifyIdentityResponse>.CreateError(ApiErrorCodes.ValidationError, "Identity data is required."));
                }

                if (string.IsNullOrEmpty(request.VerificationType))
                {
                    _logger.LogWarning("Verification type is null or empty");
                    return BadRequest(ApiResponse<VerifyIdentityResponse>.CreateError(ApiErrorCodes.ValidationError, "Verification type is required."));
                }

                var verificationId = await _complianceService.VerifyIdentityAsync(request.IdentityData, request.VerificationType);

                var response = new VerifyIdentityResponse
                {
                    VerificationId = verificationId,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Identity verification initiated with ID {VerificationId}", verificationId);

                return Ok(ApiResponse<VerifyIdentityResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying identity");
                return StatusCode(500, ApiResponse<VerifyIdentityResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while verifying the identity."));
            }
        }

        /// <summary>
        /// Gets the result of an identity verification.
        /// </summary>
        /// <param name="verificationId">The ID of the verification to get.</param>
        /// <returns>The verification result.</returns>
        [HttpGet("verify/{verificationId}")]
        [HttpGet("verification/{verificationId}")]
        [HttpGet("identity/{verificationId}")]
        [ProducesResponseType(typeof(ApiResponse<VerificationResultResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<VerificationResultResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<VerificationResultResponse>), 404)]
        [ProducesResponseType(typeof(ApiResponse<VerificationResultResponse>), 500)]
        public async Task<IActionResult> GetVerificationResult(string verificationId)
        {
            try
            {
                _logger.LogInformation("Getting verification result for {VerificationId}", verificationId);

                var result = await _complianceService.GetVerificationResultAsync(verificationId);

                if (result == null)
                {
                    _logger.LogWarning("Verification {VerificationId} not found", verificationId);
                    return NotFound(ApiResponse<VerificationResultResponse>.CreateError(ApiErrorCodes.ResourceNotFound, $"Verification with ID {verificationId} not found."));
                }

                var response = new VerificationResultResponse
                {
                    VerificationId = result.VerificationId,
                    Status = result.Status,
                    Result = new VerificationResultData
                    {
                        Verified = result.Verified ?? false,
                        Score = result.Score ?? 0.0
                    },
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5), // Simulated creation time
                    CompletedAt = result.Status == "completed" ? DateTime.UtcNow : (DateTime?)null
                };

                _logger.LogInformation("Verification result for {VerificationId} retrieved successfully", verificationId);

                return Ok(ApiResponse<VerificationResultResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification result for {VerificationId}", verificationId);
                return StatusCode(500, ApiResponse<VerificationResultResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the verification result."));
            }
        }

        /// <summary>
        /// Checks if a transaction complies with regulations.
        /// </summary>
        /// <param name="request">The request to check transaction compliance.</param>
        /// <returns>The compliance check result.</returns>
        [HttpPost("transaction")]
        [ProducesResponseType(typeof(ApiResponse<ComplianceCheckResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ComplianceCheckResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ComplianceCheckResponse>), 401)]
        [ProducesResponseType(typeof(ApiResponse<ComplianceCheckResponse>), 500)]
        public async Task<IActionResult> CheckTransactionCompliance([FromBody] CheckTransactionComplianceRequest request)
        {
            try
            {
                _logger.LogInformation("Checking transaction compliance");

                if (string.IsNullOrEmpty(request.TransactionData))
                {
                    _logger.LogWarning("Transaction data is null or empty");
                    return BadRequest(ApiResponse<ComplianceCheckResponse>.CreateError(ApiErrorCodes.ValidationError, "Transaction data is required."));
                }

                var result = await _complianceService.CheckTransactionComplianceAsync(request.TransactionData);

                var response = new ComplianceCheckResponse
                {
                    Compliant = result.Compliant,
                    Reason = result.Reason,
                    RiskScore = result.RiskScore,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Transaction compliance check completed with result: {IsCompliant}", result.Compliant);

                return Ok(ApiResponse<ComplianceCheckResponse>.CreateSuccess(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transaction compliance");
                return StatusCode(500, ApiResponse<ComplianceCheckResponse>.CreateError(ApiErrorCodes.InternalError, "An error occurred while checking transaction compliance."));
            }
        }
    }

    /// <summary>
    /// Represents a request to verify an identity.
    /// </summary>
    public class VerifyIdentityRequest
    {
        /// <summary>
        /// Gets or sets the encrypted identity data to verify.
        /// </summary>
        public string IdentityData { get; set; }

        /// <summary>
        /// Gets or sets the type of verification to perform.
        /// </summary>
        public string VerificationType { get; set; }
    }

    /// <summary>
    /// Represents a response to a verify identity request.
    /// </summary>
    public class VerifyIdentityResponse
    {
        /// <summary>
        /// Gets or sets the ID of the verification.
        /// </summary>
        public string VerificationId { get; set; }

        /// <summary>
        /// Gets or sets the status of the verification.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a response to a get verification result request.
    /// </summary>
    public class VerificationResultResponse
    {
        /// <summary>
        /// Gets or sets the ID of the verification.
        /// </summary>
        public string VerificationId { get; set; }

        /// <summary>
        /// Gets or sets the status of the verification.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the result of the verification.
        /// </summary>
        public VerificationResultData Result { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the verification was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Represents the data in a verification result.
    /// </summary>
    public class VerificationResultData
    {
        /// <summary>
        /// Gets or sets whether the identity was verified.
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>
        /// Gets or sets the verification score.
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// Represents a request to check transaction compliance.
    /// </summary>
    public class CheckTransactionComplianceRequest
    {
        /// <summary>
        /// Gets or sets the encrypted transaction data to check.
        /// </summary>
        public string TransactionData { get; set; }
    }

    /// <summary>
    /// Represents a response to a check transaction compliance request.
    /// </summary>
    public class ComplianceCheckResponse
    {
        /// <summary>
        /// Gets or sets whether the transaction complies with regulations.
        /// </summary>
        public bool Compliant { get; set; }

        /// <summary>
        /// Gets or sets the reason for the compliance check result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the risk score of the transaction.
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the compliance check.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
