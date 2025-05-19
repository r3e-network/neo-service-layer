using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.Secrets;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing secrets.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SecretsController : ControllerBase
    {
        private readonly ILogger<SecretsController> _logger;
        private readonly ISecretManager _secretManager;

        /// <summary>
        /// Initializes a new instance of the SecretsController class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="secretManager">The secret manager.</param>
        public SecretsController(
            ILogger<SecretsController> logger,
            ISecretManager secretManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretManager = secretManager ?? throw new ArgumentNullException(nameof(secretManager));
        }

        /// <summary>
        /// Gets all secret names for the current user.
        /// </summary>
        /// <returns>A list of secret names.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSecretNames()
        {
            try
            {
                var userId = GetUserId();
                var secretNames = await _secretManager.GetSecretNamesAsync(userId);
                return Ok(secretNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret names");
                return StatusCode(500, "Error getting secret names");
            }
        }

        /// <summary>
        /// Gets a secret for the current user.
        /// </summary>
        /// <param name="name">The name of the secret.</param>
        /// <returns>The secret value.</returns>
        [HttpGet("{name}")]
        [ProducesResponseType(typeof(SecretResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSecret(string name)
        {
            try
            {
                var userId = GetUserId();
                var secretValue = await _secretManager.GetSecretAsync(userId, name);
                if (secretValue == null)
                {
                    return NotFound();
                }

                return Ok(new SecretResponse { Name = name, Value = secretValue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret: {Name}", name);
                return StatusCode(500, $"Error getting secret: {name}");
            }
        }

        /// <summary>
        /// Creates or updates a secret for the current user.
        /// </summary>
        /// <param name="request">The secret request.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateSecret([FromBody] SecretRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Secret name is required");
            }

            if (request.Value == null)
            {
                return BadRequest("Secret value is required");
            }

            try
            {
                var userId = GetUserId();
                var exists = await _secretManager.SecretExistsAsync(userId, request.Name);
                bool success;

                if (exists)
                {
                    success = await _secretManager.UpdateSecretAsync(userId, request.Name, request.Value);
                }
                else
                {
                    success = await _secretManager.StoreSecretAsync(userId, request.Name, request.Value);
                }

                if (!success)
                {
                    return StatusCode(500, $"Error storing secret: {request.Name}");
                }

                return Created($"/api/secrets/{request.Name}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret: {Name}", request.Name);
                return StatusCode(500, $"Error storing secret: {request.Name}");
            }
        }

        /// <summary>
        /// Updates a secret for the current user.
        /// </summary>
        /// <param name="name">The name of the secret.</param>
        /// <param name="request">The secret request.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpPut("{name}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateSecret(string name, [FromBody] SecretRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Secret name is required");
            }

            if (request.Value == null)
            {
                return BadRequest("Secret value is required");
            }

            try
            {
                var userId = GetUserId();
                var exists = await _secretManager.SecretExistsAsync(userId, name);
                if (!exists)
                {
                    return NotFound();
                }

                var success = await _secretManager.UpdateSecretAsync(userId, name, request.Value);
                if (!success)
                {
                    return StatusCode(500, $"Error updating secret: {name}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret: {Name}", name);
                return StatusCode(500, $"Error updating secret: {name}");
            }
        }

        /// <summary>
        /// Deletes a secret for the current user.
        /// </summary>
        /// <param name="name">The name of the secret.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpDelete("{name}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteSecret(string name)
        {
            try
            {
                var userId = GetUserId();
                var exists = await _secretManager.SecretExistsAsync(userId, name);
                if (!exists)
                {
                    return NotFound();
                }

                var success = await _secretManager.DeleteSecretAsync(userId, name);
                if (!success)
                {
                    return StatusCode(500, $"Error deleting secret: {name}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret: {Name}", name);
                return StatusCode(500, $"Error deleting secret: {name}");
            }
        }

        /// <summary>
        /// Gets the user ID from the claims.
        /// </summary>
        /// <returns>The user ID.</returns>
        private string GetUserId()
        {
            // In a real implementation, this would get the user ID from the claims
            // For now, just return a fixed user ID
            return "user1";
        }
    }

    /// <summary>
    /// Request model for creating or updating a secret.
    /// </summary>
    public class SecretRequest
    {
        /// <summary>
        /// Gets or sets the name of the secret.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the secret.
        /// </summary>
        [Required]
        public string Value { get; set; }
    }

    /// <summary>
    /// Response model for a secret.
    /// </summary>
    public class SecretResponse
    {
        /// <summary>
        /// Gets or sets the name of the secret.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the secret.
        /// </summary>
        public string Value { get; set; }
    }
}
