using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing user secrets.
    /// </summary>
    [ApiController]
    [Route("api/v1/secrets")]
    [Authorize]
    public class UserSecretController : ControllerBase
    {
        private readonly IUserSecretService _userSecretService;
        private readonly ILogger<UserSecretController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserSecretController"/> class.
        /// </summary>
        /// <param name="userSecretService">The user secret service.</param>
        /// <param name="logger">The logger.</param>
        public UserSecretController(
            IUserSecretService userSecretService,
            ILogger<UserSecretController> logger)
        {
            _userSecretService = userSecretService ?? throw new ArgumentNullException(nameof(userSecretService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new user secret.
        /// </summary>
        /// <param name="request">The create secret request.</param>
        /// <returns>The created secret.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserSecretResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> CreateSecret([FromBody] CreateUserSecretRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid request"
                    });
                }

                var userId = User.Identity.Name;

                var secret = new UserSecret
                {
                    Name = request.Name,
                    Value = request.Value,
                    Description = request.Description,
                    OwnerId = userId
                };

                var createdSecret = await _userSecretService.CreateSecretAsync(secret);

                return Ok(new ApiResponse<UserSecretResponse>
                {
                    Success = true,
                    Data = new UserSecretResponse
                    {
                        Id = createdSecret.Id,
                        Name = createdSecret.Name,
                        Description = createdSecret.Description,
                        CreatedAt = createdSecret.CreatedAt,
                        UpdatedAt = createdSecret.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user secret");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Gets a user secret by ID.
        /// </summary>
        /// <param name="id">The secret ID.</param>
        /// <returns>The secret.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserSecretResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetSecret(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid secret ID"
                    });
                }

                var userId = User.Identity.Name;
                var secret = await _userSecretService.GetSecretAsync(id);

                if (secret == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Secret not found"
                    });
                }

                if (secret.OwnerId != userId)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized"
                    });
                }

                return Ok(new ApiResponse<UserSecretResponse>
                {
                    Success = true,
                    Data = new UserSecretResponse
                    {
                        Id = secret.Id,
                        Name = secret.Name,
                        Description = secret.Description,
                        CreatedAt = secret.CreatedAt,
                        UpdatedAt = secret.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user secret");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Lists all user secrets for the authenticated user.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of user secrets.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserSecretListResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> ListSecrets([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.Identity.Name;
                var (secrets, totalCount) = await _userSecretService.ListSecretsAsync(userId, page, pageSize);

                var secretResponses = secrets.Select(s => new UserSecretResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                return Ok(new ApiResponse<UserSecretListResponse>
                {
                    Success = true,
                    Data = new UserSecretListResponse
                    {
                        Secrets = secretResponses,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing user secrets");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Updates a user secret.
        /// </summary>
        /// <param name="id">The secret ID.</param>
        /// <param name="request">The update secret request.</param>
        /// <returns>The updated secret.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserSecretResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> UpdateSecret(string id, [FromBody] UpdateUserSecretRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid secret ID"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid request"
                    });
                }

                var userId = User.Identity.Name;
                var secret = await _userSecretService.GetSecretAsync(id, true);

                if (secret == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Secret not found"
                    });
                }

                if (secret.OwnerId != userId)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized"
                    });
                }

                if (!string.IsNullOrEmpty(request.Name))
                {
                    secret.Name = request.Name;
                }

                if (!string.IsNullOrEmpty(request.Value))
                {
                    secret.Value = request.Value;
                }

                if (request.Description != null)
                {
                    secret.Description = request.Description;
                }

                var updatedSecret = await _userSecretService.UpdateSecretAsync(secret);

                return Ok(new ApiResponse<UserSecretResponse>
                {
                    Success = true,
                    Data = new UserSecretResponse
                    {
                        Id = updatedSecret.Id,
                        Name = updatedSecret.Name,
                        Description = updatedSecret.Description,
                        CreatedAt = updatedSecret.CreatedAt,
                        UpdatedAt = updatedSecret.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user secret");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }
    }
}
