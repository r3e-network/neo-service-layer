using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Interfaces;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing GAS accounting.
    /// </summary>
    [ApiController]
    [Route("api/v1/gas")]
    [Authorize]
    public class GasAccountingController : ControllerBase
    {
        private readonly IGasAccountingService _gasAccountingService;
        private readonly IJavaScriptExecutionService _javaScriptExecutionService;
        private readonly ILogger<GasAccountingController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GasAccountingController"/> class.
        /// </summary>
        /// <param name="gasAccountingService">The GAS accounting service.</param>
        /// <param name="javaScriptExecutionService">The JavaScript execution service.</param>
        /// <param name="logger">The logger.</param>
        public GasAccountingController(
            IGasAccountingService gasAccountingService,
            IJavaScriptExecutionService javaScriptExecutionService,
            ILogger<GasAccountingController> logger)
        {
            _gasAccountingService = gasAccountingService ?? throw new ArgumentNullException(nameof(gasAccountingService));
            _javaScriptExecutionService = javaScriptExecutionService ?? throw new ArgumentNullException(nameof(javaScriptExecutionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the GAS usage for the authenticated user.
        /// </summary>
        /// <returns>The GAS usage.</returns>
        [HttpGet("usage")]
        [ProducesResponseType(typeof(ApiResponse<GasAccountingResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetGasUsage()
        {
            try
            {
                var userId = User.Identity.Name;
                var gasAccounting = await _gasAccountingService.GetUserGasAccountingAsync(userId);

                if (gasAccounting == null)
                {
                    return Ok(new ApiResponse<GasAccountingResponse>
                    {
                        Success = true,
                        Data = new GasAccountingResponse
                        {
                            TotalGasUsed = 0,
                            CurrentPeriodGasUsed = 0,
                            GasLimit = 0,
                            PeriodStart = DateTime.UtcNow.Date,
                            PeriodEnd = DateTime.UtcNow.Date.AddMonths(1).AddDays(-1)
                        }
                    });
                }

                return Ok(new ApiResponse<GasAccountingResponse>
                {
                    Success = true,
                    Data = new GasAccountingResponse
                    {
                        TotalGasUsed = gasAccounting.TotalGasUsed,
                        CurrentPeriodGasUsed = gasAccounting.CurrentPeriodGasUsed,
                        GasLimit = gasAccounting.GasLimit,
                        PeriodStart = gasAccounting.PeriodStart,
                        PeriodEnd = gasAccounting.PeriodEnd
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GAS usage");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Gets the GAS usage records for the authenticated user.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of GAS usage records.</returns>
        [HttpGet("records")]
        [ProducesResponseType(typeof(ApiResponse<GasUsageRecordListResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetGasUsageRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.Identity.Name;
                var (records, totalCount) = await _gasAccountingService.GetUserGasUsageRecordsAsync(userId, page, pageSize);

                var recordResponses = records.Select(r => new GasUsageRecordResponse
                {
                    Id = r.Id,
                    FunctionId = r.FunctionId,
                    ExecutionId = r.ExecutionId,
                    GasUsed = r.GasUsed,
                    ExecutionTime = r.ExecutionTime
                }).ToList();

                return Ok(new ApiResponse<GasUsageRecordListResponse>
                {
                    Success = true,
                    Data = new GasUsageRecordListResponse
                    {
                        Records = recordResponses,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GAS usage records");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Gets the GAS usage for a specific function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <returns>The function GAS usage.</returns>
        [HttpGet("functions/{functionId}")]
        [ProducesResponseType(typeof(ApiResponse<FunctionGasUsageResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetFunctionGasUsage(string functionId)
        {
            try
            {
                if (string.IsNullOrEmpty(functionId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid function ID"
                    });
                }

                var userId = User.Identity.Name;
                var function = await _javaScriptExecutionService.GetFunctionAsync(functionId);

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

                var gasAccounting = await _gasAccountingService.GetFunctionGasAccountingAsync(functionId);
                var (records, _) = await _gasAccountingService.GetFunctionGasUsageRecordsAsync(functionId, 1, 1);

                long lastExecutionGas = 0;
                if (records.Count > 0)
                {
                    lastExecutionGas = records[0].GasUsed;
                }

                long averageGasPerExecution = 0;
                int executionCount = 0;
                if (gasAccounting != null && gasAccounting.TotalGasUsed > 0)
                {
                    var (allRecords, totalCount) = await _gasAccountingService.GetFunctionGasUsageRecordsAsync(functionId, 1, int.MaxValue);
                    executionCount = totalCount;
                    if (executionCount > 0)
                    {
                        averageGasPerExecution = gasAccounting.TotalGasUsed / executionCount;
                    }
                }

                return Ok(new ApiResponse<FunctionGasUsageResponse>
                {
                    Success = true,
                    Data = new FunctionGasUsageResponse
                    {
                        FunctionId = functionId,
                        TotalGasUsed = gasAccounting?.TotalGasUsed ?? 0,
                        AverageGasPerExecution = averageGasPerExecution,
                        ExecutionCount = executionCount,
                        LastExecutionGas = lastExecutionGas
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting function GAS usage");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Sets the GAS limit for the authenticated user.
        /// </summary>
        /// <param name="request">The set GAS limit request.</param>
        /// <returns>The updated GAS accounting.</returns>
        [HttpPost("limit")]
        [ProducesResponseType(typeof(ApiResponse<GasAccountingResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> SetGasLimit([FromBody] SetGasLimitRequest request)
        {
            try
            {
                if (request == null || request.GasLimit <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid GAS limit"
                    });
                }

                var userId = User.Identity.Name;
                var gasAccounting = await _gasAccountingService.SetUserGasLimitAsync(userId, request.GasLimit);

                return Ok(new ApiResponse<GasAccountingResponse>
                {
                    Success = true,
                    Data = new GasAccountingResponse
                    {
                        TotalGasUsed = gasAccounting.TotalGasUsed,
                        CurrentPeriodGasUsed = gasAccounting.CurrentPeriodGasUsed,
                        GasLimit = gasAccounting.GasLimit,
                        PeriodStart = gasAccounting.PeriodStart,
                        PeriodEnd = gasAccounting.PeriodEnd
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting GAS limit");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }
    }
}
