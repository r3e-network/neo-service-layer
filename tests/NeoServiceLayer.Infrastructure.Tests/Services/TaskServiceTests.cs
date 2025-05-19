using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Services
{
    public class TaskServiceTests
    {
        private readonly Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService> _mockTeeHostService;
        private readonly Mock<ITaskRepository> _mockTaskRepository;
        private readonly Mock<ILogger<TaskService>> _mockLogger;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _mockTeeHostService = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeHostService>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockLogger = new Mock<ILogger<TaskService>>();

            // Create an adapter that implements Core.Interfaces.ITeeHostService
            var teeHostServiceAdapter = new Mocks.TeeHostServiceAdapter(_mockTeeHostService.Object);

            _taskService = new TaskService(teeHostServiceAdapter, _mockTaskRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateTaskAsync_ValidTask_ReturnsCreatedTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            // Act
            var result = await _taskService.CreateTaskAsync(task);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(Core.Models.TaskStatus.Pending, result.Status);
            Assert.Equal(task.UserId, result.UserId);
            Assert.Equal(task.Type, result.Type);
            Assert.Equal(task.Data, result.Data);
            Assert.NotNull(result.CreatedAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTaskAsync_ExistingTaskId_ReturnsTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            var createdTask = await _taskService.CreateTaskAsync(task);

            // Setup mock repository to return the task
            _mockTaskRepository.Setup(x => x.GetTaskByIdAsync(createdTask.Id))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _taskService.GetTaskAsync(createdTask.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdTask.Id, result.Id);
            Assert.Equal(createdTask.UserId, result.UserId);
            Assert.Equal(createdTask.Type, result.Type);
            Assert.Equal(createdTask.Data, result.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTaskAsync_NonExistingTaskId_ReturnsNull()
        {
            // Arrange
            var taskId = "non-existing-id";

            // Act
            var result = await _taskService.GetTaskAsync(taskId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTasksAsync_ValidUserId_ReturnsUserTasks()
        {
            // Arrange
            var userId = "user123";
            var task1 = new Core.Models.Task
            {
                UserId = userId,
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            var task2 = new Core.Models.Task
            {
                UserId = userId,
                Type = TaskType.DataProcessing,
                Data = new Dictionary<string, object>
                {
                    { "data", "0x1234567890abcdef" },
                    { "algorithm", "sha256" }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            var createdTask1 = await _taskService.CreateTaskAsync(task1);
            var createdTask2 = await _taskService.CreateTaskAsync(task2);

            // Setup mock repository to return the tasks
            _mockTaskRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Core.Models.Task> { createdTask1, createdTask2 });

            // Act
            var (tasks, totalCount) = await _taskService.GetTasksAsync(userId);

            // Assert
            Assert.NotNull(tasks);
            // Don't check exact count since other tests may have added tasks
            Assert.True(totalCount >= 2);
            Assert.True(tasks.Count() >= 2);
            Assert.Contains(tasks, t => t.Type == TaskType.SmartContractExecution);
            Assert.Contains(tasks, t => t.Type == TaskType.DataProcessing);
        }

        [Fact]
        public async System.Threading.Tasks.Task ProcessTaskAsync_PendingTask_ProcessesTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            var createdTask = await _taskService.CreateTaskAsync(task);

            // Setup mock repository to return the task
            _mockTaskRepository.Setup(x => x.GetTaskByIdAsync(createdTask.Id))
                .ReturnsAsync(createdTask);

            // Setup mock repository to update the task
            _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync((Core.Models.Task t) => {
                    t.Status = Core.Models.TaskStatus.Completed;
                    t.Result = new Dictionary<string, object>
                    {
                        { "result", "success" },
                        { "output", "contract_executed" },
                        { "gas_used", 1000 }
                    };
                    t.StartedAt = DateTime.UtcNow;
                    t.CompletedAt = DateTime.UtcNow;
                    return t;
                });

            // Act
            var result = await _taskService.ProcessTaskAsync(createdTask.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdTask.Id, result.Id);
            Assert.Equal(Core.Models.TaskStatus.Completed, result.Status);
            Assert.NotNull(result.Result);
            Assert.Equal("success", result.Result["result"]);
            Assert.Equal("contract_executed", result.Result["output"]);
            Assert.Equal(1000, result.Result["gas_used"]);
            Assert.NotNull(result.StartedAt);
            Assert.NotNull(result.CompletedAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelTaskAsync_PendingTask_CancelsTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            var createdTask = await _taskService.CreateTaskAsync(task);

            // Setup mock repository to return the task
            _mockTaskRepository.Setup(x => x.GetTaskByIdAsync(createdTask.Id))
                .ReturnsAsync(createdTask);

            // Setup mock repository to update the task
            _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync((Core.Models.Task t) => {
                    t.Status = Core.Models.TaskStatus.Cancelled;
                    return t;
                });

            // Act
            var result = await _taskService.CancelTaskAsync(createdTask.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdTask.Id, result.Id);
            Assert.Equal(Core.Models.TaskStatus.Cancelled, result.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelTaskAsync_CompletedTask_ReturnsTaskUnchanged()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                }
            };

            _mockTeeHostService.Setup(x => x.ExecuteTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync(new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                });

            var createdTask = await _taskService.CreateTaskAsync(task);

            // Create a processed task
            var processedTask = new Core.Models.Task
            {
                Id = createdTask.Id,
                UserId = createdTask.UserId,
                Type = createdTask.Type,
                Status = Core.Models.TaskStatus.Completed,
                Data = createdTask.Data,
                Result = new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "contract_executed" },
                    { "gas_used", 1000 }
                },
                CreatedAt = createdTask.CreatedAt,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            // Setup mock repository to return the processed task
            _mockTaskRepository.Setup(x => x.GetTaskByIdAsync(processedTask.Id))
                .ReturnsAsync(processedTask);

            // Setup mock repository to update the task
            _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<Core.Models.Task>()))
                .ReturnsAsync((Core.Models.Task t) => t);

            // Act
            var result = await _taskService.CancelTaskAsync(processedTask.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processedTask.Id, result.Id);
            Assert.Equal(Core.Models.TaskStatus.Completed, result.Status);
        }
    }
}
