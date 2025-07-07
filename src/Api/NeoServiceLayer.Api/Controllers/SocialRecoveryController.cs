using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.SocialRecovery;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// API controller for social recovery network operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SocialRecoveryController : ControllerBase
    {
        private readonly ILogger<SocialRecoveryController> _logger;
        private readonly ISocialRecoveryService _socialRecoveryService;

        public SocialRecoveryController(
            ILogger<SocialRecoveryController> logger,
            ISocialRecoveryService socialRecoveryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _socialRecoveryService = socialRecoveryService ?? throw new ArgumentNullException(nameof(socialRecoveryService));
        }

        /// <summary>
        /// Enrolls a new guardian in the social recovery network
        /// </summary>
        [HttpPost("guardians/enroll")]
        [ProducesResponseType(typeof(GuardianEnrollmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EnrollGuardian([FromBody] GuardianEnrollmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var guardianInfo = await _socialRecoveryService.EnrollGuardianAsync(
                    request.Address,
                    BigInteger.Parse(request.StakeAmount),
                    request.Blockchain);

                return Ok(new GuardianEnrollmentResponse
                {
                    Success = true,
                    Guardian = guardianInfo,
                    Message = "Successfully enrolled as guardian"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling guardian");
                return StatusCode(500, new ErrorResponse { Error = "Failed to enroll guardian", Details = ex.Message });
            }
        }

        /// <summary>
        /// Initiates a recovery request for an account
        /// </summary>
        [HttpPost("recovery/initiate")]
        [ProducesResponseType(typeof(RecoveryInitiationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InitiateRecovery([FromBody] RecoveryInitiationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var recovery = await _socialRecoveryService.InitiateRecoveryAsync(
                    request.AccountAddress,
                    request.NewOwner,
                    request.StrategyId,
                    request.IsEmergency,
                    BigInteger.Parse(request.RecoveryFee),
                    request.AuthFactors,
                    request.Blockchain);

                return Ok(new RecoveryInitiationResponse
                {
                    Success = true,
                    RecoveryRequest = recovery,
                    Message = "Recovery initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating recovery");
                return StatusCode(500, new ErrorResponse { Error = "Failed to initiate recovery", Details = ex.Message });
            }
        }

        /// <summary>
        /// Confirms a recovery request as a guardian
        /// </summary>
        [HttpPost("recovery/{recoveryId}/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmRecovery(
            [FromRoute] string recoveryId,
            [FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var result = await _socialRecoveryService.ConfirmRecoveryAsync(recoveryId, blockchain);

                if (result)
                {
                    return Ok(new { success = true, message = "Recovery confirmed successfully" });
                }

                return BadRequest(new ErrorResponse { Error = "Failed to confirm recovery" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming recovery {RecoveryId}", recoveryId);
                return StatusCode(500, new ErrorResponse { Error = "Failed to confirm recovery", Details = ex.Message });
            }
        }

        /// <summary>
        /// Establishes trust relationship with another guardian
        /// </summary>
        [HttpPost("trust/establish")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EstablishTrust([FromBody] EstablishTrustRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _socialRecoveryService.EstablishTrustAsync(
                    request.Trustee,
                    request.TrustLevel,
                    request.Blockchain);

                if (result)
                {
                    return Ok(new { success = true, message = "Trust established successfully" });
                }

                return BadRequest(new ErrorResponse { Error = "Failed to establish trust" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error establishing trust");
                return StatusCode(500, new ErrorResponse { Error = "Failed to establish trust", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets information about a guardian
        /// </summary>
        [HttpGet("guardians/{address}")]
        [ProducesResponseType(typeof(GuardianInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGuardianInfo(
            [FromRoute] string address,
            [FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var guardian = await _socialRecoveryService.GetGuardianInfoAsync(address, blockchain);

                if (guardian == null)
                {
                    return NotFound(new ErrorResponse { Error = "Guardian not found" });
                }

                return Ok(guardian);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting guardian info");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get guardian info", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets information about a recovery request
        /// </summary>
        [HttpGet("recovery/{recoveryId}")]
        [ProducesResponseType(typeof(RecoveryInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRecoveryInfo(
            [FromRoute] string recoveryId,
            [FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var recovery = await _socialRecoveryService.GetRecoveryInfoAsync(recoveryId, blockchain);

                if (recovery == null)
                {
                    return NotFound(new ErrorResponse { Error = "Recovery not found" });
                }

                return Ok(recovery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recovery info");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get recovery info", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets available recovery strategies
        /// </summary>
        [HttpGet("strategies")]
        [ProducesResponseType(typeof(List<RecoveryStrategy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStrategies([FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var strategies = await _socialRecoveryService.GetAvailableStrategiesAsync(blockchain);
                return Ok(strategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting strategies");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get strategies", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets network statistics
        /// </summary>
        [HttpGet("stats")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(NetworkStats), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNetworkStats([FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var stats = await _socialRecoveryService.GetNetworkStatsAsync(blockchain);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network stats");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get network stats", Details = ex.Message });
            }
        }

        /// <summary>
        /// Adds multi-factor authentication to an account
        /// </summary>
        [HttpPost("auth/factor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddAuthFactor([FromBody] AddAuthFactorRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _socialRecoveryService.AddAuthFactorAsync(
                    request.FactorType,
                    request.FactorHash,
                    request.Blockchain);

                if (result)
                {
                    return Ok(new { success = true, message = "Authentication factor added successfully" });
                }

                return BadRequest(new ErrorResponse { Error = "Failed to add authentication factor" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding auth factor");
                return StatusCode(500, new ErrorResponse { Error = "Failed to add auth factor", Details = ex.Message });
            }
        }

        /// <summary>
        /// Configures account recovery preferences
        /// </summary>
        [HttpPost("accounts/{accountAddress}/configure")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfigureAccountRecovery(
            [FromRoute] string accountAddress,
            [FromBody] ConfigureRecoveryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _socialRecoveryService.ConfigureAccountRecoveryAsync(
                    accountAddress,
                    request.PreferredStrategy,
                    BigInteger.Parse(request.RecoveryThreshold),
                    request.AllowNetworkGuardians,
                    BigInteger.Parse(request.MinGuardianReputation),
                    request.Blockchain);

                if (result)
                {
                    return Ok(new { success = true, message = "Recovery configuration updated successfully" });
                }

                return BadRequest(new ErrorResponse { Error = "Failed to update configuration" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring recovery");
                return StatusCode(500, new ErrorResponse { Error = "Failed to configure recovery", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets active recovery requests for an account
        /// </summary>
        [HttpGet("accounts/{accountAddress}/recoveries")]
        [ProducesResponseType(typeof(List<RecoveryRequest>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveRecoveries(
            [FromRoute] string accountAddress,
            [FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var recoveries = await _socialRecoveryService.GetActiveRecoveriesAsync(accountAddress, blockchain);
                return Ok(recoveries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active recoveries");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get active recoveries", Details = ex.Message });
            }
        }

        /// <summary>
        /// Gets trust relationships for a guardian
        /// </summary>
        [HttpGet("guardians/{guardian}/trust")]
        [ProducesResponseType(typeof(List<TrustRelation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTrustRelationships(
            [FromRoute] string guardian,
            [FromQuery] string blockchain = "neo-n3")
        {
            try
            {
                var relations = await _socialRecoveryService.GetTrustRelationshipsAsync(guardian, blockchain);
                return Ok(relations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trust relationships");
                return StatusCode(500, new ErrorResponse { Error = "Failed to get trust relationships", Details = ex.Message });
            }
        }
    }

    #region Request/Response Models
    public class GuardianEnrollmentRequest
    {
        [Required]
        public string Address { get; set; }

        [Required]
        public string StakeAmount { get; set; }

        public string Blockchain { get; set; } = "neo-n3";
    }

    public class GuardianEnrollmentResponse
    {
        public bool Success { get; set; }
        public GuardianInfo Guardian { get; set; }
        public string Message { get; set; }
    }

    public class RecoveryInitiationRequest
    {
        [Required]
        public string AccountAddress { get; set; }

        [Required]
        public string NewOwner { get; set; }

        [Required]
        public string StrategyId { get; set; }

        public bool IsEmergency { get; set; }

        [Required]
        public string RecoveryFee { get; set; }

        public List<AuthFactor> AuthFactors { get; set; }

        public string Blockchain { get; set; } = "neo-n3";
    }

    public class RecoveryInitiationResponse
    {
        public bool Success { get; set; }
        public RecoveryRequest RecoveryRequest { get; set; }
        public string Message { get; set; }
    }

    public class EstablishTrustRequest
    {
        [Required]
        public string Trustee { get; set; }

        [Required]
        [Range(0, 100)]
        public int TrustLevel { get; set; }

        public string Blockchain { get; set; } = "neo-n3";
    }

    public class AddAuthFactorRequest
    {
        [Required]
        public string FactorType { get; set; }

        [Required]
        public string FactorHash { get; set; }

        public string Blockchain { get; set; } = "neo-n3";
    }

    public class ConfigureRecoveryRequest
    {
        [Required]
        public string PreferredStrategy { get; set; }

        [Required]
        public string RecoveryThreshold { get; set; }

        public bool AllowNetworkGuardians { get; set; }

        [Required]
        public string MinGuardianReputation { get; set; }

        public string Blockchain { get; set; } = "neo-n3";
    }

    public class ErrorResponse
    {
        public string Error { get; set; }
        public string Details { get; set; }
    }
    #endregion
}
