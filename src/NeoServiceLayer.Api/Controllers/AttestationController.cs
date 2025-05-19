using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Models
{
    public class AttestationProofResponse
    {
        public string Id { get; set; }
        public string Report { get; set; }
        public string Signature { get; set; }
        public string MrEnclave { get; set; }
        public string MrSigner { get; set; }
        public string ProductId { get; set; }
        public string SecurityVersion { get; set; }
        public string Attributes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class VerifyAttestationResponse
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

namespace NeoServiceLayer.Api.Controllers
{
    using NeoServiceLayer.Api.Models;

    /// <summary>
    /// Controller for managing attestation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AttestationController : ControllerBase
    {
        private readonly IAttestationService _attestationService;
        private readonly ILogger<AttestationController> _logger;

        /// <summary>
        /// Initializes a new instance of the AttestationController class.
        /// </summary>
        /// <param name="attestationService">The attestation service.</param>
        /// <param name="logger">The logger.</param>
        public AttestationController(IAttestationService attestationService, ILogger<AttestationController> logger)
        {
            _attestationService = attestationService ?? throw new ArgumentNullException(nameof(attestationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current attestation proof.
        /// </summary>
        /// <returns>The current attestation proof.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 500)]
        public async Task<IActionResult> GetAttestation()
        {
            try
            {
                _logger.LogInformation("Getting current attestation proof");

                var attestationProof = await _attestationService.GetCurrentAttestationProofAsync();

                _logger.LogInformation("Attestation proof retrieved successfully");

                return Ok(ApiResponse<AttestationProof>.CreateSuccess(attestationProof));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation proof");
                return StatusCode(500, ApiResponse<AttestationProof>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while getting the attestation proof."));
            }
        }

        /// <summary>
        /// Gets the current attestation proof.
        /// </summary>
        /// <returns>The current attestation proof.</returns>
        [HttpGet("proof")]
        [ProducesResponseType(typeof(ApiResponse<AttestationProofResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AttestationProofResponse>), 500)]
        public async Task<IActionResult> GetAttestationProof()
        {
            try
            {
                _logger.LogInformation("Getting attestation proof proof");

                // Get the current attestation proof from the service
                var currentProof = await _attestationService.GetCurrentAttestationProofAsync();

                // Map to response model
                var attestationProof = new AttestationProofResponse
                {
                    Id = currentProof.Id,
                    MrEnclave = currentProof.MrEnclave,
                    MrSigner = currentProof.MrSigner,
                    ProductId = currentProof.ProductId,
                    SecurityVersion = currentProof.SecurityVersion,
                    Attributes = currentProof.Attributes,
                    Report = currentProof.Report,
                    Signature = currentProof.Signature,
                    CreatedAt = currentProof.CreatedAt,
                    ExpiresAt = currentProof.ExpiresAt
                };

                _logger.LogInformation("Attestation proof retrieved successfully");

                return Ok(ApiResponse<AttestationProofResponse>.CreateSuccess(attestationProof));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation proof");
                return StatusCode(500, ApiResponse<AttestationProofResponse>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while getting the attestation proof."));
            }
        }

        /// <summary>
        /// Gets the enclave identity.
        /// </summary>
        /// <returns>The enclave identity.</returns>
        [HttpGet("identity")]
        [ProducesResponseType(typeof(ApiResponse<EnclaveIdentityResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<EnclaveIdentityResponse>), 500)]
        public async Task<IActionResult> GetEnclaveIdentity()
        {
            try
            {
                _logger.LogInformation("Getting attestation proof identity");

                // Get the current attestation proof from the service
                var currentProof = await _attestationService.GetCurrentAttestationProofAsync();

                // Extract enclave identity information from the attestation proof
                var identity = new EnclaveIdentityResponse
                {
                    EnclaveId = currentProof.Id,
                    MrEnclave = currentProof.MrEnclave,
                    MrSigner = currentProof.MrSigner,
                    IsSimulationMode = currentProof.Metadata?.ContainsKey("IsSimulationMode") == true &&
                                      Convert.ToBoolean(currentProof.Metadata["IsSimulationMode"])
                };

                _logger.LogInformation("Enclave identity retrieved successfully");

                return Ok(ApiResponse<EnclaveIdentityResponse>.CreateSuccess(identity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave identity");
                return StatusCode(500, ApiResponse<EnclaveIdentityResponse>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while getting the enclave identity."));
            }
        }

        /// <summary>
        /// Gets the enclave status.
        /// </summary>
        /// <returns>The enclave status.</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<EnclaveStatusResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<EnclaveStatusResponse>), 500)]
        public async Task<IActionResult> GetEnclaveStatus()
        {
            try
            {
                _logger.LogInformation("Getting enclave status");

                // Get the current attestation proof from the service
                var currentProof = await _attestationService.GetCurrentAttestationProofAsync();

                // Extract enclave status information from the attestation proof
                var status = new EnclaveStatusResponse
                {
                    Status = "Running", // Assuming the enclave is running if we can get an attestation proof
                    IsInitialized = true, // Assuming the enclave is initialized if we can get an attestation proof
                    IsSimulationMode = currentProof.Metadata?.ContainsKey("IsSimulationMode") == true &&
                                      Convert.ToBoolean(currentProof.Metadata["IsSimulationMode"]),
                    Version = currentProof.SecurityVersion,
                    MrEnclave = currentProof.MrEnclave,
                    MrSigner = currentProof.MrSigner,
                    EnclaveId = currentProof.Id
                };

                _logger.LogInformation("Enclave status retrieved successfully");

                return Ok(ApiResponse<EnclaveStatusResponse>.CreateSuccess(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enclave status");
                return StatusCode(500, ApiResponse<EnclaveStatusResponse>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while getting the enclave status."));
            }
        }

        /// <summary>
        /// Verifies an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(ApiResponse<VerifyAttestationResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<VerifyAttestationResponse>), 400)]
        [ProducesResponseType(typeof(ApiResponse<VerifyAttestationResponse>), 500)]
        public async Task<IActionResult> VerifyAttestation([FromBody] AttestationProofResponse attestationProof)
        {
            try
            {
                _logger.LogInformation("Verifying attestation proof");

                if (attestationProof == null)
                {
                    _logger.LogWarning("Attestation proof is null");
                    return BadRequest(ApiResponse<VerifyAttestationResponse>.CreateError(ApiErrorCodes.ValidationError, "Attestation proof is required."));
                }

                // Convert the response model to domain model
                var proof = new AttestationProof
                {
                    Id = attestationProof.Id,
                    MrEnclave = attestationProof.MrEnclave,
                    MrSigner = attestationProof.MrSigner,
                    ProductId = attestationProof.ProductId,
                    SecurityVersion = attestationProof.SecurityVersion,
                    Attributes = attestationProof.Attributes,
                    Report = attestationProof.Report,
                    Signature = attestationProof.Signature,
                    CreatedAt = attestationProof.CreatedAt,
                    ExpiresAt = attestationProof.ExpiresAt
                };

                // Verify the attestation proof
                var isValid = await _attestationService.VerifyAttestationProofAsync(proof);

                // Create the response
                var result = new VerifyAttestationResponse
                {
                    IsValid = isValid,
                    Reason = isValid ? "Verification successful" : "Verification failed",
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Attestation proof verification completed with result: {IsValid}", result.IsValid);

                return Ok(ApiResponse<VerifyAttestationResponse>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying attestation proof");
                return StatusCode(500, ApiResponse<VerifyAttestationResponse>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while verifying the attestation proof."));
            }
        }

        /// <summary>
        /// Gets an attestation proof by ID.
        /// </summary>
        /// <param name="attestationProofId">The ID of the attestation proof to get.</param>
        /// <returns>The attestation proof with the specified ID.</returns>
        [HttpGet("{attestationProofId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 401)]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 404)]
        [ProducesResponseType(typeof(ApiResponse<AttestationProof>), 500)]
        public async Task<IActionResult> GetAttestationProof(string attestationProofId)
        {
            try
            {
                _logger.LogInformation("Getting attestation proof {AttestationProofId}", attestationProofId);

                var attestationProof = await _attestationService.GetAttestationProofAsync(attestationProofId);

                if (attestationProof == null)
                {
                    _logger.LogWarning("Attestation proof {AttestationProofId} not found", attestationProofId);
                    return NotFound(ApiResponse<AttestationProof>.CreateError(ApiErrorCodes.NotFound, $"Attestation proof with ID {attestationProofId} not found."));
                }

                _logger.LogInformation("Attestation proof {AttestationProofId} retrieved successfully", attestationProofId);

                return Ok(ApiResponse<AttestationProof>.CreateSuccess(attestationProof));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attestation proof {AttestationProofId}", attestationProofId);
                return StatusCode(500, ApiResponse<AttestationProof>.CreateError(ApiErrorCodes.InternalServerError, "An error occurred while getting the attestation proof."));
            }
        }
    }

    /// <summary>
    /// Represents the result of an attestation verification.
    /// </summary>
    public class AttestationVerificationResult
    {
        /// <summary>
        /// Gets or sets whether the attestation proof is valid.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Gets or sets the enclave identity.
        /// </summary>
        public EnclaveIdentity EnclaveIdentity { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the attestation proof.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the identity of an enclave.
    /// </summary>
    public class EnclaveIdentity
    {
        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        public string MrSigner { get; set; }
    }

    /// <summary>
    /// Represents the response for enclave identity.
    /// </summary>
    public class EnclaveIdentityResponse
    {
        /// <summary>
        /// Gets or sets the enclave ID.
        /// </summary>
        public string EnclaveId { get; set; }

        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        public string MrSigner { get; set; }

        /// <summary>
        /// Gets or sets whether the enclave is running in simulation mode.
        /// </summary>
        public bool IsSimulationMode { get; set; }
    }

    /// <summary>
    /// Represents the response for enclave status.
    /// </summary>
    public class EnclaveStatusResponse
    {
        /// <summary>
        /// Gets or sets the status of the enclave.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets whether the enclave is initialized.
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Gets or sets whether the enclave is running in simulation mode.
        /// </summary>
        public bool IsSimulationMode { get; set; }

        /// <summary>
        /// Gets or sets the version of the enclave.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        public string MrSigner { get; set; }

        /// <summary>
        /// Gets or sets the enclave ID.
        /// </summary>
        public string EnclaveId { get; set; }
    }
}
