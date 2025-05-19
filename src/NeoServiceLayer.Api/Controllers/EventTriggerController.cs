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
    /// Controller for managing event triggers.
    /// </summary>
    [ApiController]
    [Route("api/v1/triggers")]
    [Authorize]
    public class EventTriggerController : ControllerBase
    {
        private readonly IEventTriggerService _eventTriggerService;
        private readonly IJavaScriptExecutionService _javaScriptExecutionService;
        private readonly ILogger<EventTriggerController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTriggerController"/> class.
        /// </summary>
        /// <param name="eventTriggerService">The event trigger service.</param>
        /// <param name="javaScriptExecutionService">The JavaScript execution service.</param>
        /// <param name="logger">The logger.</param>
        public EventTriggerController(
            IEventTriggerService eventTriggerService,
            IJavaScriptExecutionService javaScriptExecutionService,
            ILogger<EventTriggerController> logger)
        {
            _eventTriggerService = eventTriggerService ?? throw new ArgumentNullException(nameof(eventTriggerService));
            _javaScriptExecutionService = javaScriptExecutionService ?? throw new ArgumentNullException(nameof(javaScriptExecutionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new event trigger.
        /// </summary>
        /// <param name="request">The create trigger request.</param>
        /// <returns>The created trigger.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EventTriggerResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> CreateTrigger([FromBody] CreateEventTriggerRequest request)
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

                // Verify that the function exists and belongs to the user
                var function = await _javaScriptExecutionService.GetFunctionAsync(request.FunctionId);
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

                var trigger = new EventTrigger
                {
                    FunctionId = request.FunctionId,
                    EventType = request.EventType,
                    Filters = request.Filters,
                    InputMapping = request.InputMapping,
                    OwnerId = userId
                };

                var createdTrigger = await _eventTriggerService.CreateTriggerAsync(trigger);

                return Ok(new ApiResponse<EventTriggerResponse>
                {
                    Success = true,
                    Data = new EventTriggerResponse
                    {
                        Id = createdTrigger.Id,
                        FunctionId = createdTrigger.FunctionId,
                        EventType = createdTrigger.EventType,
                        Filters = createdTrigger.Filters,
                        InputMapping = createdTrigger.InputMapping,
                        Status = createdTrigger.Status.ToString(),
                        CreatedAt = createdTrigger.CreatedAt,
                        UpdatedAt = createdTrigger.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event trigger");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Gets an event trigger by ID.
        /// </summary>
        /// <param name="id">The trigger ID.</param>
        /// <returns>The trigger.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<EventTriggerResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetTrigger(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid trigger ID"
                    });
                }

                var userId = User.Identity.Name;
                var trigger = await _eventTriggerService.GetTriggerAsync(id);

                if (trigger == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Trigger not found"
                    });
                }

                if (trigger.OwnerId != userId)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Unauthorized"
                    });
                }

                return Ok(new ApiResponse<EventTriggerResponse>
                {
                    Success = true,
                    Data = new EventTriggerResponse
                    {
                        Id = trigger.Id,
                        FunctionId = trigger.FunctionId,
                        EventType = trigger.EventType,
                        Filters = trigger.Filters,
                        InputMapping = trigger.InputMapping,
                        Status = trigger.Status.ToString(),
                        CreatedAt = trigger.CreatedAt,
                        UpdatedAt = trigger.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event trigger");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Lists all event triggers for the authenticated user.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of event triggers.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<EventTriggerListResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> ListTriggers(
            [FromQuery] string status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.Identity.Name;
                EventTriggerStatus? statusFilter = null;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<EventTriggerStatus>(status, true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }

                var (triggers, totalCount) = await _eventTriggerService.ListTriggersAsync(userId, statusFilter, page, pageSize);

                var triggerResponses = triggers.Select(t => new EventTriggerResponse
                {
                    Id = t.Id,
                    FunctionId = t.FunctionId,
                    EventType = t.EventType,
                    Filters = t.Filters,
                    InputMapping = t.InputMapping,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();

                return Ok(new ApiResponse<EventTriggerListResponse>
                {
                    Success = true,
                    Data = new EventTriggerListResponse
                    {
                        Triggers = triggerResponses,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing event triggers");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "Internal server error"
                });
            }
        }
    }
}
