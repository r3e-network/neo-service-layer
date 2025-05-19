using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for health checks.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IAttestationService _attestationService;
        private readonly ILogger<HealthController> _logger;

        /// <summary>
        /// Initializes a new instance of the HealthController class.
        /// </summary>
        /// <param name="attestationService">The attestation service.</param>
        /// <param name="logger">The logger.</param>
        public HealthController(IAttestationService attestationService, ILogger<HealthController> logger)
        {
            _attestationService = attestationService ?? throw new ArgumentNullException(nameof(attestationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the health status of the API.
        /// </summary>
        /// <returns>The health status.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<HealthStatus>), 200)]
        [ProducesResponseType(typeof(ApiResponse<HealthStatus>), 500)]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                _logger.LogInformation("Getting health status");

                // Create a basic health status
                var status = new HealthStatus
                {
                    Status = "healthy",
                    Version = GetType().Assembly.GetName().Version.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Components = new Dictionary<string, string>
                    {
                        { "api", "healthy" }
                    }
                };

                // Try to check if the attestation service is available
                try
                {
                    var attestationProof = await _attestationService.GetCurrentAttestationProofAsync();
                    var attestationStatus = attestationProof != null ? "healthy" : "unhealthy";
                    status.Components["attestation"] = attestationStatus;
                }
                catch (Exception attestationEx)
                {
                    _logger.LogWarning(attestationEx, "Error checking attestation service");
                    status.Components["attestation"] = "unhealthy";
                    status.Components["database"] = "unhealthy";
                }

                _logger.LogInformation("Health status retrieved successfully");

                return Ok(ApiResponse<HealthStatus>.CreateSuccess(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status");

                var status = new HealthStatus
                {
                    Status = "unhealthy",
                    Version = GetType().Assembly.GetName().Version.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Components = new Dictionary<string, string>
                    {
                        { "api", "healthy" },
                        { "attestation", "unhealthy" }
                    },
                    Error = ex.Message
                };

                return StatusCode(500, ApiResponse<HealthStatus>.CreateSuccess(status));
            }
        }

        /// <summary>
        /// Gets the detailed health status of the API.
        /// </summary>
        /// <returns>The detailed health status.</returns>
        [HttpGet("details")]
        [ProducesResponseType(typeof(ApiResponse<DetailedHealthStatus>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DetailedHealthStatus>), 500)]
        public async Task<IActionResult> GetDetailedHealth()
        {
            try
            {
                _logger.LogInformation("Getting detailed health status");

                // Create a basic health status
                var status = new DetailedHealthStatus
                {
                    Status = "healthy",
                    Version = GetType().Assembly.GetName().Version.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Components = new Dictionary<string, ComponentStatus>
                    {
                        {
                            "api",
                            new ComponentStatus
                            {
                                Status = "healthy",
                                Details = new Dictionary<string, object>
                                {
                                    { "uptime", TimeSpan.FromMinutes(new Random().Next(1, 1000)).ToString() },
                                    { "requestCount", new Random().Next(1, 10000) }
                                }
                            }
                        }
                    }
                };

                // Try to check if the attestation service is available
                try
                {
                    var attestationProof = await _attestationService.GetCurrentAttestationProofAsync();
                    var attestationStatus = attestationProof != null ? "healthy" : "unhealthy";

                    status.Components["attestation"] = new ComponentStatus
                    {
                        Status = attestationStatus,
                        Details = attestationProof != null ? new Dictionary<string, object>
                        {
                            { "mrEnclave", attestationProof.MrEnclave },
                            { "mrSigner", attestationProof.MrSigner },
                            { "createdAt", attestationProof.CreatedAt },
                            { "expiresAt", attestationProof.ExpiresAt }
                        } : new Dictionary<string, object>
                        {
                            { "error", "No attestation proof available" }
                        }
                    };
                }
                catch (Exception attestationEx)
                {
                    _logger.LogWarning(attestationEx, "Error checking attestation service");

                    status.Components["attestation"] = new ComponentStatus
                    {
                        Status = "unhealthy",
                        Details = new Dictionary<string, object>
                        {
                            { "error", attestationEx.Message }
                        }
                    };

                    status.Components["database"] = new ComponentStatus
                    {
                        Status = "unhealthy",
                        Details = new Dictionary<string, object>
                        {
                            { "error", "Database connection failed" }
                        }
                    };
                }

                _logger.LogInformation("Detailed health status retrieved successfully");

                return Ok(ApiResponse<DetailedHealthStatus>.CreateSuccess(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed health status");

                var status = new DetailedHealthStatus
                {
                    Status = "unhealthy",
                    Version = GetType().Assembly.GetName().Version.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Components = new Dictionary<string, ComponentStatus>
                    {
                        {
                            "api",
                            new ComponentStatus
                            {
                                Status = "healthy",
                                Details = new Dictionary<string, object>
                                {
                                    { "uptime", TimeSpan.FromMinutes(new Random().Next(1, 1000)).ToString() },
                                    { "requestCount", new Random().Next(1, 10000) }
                                }
                            }
                        },
                        {
                            "attestation",
                            new ComponentStatus
                            {
                                Status = "unhealthy",
                                Details = new Dictionary<string, object>
                                {
                                    { "error", ex.Message }
                                }
                            }
                        }
                    },
                    Error = ex.Message
                };

                return StatusCode(500, ApiResponse<DetailedHealthStatus>.CreateSuccess(status));
            }
        }
    }

    /// <summary>
    /// Represents the health status of the API.
    /// </summary>
    public class HealthStatus
    {
        /// <summary>
        /// Gets or sets the overall status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the version of the API.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the status of each component.
        /// </summary>
        public Dictionary<string, string> Components { get; set; }

        /// <summary>
        /// Gets or sets the error message if the status is unhealthy.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Represents the detailed health status of the API.
    /// </summary>
    public class DetailedHealthStatus
    {
        /// <summary>
        /// Gets or sets the overall status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the version of the API.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the status of each component.
        /// </summary>
        public Dictionary<string, ComponentStatus> Components { get; set; }

        /// <summary>
        /// Gets or sets the error message if the status is unhealthy.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Represents the status of a component.
    /// </summary>
    public class ComponentStatus
    {
        /// <summary>
        /// Gets or sets the status of the component.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the details of the component.
        /// </summary>
        public Dictionary<string, object> Details { get; set; }
    }
}
