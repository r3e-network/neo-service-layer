using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Shared.Functions;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing and executing functions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FunctionsController : ControllerBase
    {
        private readonly ILogger<FunctionsController> _logger;
        private readonly IFunctionExecutionService _functionExecutionService;

        /// <summary>
        /// Initializes a new instance of the FunctionsController class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="functionExecutionService">The function execution service.</param>
        public FunctionsController(
            ILogger<FunctionsController> logger,
            IFunctionExecutionService functionExecutionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _functionExecutionService = functionExecutionService ?? throw new ArgumentNullException(nameof(functionExecutionService));
        }

        /// <summary>
        /// Creates a new function.
        /// </summary>
        /// <param name="request">The function creation request.</param>
        /// <returns>The created function.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(FunctionResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateFunction([FromBody] CreateFunctionRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest("Function name is required");
                }

                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest("Function code is required");
                }

                if (string.IsNullOrEmpty(request.EntryPoint))
                {
                    return BadRequest("Function entry point is required");
                }

                // Create the function
                var function = new FunctionInfo
                {
                    Name = request.Name,
                    Description = request.Description,
                    Code = request.Code,
                    EntryPoint = request.EntryPoint,
                    Runtime = request.Runtime,
                    RequiredSecrets = request.RequiredSecrets,
                    GasLimit = request.GasLimit ?? 1000000,
                    TimeoutMs = request.TimeoutMs ?? 30000,
                    MemoryLimit = request.MemoryLimit ?? 128 * 1024 * 1024,
                    OwnerId = User.Identity.Name,
                    Tags = request.Tags,
                    Metadata = request.Metadata
                };

                // Validate the function
                var validationErrors = await _functionExecutionService.ValidateFunctionAsync(function);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(new { Errors = validationErrors });
                }

                // Create the function
                var createdFunction = await _functionExecutionService.CreateFunctionAsync(function);

                // Return the created function
                var response = MapFunctionToResponse(createdFunction);
                return CreatedAtAction(nameof(GetFunction), new { id = createdFunction.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating function");
                return StatusCode(500, "Error creating function");
            }
        }

        /// <summary>
        /// Updates an existing function.
        /// </summary>
        /// <param name="id">The ID of the function to update.</param>
        /// <param name="request">The function update request.</param>
        /// <returns>The updated function.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(FunctionResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateFunction(string id, [FromBody] UpdateFunctionRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            try
            {
                // Get the existing function
                var existingFunction = await _functionExecutionService.GetFunctionAsync(id);
                if (existingFunction == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (existingFunction.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Update the function
                existingFunction.Name = request.Name ?? existingFunction.Name;
                existingFunction.Description = request.Description ?? existingFunction.Description;
                existingFunction.Code = request.Code ?? existingFunction.Code;
                existingFunction.EntryPoint = request.EntryPoint ?? existingFunction.EntryPoint;
                existingFunction.Runtime = request.Runtime ?? existingFunction.Runtime;
                existingFunction.RequiredSecrets = request.RequiredSecrets ?? existingFunction.RequiredSecrets;
                existingFunction.GasLimit = request.GasLimit ?? existingFunction.GasLimit;
                existingFunction.TimeoutMs = request.TimeoutMs ?? existingFunction.TimeoutMs;
                existingFunction.MemoryLimit = request.MemoryLimit ?? existingFunction.MemoryLimit;
                existingFunction.Tags = request.Tags ?? existingFunction.Tags;
                existingFunction.Metadata = request.Metadata ?? existingFunction.Metadata;

                // Validate the function
                var validationErrors = await _functionExecutionService.ValidateFunctionAsync(existingFunction);
                if (validationErrors.Count > 0)
                {
                    return BadRequest(new { Errors = validationErrors });
                }

                // Update the function
                var updatedFunction = await _functionExecutionService.UpdateFunctionAsync(existingFunction);

                // Return the updated function
                var response = MapFunctionToResponse(updatedFunction);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating function {FunctionId}", id);
                return StatusCode(500, $"Error updating function {id}");
            }
        }

        /// <summary>
        /// Gets a function by ID.
        /// </summary>
        /// <param name="id">The ID of the function to get.</param>
        /// <returns>The function.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FunctionResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFunction(string id)
        {
            try
            {
                // Get the function
                var function = await _functionExecutionService.GetFunctionAsync(id);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Return the function
                var response = MapFunctionToResponse(function);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function {FunctionId}", id);
                return StatusCode(500, $"Error getting function {id}");
            }
        }

        /// <summary>
        /// Gets a specific version of a function.
        /// </summary>
        /// <param name="id">The ID of the function to get.</param>
        /// <param name="version">The version of the function to get.</param>
        /// <returns>The function version.</returns>
        [HttpGet("{id}/versions/{version}")]
        [ProducesResponseType(typeof(FunctionResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFunctionVersion(string id, string version)
        {
            try
            {
                // Get the function version
                var function = await _functionExecutionService.GetFunctionVersionAsync(id, version);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Return the function
                var response = MapFunctionToResponse(function);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function {FunctionId} version {Version}", id, version);
                return StatusCode(500, $"Error getting function {id} version {version}");
            }
        }

        /// <summary>
        /// Gets all versions of a function.
        /// </summary>
        /// <param name="id">The ID of the function to get versions for.</param>
        /// <returns>A list of function versions.</returns>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(IEnumerable<FunctionResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFunctionVersions(string id)
        {
            try
            {
                // Get the function versions
                var versions = await _functionExecutionService.GetFunctionVersionsAsync(id);
                if (versions.Count == 0)
                {
                    return NotFound();
                }

                // Check ownership
                if (versions[0].OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Return the function versions
                var response = versions.Select(MapFunctionToResponse);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function versions {FunctionId}", id);
                return StatusCode(500, $"Error getting function versions {id}");
            }
        }

        /// <summary>
        /// Lists all functions for the current user.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of functions.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<FunctionResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ListFunctions(
            [FromQuery] FunctionStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get the functions
                var (functions, totalCount) = await _functionExecutionService.ListFunctionsAsync(
                    User.Identity.Name,
                    status,
                    page,
                    pageSize);

                // Return the functions
                var response = new PaginatedResponse<FunctionResponse>
                {
                    Items = functions.Select(MapFunctionToResponse).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing functions");
                return StatusCode(500, "Error listing functions");
            }
        }

        /// <summary>
        /// Deletes a function.
        /// </summary>
        /// <param name="id">The ID of the function to delete.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteFunction(string id)
        {
            try
            {
                // Get the function
                var function = await _functionExecutionService.GetFunctionAsync(id);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Delete the function
                var success = await _functionExecutionService.DeleteFunctionAsync(id);
                if (!success)
                {
                    return StatusCode(500, $"Error deleting function {id}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting function {FunctionId}", id);
                return StatusCode(500, $"Error deleting function {id}");
            }
        }

        /// <summary>
        /// Deploys a function.
        /// </summary>
        /// <param name="id">The ID of the function to deploy.</param>
        /// <param name="version">The version of the function to deploy.</param>
        /// <returns>A status code indicating success or failure.</returns>
        [HttpPost("{id}/versions/{version}/deploy")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeployFunction(string id, string version)
        {
            try
            {
                // Get the function version
                var function = await _functionExecutionService.GetFunctionVersionAsync(id, version);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Deploy the function
                var success = await _functionExecutionService.DeployFunctionAsync(id, version);
                if (!success)
                {
                    return StatusCode(500, $"Error deploying function {id} version {version}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying function {FunctionId} version {Version}", id, version);
                return StatusCode(500, $"Error deploying function {id} version {version}");
            }
        }

        /// <summary>
        /// Executes a function.
        /// </summary>
        /// <param name="id">The ID of the function to execute.</param>
        /// <param name="request">The execution request.</param>
        /// <returns>The execution result.</returns>
        [HttpPost("{id}/execute")]
        [ProducesResponseType(typeof(ExecutionResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ExecuteFunction(string id, [FromBody] ExecuteFunctionRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            try
            {
                // Get the function
                var function = await _functionExecutionService.GetFunctionAsync(id);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Execute the function
                var context = await _functionExecutionService.ExecuteAsync(
                    id,
                    request.Input ?? "{}",
                    User.Identity.Name,
                    request.GasLimit ?? function.GasLimit);

                // Return the execution result
                var response = new ExecutionResponse
                {
                    ExecutionId = context.ExecutionId,
                    Success = context.Success,
                    Result = context.Result,
                    Error = context.Error,
                    GasUsed = context.GasUsed,
                    ExecutionTimeMs = context.ExecutionTimeMs,
                    MemoryUsed = context.MemoryUsed,
                    Logs = context.Logs
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionId}", id);
                return StatusCode(500, $"Error executing function {id}");
            }
        }

        /// <summary>
        /// Gets the execution history for a function.
        /// </summary>
        /// <param name="id">The ID of the function.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of execution records.</returns>
        [HttpGet("{id}/executions")]
        [ProducesResponseType(typeof(PaginatedResponse<ExecutionRecordResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetExecutionHistory(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get the function
                var function = await _functionExecutionService.GetFunctionAsync(id);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Get the execution history
                var (records, totalCount) = await _functionExecutionService.GetExecutionHistoryAsync(id, page, pageSize);

                // Return the execution history
                var response = new PaginatedResponse<ExecutionRecordResponse>
                {
                    Items = records.Select(MapExecutionRecordToResponse).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for function {FunctionId}", id);
                return StatusCode(500, $"Error getting execution history for function {id}");
            }
        }

        /// <summary>
        /// Gets the gas usage for a function.
        /// </summary>
        /// <param name="id">The ID of the function.</param>
        /// <returns>The gas usage information.</returns>
        [HttpGet("{id}/gas-usage")]
        [ProducesResponseType(typeof(GasUsageResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFunctionGasUsage(string id)
        {
            try
            {
                // Get the function
                var function = await _functionExecutionService.GetFunctionAsync(id);
                if (function == null)
                {
                    return NotFound();
                }

                // Check ownership
                if (function.OwnerId != User.Identity.Name)
                {
                    return Forbid();
                }

                // Get the gas usage
                var gasUsage = await _functionExecutionService.GetFunctionGasUsageAsync(id);

                // Return the gas usage
                var response = MapGasUsageToResponse(gasUsage);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gas usage for function {FunctionId}", id);
                return StatusCode(500, $"Error getting gas usage for function {id}");
            }
        }

        /// <summary>
        /// Gets the gas usage for the current user.
        /// </summary>
        /// <returns>The gas usage information.</returns>
        [HttpGet("gas-usage")]
        [ProducesResponseType(typeof(GasUsageResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUserGasUsage()
        {
            try
            {
                // Get the gas usage
                var gasUsage = await _functionExecutionService.GetUserGasUsageAsync(User.Identity.Name);

                // Return the gas usage
                var response = MapGasUsageToResponse(gasUsage);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gas usage for user {UserId}", User.Identity.Name);
                return StatusCode(500, "Error getting gas usage for user");
            }
        }

        /// <summary>
        /// Executes JavaScript code directly using Occlum.
        /// </summary>
        /// <param name="request">The JavaScript execution request.</param>
        /// <returns>The execution result.</returns>
        [HttpPost("execute-javascript")]
        [ProducesResponseType(typeof(ApiResponse<JavaScriptExecutionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> ExecuteJavaScript([FromBody] JavaScriptExecutionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "Invalid request",
                        Details = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                    }
                });
            }

            try
            {
                // Get the TEE host service
                var teeHostService = HttpContext.RequestServices.GetService(typeof(ITeeHostService)) as ITeeHostService;
                if (teeHostService == null)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ApiError
                        {
                            Code = "INTERNAL_ERROR",
                            Message = "TEE host service not available"
                        }
                    });
                }

                // Check if Occlum support is enabled
                var occlumManager = HttpContext.RequestServices.GetService(typeof(IOcclumManager)) as IOcclumManager;
                if (occlumManager == null || !occlumManager.IsOcclumSupportEnabled())
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ApiError
                        {
                            Code = "OCCLUM_NOT_SUPPORTED",
                            Message = "Occlum support is not enabled"
                        }
                    });
                }

                // Start a stopwatch to measure execution time
                var stopwatch = Stopwatch.StartNew();

                // Execute the JavaScript code
                string result = await teeHostService.ExecuteJavaScriptAsync(
                    request.Code,
                    request.Input ?? "{}",
                    request.Secrets ?? "{}",
                    request.FunctionId ?? Guid.NewGuid().ToString(),
                    request.UserId ?? User.Identity?.Name ?? "anonymous");

                // Stop the stopwatch
                stopwatch.Stop();

                // Create the response
                var response = new JavaScriptExecutionResponse
                {
                    Result = result,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    GasUsed = CalculateGasUsed(stopwatch.ElapsedMilliseconds, result.Length)
                };

                return Ok(new ApiResponse<JavaScriptExecutionResponse>
                {
                    Success = true,
                    Data = response,
                    Metadata = new ApiMetadata
                    {
                        RequestId = HttpContext.TraceIdentifier,
                        Timestamp = DateTime.UtcNow,
                        Additional = new Dictionary<string, object>
                        {
                            ["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds,
                            ["GasUsed"] = response.GasUsed,
                            ["OcclumEnabled"] = true
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript code: {Error}", ex.Message);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "EXECUTION_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Gets information about the TEE.
        /// </summary>
        /// <returns>Information about the TEE.</returns>
        [HttpGet("tee-info")]
        [ProducesResponseType(typeof(ApiResponse<TeeInfoResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public IActionResult GetTeeInfo()
        {
            try
            {
                // Get the TEE host service
                var teeHostService = HttpContext.RequestServices.GetService(typeof(ITeeHostService)) as ITeeHostService;
                if (teeHostService == null)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Error = new ApiError
                        {
                            Code = "INTERNAL_ERROR",
                            Message = "TEE host service not available"
                        }
                    });
                }

                // Get the TEE information
                var teeInfo = new TeeInfoResponse
                {
                    OpenEnclaveVersion = teeHostService.GetOpenEnclaveVersion(),
                    OcclumSupportEnabled = teeHostService.IsOcclumSupportEnabled(),
                    SimulationMode = teeHostService.IsSimulationMode(),
                    EnclaveConfiguration = teeHostService.GetEnclaveConfiguration()
                };

                return Ok(new ApiResponse<TeeInfoResponse>
                {
                    Success = true,
                    Data = teeInfo,
                    Metadata = new ApiMetadata
                    {
                        RequestId = HttpContext.TraceIdentifier,
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TEE information");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "Error getting TEE information"
                    }
                });
            }
        }

        /// <summary>
        /// Calculates the gas used for a JavaScript execution.
        /// </summary>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        /// <param name="resultSize">The size of the result in bytes.</param>
        /// <returns>The gas used.</returns>
        private static long CalculateGasUsed(long executionTimeMs, int resultSize)
        {
            // Simple gas calculation: 1 gas per millisecond + 1 gas per 100 bytes of result
            return executionTimeMs + (resultSize / 100);
        }

        /// <summary>
        /// Maps a function to a response.
        /// </summary>
        /// <param name="function">The function to map.</param>
        /// <returns>The function response.</returns>
        private static FunctionResponse MapFunctionToResponse(FunctionInfo function)
        {
            return new FunctionResponse
            {
                Id = function.Id,
                Name = function.Name,
                Description = function.Description,
                Version = function.Version,
                CodeHash = function.CodeHash,
                Runtime = function.Runtime.ToString(),
                EntryPoint = function.EntryPoint,
                RequiredSecrets = function.RequiredSecrets,
                GasLimit = function.GasLimit,
                TimeoutMs = function.TimeoutMs,
                MemoryLimit = function.MemoryLimit,
                Status = function.Status.ToString(),
                OwnerId = function.OwnerId,
                CreatedAt = function.CreatedAt,
                UpdatedAt = function.UpdatedAt,
                DeployedAt = function.DeployedAt,
                LastExecutedAt = function.LastExecutedAt,
                ExecutionCount = function.ExecutionCount,
                TotalGasUsed = function.TotalGasUsed,
                AverageGasUsed = function.AverageGasUsed,
                AverageExecutionTimeMs = function.AverageExecutionTimeMs,
                Tags = function.Tags,
                Metadata = function.Metadata
            };
        }

        /// <summary>
        /// Maps an execution record to a response.
        /// </summary>
        /// <param name="record">The execution record to map.</param>
        /// <returns>The execution record response.</returns>
        private static ExecutionRecordResponse MapExecutionRecordToResponse(FunctionExecutionRecord record)
        {
            return new ExecutionRecordResponse
            {
                ExecutionId = record.ExecutionId,
                FunctionId = record.FunctionId,
                FunctionVersion = record.FunctionVersion,
                FunctionName = record.FunctionName,
                UserId = record.UserId,
                Success = record.Success,
                Error = record.Error,
                GasUsed = record.GasUsed,
                ExecutionTimeMs = record.ExecutionTimeMs,
                MemoryUsed = record.MemoryUsed,
                StartTime = record.StartTime,
                EndTime = record.EndTime,
                Metadata = record.Metadata
            };
        }

        /// <summary>
        /// Maps gas usage to a response.
        /// </summary>
        /// <param name="gasUsage">The gas usage to map.</param>
        /// <returns>The gas usage response.</returns>
        private static GasUsageResponse MapGasUsageToResponse(FunctionGasUsage gasUsage)
        {
            return new GasUsageResponse
            {
                Id = gasUsage.Id,
                TotalGasUsed = gasUsage.TotalGasUsed,
                ExecutionCount = gasUsage.ExecutionCount,
                AverageGasUsed = gasUsage.AverageGasUsed,
                GasUsageByDay = gasUsage.GasUsageByDay.ToDictionary(
                    kvp => kvp.Key.ToString("yyyy-MM-dd"),
                    kvp => kvp.Value),
                GasUsageByFunction = gasUsage.GasUsageByFunction,
                GasUsageByUser = gasUsage.GasUsageByUser
            };
        }
    }
}
