using System;
using System.Collections.Generic;
using System.Text.Json;
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
    /// Controller for managing JavaScript functions.
    /// </summary>
    [ApiController]
    [Route("api/v1/functions")]
    [Authorize]
    public class JavaScriptFunctionController : ControllerBase
    {
        private readonly IJavaScriptExecutionService _javaScriptExecutionService;
        private readonly ILogger<JavaScriptFunctionController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptFunctionController"/> class.
        /// </summary>
        /// <param name="javaScriptExecutionService">The JavaScript execution service.</param>
        /// <param name="logger">The logger.</param>
        public JavaScriptFunctionController(
            IJavaScriptExecutionService javaScriptExecutionService,
            ILogger<JavaScriptFunctionController> logger)
        {
            _javaScriptExecutionService = javaScriptExecutionService ?? throw new ArgumentNullException(nameof(javaScriptExecutionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new JavaScript function.
        /// </summary>
        /// <param name="request">The create function request.</param>
        /// <returns>The created function.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<JavaScriptFunctionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> CreateFunction([FromBody] CreateJavaScriptFunctionRequest request)
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

                var function = new JavaScriptFunction
                {
                    Name = request.Name,
                    Description = request.Description,
                    Code = request.Code,
                    RequiredSecrets = request.RequiredSecrets,
                    GasLimit = request.GasLimit ?? 1000000,
                    OwnerId = userId
                };

                var createdFunction = await _javaScriptExecutionService.CreateFunctionAsync(function);

                return Ok(new ApiResponse<JavaScriptFunctionResponse>
                {
                    Success = true,
                    Data = new JavaScriptFunctionResponse
                    {
                        Id = createdFunction.Id,
                        Name = createdFunction.Name,
                        Description = createdFunction.Description,
                        RequiredSecrets = createdFunction.RequiredSecrets,
                        GasLimit = createdFunction.GasLimit,
                        Status = createdFunction.Status.ToString(),
                        CreatedAt = createdFunction.CreatedAt,
                        UpdatedAt = createdFunction.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating JavaScript function");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Gets a JavaScript function by ID.
        /// </summary>
        /// <param name="id">The function ID.</param>
        /// <returns>The function.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<JavaScriptFunctionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetFunction(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid function ID"
                    });
                }

                var userId = User.Identity.Name;
                var function = await _javaScriptExecutionService.GetFunctionAsync(id);

                if (function == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Function not found"
                    });
                }

                if (function.OwnerId != userId)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized"
                    });
                }

                return Ok(new ApiResponse<JavaScriptFunctionResponse>
                {
                    Success = true,
                    Data = new JavaScriptFunctionResponse
                    {
                        Id = function.Id,
                        Name = function.Name,
                        Description = function.Description,
                        Code = function.Code,
                        RequiredSecrets = function.RequiredSecrets,
                        GasLimit = function.GasLimit,
                        Status = function.Status.ToString(),
                        CreatedAt = function.CreatedAt,
                        UpdatedAt = function.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting JavaScript function");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Executes a JavaScript function.
        /// </summary>
        /// <param name="id">The function ID.</param>
        /// <param name="input">The function input.</param>
        /// <returns>The function result.</returns>
        [HttpPost("{id}/execute")]
        [ProducesResponseType(typeof(ApiResponse<JsonDocument>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> ExecuteFunction(string id, [FromBody] JsonDocument input)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid function ID"
                    });
                }

                if (input == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid input"
                    });
                }

                var userId = User.Identity.Name;
                var function = await _javaScriptExecutionService.GetFunctionAsync(id);

                if (function == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Function not found"
                    });
                }

                if (function.OwnerId != userId)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized"
                    });
                }

                var result = await _javaScriptExecutionService.ExecuteFunctionAsync(id, input, userId);

                return Ok(new ApiResponse<JsonDocument>
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript function");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }
    }
}
