using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Integration.Tests.Mocks
{
    public class MockTaskService : ITaskService
    {
        private readonly ILogger<MockTaskService> _logger;
        private readonly List<Core.Models.Task> _tasks = new List<Core.Models.Task>();

        public MockTaskService(ILogger<MockTaskService> logger)
        {
            _logger = logger;

            // Add some sample tasks
            _tasks.Add(new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Status = Core.Models.TaskStatus.Completed,
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
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                StartedAt = DateTime.UtcNow.AddMinutes(-9),
                CompletedAt = DateTime.UtcNow.AddMinutes(-8)
            });

            _tasks.Add(new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user123",
                Type = TaskType.DataProcessing,
                Status = Core.Models.TaskStatus.Pending,
                Data = new Dictionary<string, object>
                {
                    { "data_id", "data123" },
                    { "operation", "encrypt" }
                },
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
        }

        public Task<Core.Models.Task> CreateTaskAsync(Core.Models.Task task)
        {
            _logger.LogInformation("Creating task of type {TaskType}", task.Type);

            // Set task properties
            task.Id = Guid.NewGuid().ToString();
            task.Status = Core.Models.TaskStatus.Pending;
            task.CreatedAt = DateTime.UtcNow;

            _tasks.Add(task);

            // Simulate async processing
            Task.Run(async () =>
            {
                await Task.Delay(2000); // Simulate processing time
                task.Status = Core.Models.TaskStatus.Processing;
                task.StartedAt = DateTime.UtcNow;

                await Task.Delay(3000); // Simulate execution time
                task.Status = Core.Models.TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.Result = new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "task_executed" }
                };
            });

            return System.Threading.Tasks.Task.FromResult(task);
        }

        public Task<Core.Models.Task> GetTaskAsync(string taskId)
        {
            _logger.LogInformation("Getting task {TaskId}", taskId);

            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            return System.Threading.Tasks.Task.FromResult(task!);
        }

        public Task<(IEnumerable<Core.Models.Task> Tasks, int TotalCount)> GetTasksAsync(string userId, Core.Models.TaskStatus? status = null, TaskType? type = null, int page = 1, int limit = 10)
        {
            _logger.LogInformation("Getting tasks for user {UserId} with status {Status} and type {Type}, page {Page}, limit {Limit}", userId, status, type, page, limit);

            var query = _tasks.Where(t => t.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            var totalCount = query.Count();
            var tasks = query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return System.Threading.Tasks.Task.FromResult((tasks.AsEnumerable(), totalCount));
        }

        public Task<Core.Models.Task> CancelTaskAsync(string taskId)
        {
            _logger.LogInformation("Cancelling task {TaskId}", taskId);

            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && (task.Status == Core.Models.TaskStatus.Pending || task.Status == Core.Models.TaskStatus.Processing))
            {
                task.Status = Core.Models.TaskStatus.Cancelled;
                task.CompletedAt = DateTime.UtcNow;
            }

            return Task.FromResult(task!);
        }

        public Task<Core.Models.Task> UpdateTaskAsync(Core.Models.Task task)
        {
            _logger.LogInformation("Updating task {TaskId}", task.Id);

            var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existingTask != null)
            {
                var index = _tasks.IndexOf(existingTask);
                _tasks[index] = task;
                return Task.FromResult(task);
            }

            // Return a default task if not found
            return Task.FromResult<Core.Models.Task>(new Core.Models.Task
            {
                Id = task.Id,
                UserId = task.UserId,
                Type = task.Type,
                Status = Core.Models.TaskStatus.Failed,
                CreatedAt = DateTime.UtcNow,
                Result = new Dictionary<string, object> { { "error", "Task not found" } }
            });
        }

        public Task<Core.Models.Task> ProcessTaskAsync(string taskId)
        {
            _logger.LogInformation("Processing task {TaskId}", taskId);

            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.Status == Core.Models.TaskStatus.Pending)
            {
                task.Status = Core.Models.TaskStatus.Processing;
                task.StartedAt = DateTime.UtcNow;

                // Simulate processing
                task.Status = Core.Models.TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.Result = new Dictionary<string, object>
                {
                    { "result", "success" },
                    { "output", "task_processed" }
                };

                return Task.FromResult(task!);
            }

            return Task.FromResult(task!);
        }
    }
}
