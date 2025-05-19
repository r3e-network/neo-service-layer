using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Api.Controllers;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using Xunit;

namespace NeoServiceLayer.Api.Tests.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskService> _mockTaskService;
        private readonly Mock<ILogger<TasksController>> _mockLogger;
        private readonly TasksController _controller;
        private readonly string _userId = "user123";

        public TasksControllerTests()
        {
            _mockTaskService = new Mock<ITaskService>();
            _mockLogger = new Mock<ILogger<TasksController>>();
            _controller = new TasksController(_mockTaskService.Object, _mockLogger.Object);

            // Setup controller context with authenticated user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, _userId),
                new Claim(ClaimTypes.NameIdentifier, _userId),
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateTask_ValidTask_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            var createdTask = new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                UserId = _userId,
                Type = task.Type,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Pending,
                Data = task.Data,
                CreatedAt = DateTime.UtcNow
            };

            _mockTaskService.Setup(x => x.CreateTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _controller.CreateTask(task);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetTask), createdAtActionResult.ActionName);
            Assert.Equal(createdTask.Id, createdAtActionResult.RouteValues["taskId"]);

            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(createdAtActionResult.Value);
            Assert.True(response.Success);
            Assert.Equal(createdTask.Id, response.Data.Id);
            Assert.Equal(_userId, response.Data.UserId);
            Assert.Equal(task.Type, response.Data.Type);
            Assert.Equal(NeoServiceLayer.Core.Models.TaskStatus.Pending, response.Data.Status);
            Assert.Equal(task.Data, response.Data.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTask_ExistingTaskId_ReturnsOkResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();
            var task = new Core.Models.Task
            {
                Id = taskId,
                UserId = _userId,
                Type = TaskType.SmartContractExecution,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Completed,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                },
                Result = new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                },
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                StartedAt = DateTime.UtcNow.AddMinutes(-4),
                CompletedAt = DateTime.UtcNow.AddMinutes(-3)
            };

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(taskId, response.Data.Id);
            Assert.Equal(_userId, response.Data.UserId);
            Assert.Equal(TaskType.SmartContractExecution, response.Data.Type);
            Assert.Equal(NeoServiceLayer.Core.Models.TaskStatus.Completed, response.Data.Status);
            Assert.Equal("success", response.Data.Result["result"]);
            Assert.Equal("contract_executed", response.Data.Result["output"]);
            Assert.Equal(1000, response.Data.Result["gas_used"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTask_NonExistingTaskId_ReturnsNotFoundResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync((Core.Models.Task)null);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ApiErrorCodes.ResourceNotFound, response.Error.Code);
            Assert.Contains(taskId, response.Error.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTask_TaskBelongingToAnotherUser_ReturnsUnauthorizedResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();
            var task = new Core.Models.Task
            {
                Id = taskId,
                UserId = "another-user",
                Type = TaskType.SmartContractExecution,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Completed,
                Data = new Dictionary<string, object>(),
                Result = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ApiErrorCodes.AuthorizationError, response.Error.Code);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTasks_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var tasks = new List<Core.Models.Task>
            {
                new Core.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = _userId,
                    Type = TaskType.SmartContractExecution,
                    Status = NeoServiceLayer.Core.Models.TaskStatus.Completed,
                    Data = new Dictionary<string, object>(),
                    Result = new Dictionary<string, object>(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new Core.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = _userId,
                    Type = TaskType.DataProcessing,
                    Status = NeoServiceLayer.Core.Models.TaskStatus.Pending,
                    Data = new Dictionary<string, object>(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            _mockTaskService.Setup(x => x.GetTasksAsync(_userId, null, null, 1, 10))
                .ReturnsAsync((tasks, tasks.Count));

            // Act
            var result = await _controller.GetTasks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PaginatedResult<Core.Models.Task>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data.Pagination.Total);
            Assert.Equal(1, response.Data.Pagination.Page);
            Assert.Equal(10, response.Data.Pagination.Limit);
            Assert.Equal(1, response.Data.Pagination.Pages);
            Assert.Equal(2, response.Data.Items.Count());
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelTask_PendingTask_ReturnsOkResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();
            var task = new Core.Models.Task
            {
                Id = taskId,
                UserId = _userId,
                Type = TaskType.SmartContractExecution,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Pending,
                Data = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            var cancelledTask = new Core.Models.Task
            {
                Id = taskId,
                UserId = _userId,
                Type = TaskType.SmartContractExecution,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Cancelled,
                Data = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync(task);

            _mockTaskService.Setup(x => x.CancelTaskAsync(taskId))
                .ReturnsAsync(cancelledTask);

            // Act
            var result = await _controller.CancelTask(taskId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(taskId, response.Data.Id);
            Assert.Equal(_userId, response.Data.UserId);
            Assert.Equal(NeoServiceLayer.Core.Models.TaskStatus.Cancelled, response.Data.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelTask_NonExistingTaskId_ReturnsNotFoundResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync((Core.Models.Task)null);

            // Act
            var result = await _controller.CancelTask(taskId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ApiErrorCodes.ResourceNotFound, response.Error.Code);
            Assert.Contains(taskId, response.Error.Message);
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelTask_TaskBelongingToAnotherUser_ReturnsUnauthorizedResult()
        {
            // Arrange
            var taskId = Guid.NewGuid().ToString();
            var task = new Core.Models.Task
            {
                Id = taskId,
                UserId = "another-user",
                Type = TaskType.SmartContractExecution,
                Status = NeoServiceLayer.Core.Models.TaskStatus.Pending,
                Data = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            _mockTaskService.Setup(x => x.GetTaskAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.CancelTask(taskId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Core.Models.Task>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ApiErrorCodes.AuthorizationError, response.Error.Code);
        }
    }
}
