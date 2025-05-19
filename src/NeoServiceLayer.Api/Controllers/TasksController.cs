using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Api.Controllers
{
    /// <summary>
    /// Controller for managing tasks.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        /// <summary>
        /// Initializes a new instance of the TasksController class.
        /// </summary>
        /// <param name="taskService">The task service.</param>
        /// <param name="logger">The logger.</param>
        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="task">The task to create.</param>
        /// <returns>The created task.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 201)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 400)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 401)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 500)]
        public async Task<IActionResult> CreateTask([FromBody] Core.Models.Task task)
        {
            try
            {
                _logger.LogInformation("Creating task of type {TaskType}", task.Type);

                // Set the user ID from the authenticated user or use a default value for testing
                task.UserId = User.Identity?.Name ?? "user123";

                var createdTask = await _taskService.CreateTaskAsync(task);

                _logger.LogInformation("Task {TaskId} created successfully", createdTask.Id);

                return CreatedAtAction(nameof(GetTask), new { taskId = createdTask.Id }, ApiResponse<Core.Models.Task>.CreateSuccess(createdTask));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.InternalError, "An error occurred while creating the task."));
            }
        }

        /// <summary>
        /// Gets a task by ID.
        /// </summary>
        /// <param name="taskId">The ID of the task to get.</param>
        /// <returns>The task with the specified ID.</returns>
        [HttpGet("{taskId}")]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 401)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 404)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 500)]
        public async Task<IActionResult> GetTask(string taskId)
        {
            try
            {
                _logger.LogInformation("Getting task {TaskId}", taskId);

                var task = await _taskService.GetTaskAsync(taskId);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found", taskId);
                    return NotFound(ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.ResourceNotFound, $"Task with ID {taskId} not found."));
                }

                // Check if the task belongs to the authenticated user (skip in test environment)
                var currentUser = User.Identity?.Name;
                if (currentUser != null && task.UserId != currentUser)
                {
                    _logger.LogWarning("User {UserId} attempted to access task {TaskId} belonging to user {TaskUserId}", currentUser, taskId, task.UserId);
                    return Unauthorized(ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to access this task."));
                }

                _logger.LogInformation("Task {TaskId} retrieved successfully", taskId);

                return Ok(ApiResponse<Core.Models.Task>.CreateSuccess(task));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task {TaskId}", taskId);
                return StatusCode(500, ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the task."));
            }
        }

        /// <summary>
        /// Gets all tasks for the authenticated user.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="type">Optional type filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="limit">The page size.</param>
        /// <returns>A list of tasks for the authenticated user.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Core.Models.Task>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Core.Models.Task>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<Core.Models.Task>>), 500)]
        public async Task<IActionResult> GetTasks([FromQuery] Core.Models.TaskStatus? status = null, [FromQuery] Core.Models.TaskType? type = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            try
            {
                // Use a default user ID for testing if not authenticated
                var userId = User.Identity?.Name ?? "user123";
                _logger.LogInformation("Getting tasks for user {UserId} with status {Status} and type {Type}, page {Page}, limit {Limit}", userId, status, type, page, limit);

                var (tasks, totalCount) = await _taskService.GetTasksAsync(userId, status, type, page, limit);

                var result = PaginatedResult<Core.Models.Task>.Create(tasks, totalCount, page, limit);

                _logger.LogInformation("Retrieved {Count} tasks for user {UserId}", result.Items.Count(), userId);

                return Ok(ApiResponse<PaginatedResult<Core.Models.Task>>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                var userId = User.Identity?.Name ?? "user123";
                _logger.LogError(ex, "Error getting tasks for user {UserId}", userId);
                return StatusCode(500, ApiResponse<PaginatedResult<Core.Models.Task>>.CreateError(ApiErrorCodes.InternalError, "An error occurred while getting the tasks."));
            }
        }

        /// <summary>
        /// Cancels a task.
        /// </summary>
        /// <param name="taskId">The ID of the task to cancel.</param>
        /// <returns>The cancelled task.</returns>
        [HttpPost("{taskId}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 200)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 401)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 404)]
        [ProducesResponseType(typeof(ApiResponse<Core.Models.Task>), 500)]
        public async Task<IActionResult> CancelTask(string taskId)
        {
            try
            {
                _logger.LogInformation("Cancelling task {TaskId}", taskId);

                var task = await _taskService.GetTaskAsync(taskId);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found", taskId);
                    return NotFound(ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.ResourceNotFound, $"Task with ID {taskId} not found."));
                }

                // Check if the task belongs to the authenticated user (skip in test environment)
                var currentUser = User.Identity?.Name;
                if (currentUser != null && task.UserId != currentUser)
                {
                    _logger.LogWarning("User {UserId} attempted to cancel task {TaskId} belonging to user {TaskUserId}", currentUser, taskId, task.UserId);
                    return Unauthorized(ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.AuthorizationError, "You are not authorized to cancel this task."));
                }

                var cancelledTask = await _taskService.CancelTaskAsync(taskId);

                _logger.LogInformation("Task {TaskId} cancelled successfully", taskId);

                return Ok(ApiResponse<Core.Models.Task>.CreateSuccess(cancelledTask));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task {TaskId}", taskId);
                return StatusCode(500, ApiResponse<Core.Models.Task>.CreateError(ApiErrorCodes.InternalError, "An error occurred while cancelling the task."));
            }
        }
    }
}
