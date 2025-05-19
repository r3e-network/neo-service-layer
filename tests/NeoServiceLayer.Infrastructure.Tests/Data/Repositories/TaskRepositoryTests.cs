using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Data.Entities;
using TaskStatus = NeoServiceLayer.Core.Models.TaskStatus;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Data.Repositories
{
    public class TaskRepositoryTests
    {
        private readonly DbContextOptions<NeoServiceLayerDbContext> _options;

        public TaskRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<NeoServiceLayerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddTaskAsync_ShouldAddTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                Type = TaskType.Compute,
                Status = TaskStatus.Pending,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                Data = new Dictionary<string, object> { { "input", "test" } },
                DataJson = "{\"input\":\"test\"}",
                Result = new Dictionary<string, object>(),
                ResultJson = "",
                ErrorMessage = ""
            };

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TaskRepository(context);
                await repository.AddTaskAsync(task);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var savedTask = await context.Tasks.FindAsync(task.Id);
                Assert.NotNull(savedTask);
                Assert.Equal(task.Type, savedTask.Type);
                Assert.Equal(task.Status, savedTask.Status);
                Assert.Equal(task.UserId, savedTask.UserId);
                Assert.Equal(task.DataJson, savedTask.DataJson);
            }
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                Type = TaskType.Compute,
                Status = TaskStatus.Pending,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                Data = new Dictionary<string, object> { { "input", "test" } },
                DataJson = "{\"input\":\"test\"}",
                Result = new Dictionary<string, object>(),
                ResultJson = "",
                ErrorMessage = ""
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.Tasks.Add(TaskEntity.FromDomainModel(task));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TaskRepository(context);
                var result = await repository.GetTaskByIdAsync(task.Id);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(task.Id, result.Id);
                Assert.Equal(task.Type, result.Type);
                Assert.Equal(task.Status, result.Status);
                Assert.Equal(task.UserId, result.UserId);
                Assert.Equal(task.Data["input"].ToString(), result.Data["input"].ToString());
            }
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserTasks()
        {
            // Arrange
            var userId = "user1";
            var tasks = new List<Core.Models.Task>
            {
                new Core.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = TaskType.Compute,
                    Status = TaskStatus.Pending,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { { "input", "test1" } },
                    DataJson = "{\"input\":\"test1\"}",
                    Result = new Dictionary<string, object>(),
                    ResultJson = "",
                    ErrorMessage = ""
                },
                new Core.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = TaskType.DataProcessing,
                    Status = TaskStatus.Completed,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { { "input", "test2" } },
                    DataJson = "{\"input\":\"test2\"}",
                    Result = new Dictionary<string, object>(),
                    ResultJson = "",
                    ErrorMessage = ""
                },
                new Core.Models.Task
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = TaskType.Compute,
                    Status = TaskStatus.Pending,
                    UserId = "user2", // Different user
                    CreatedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { { "input", "test3" } },
                    DataJson = "{\"input\":\"test3\"}",
                    Result = new Dictionary<string, object>(),
                    ResultJson = "",
                    ErrorMessage = ""
                }
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var entities = tasks.Select(t => TaskEntity.FromDomainModel(t)).ToList();
                context.Tasks.AddRange(entities);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TaskRepository(context);
                var result = await repository.GetByUserIdAsync(userId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                Assert.All(result, t => Assert.Equal(userId, t.UserId));
                Assert.Contains(result, t => t.Data.ContainsKey("input") && t.Data["input"].ToString() == "test1");
                Assert.Contains(result, t => t.Data.ContainsKey("input") && t.Data["input"].ToString() == "test2");
                Assert.DoesNotContain(result, t => t.Data.ContainsKey("input") && t.Data["input"].ToString() == "test3");
            }
        }

        [Fact]
        public async Task UpdateTaskAsync_ShouldUpdateTask()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                Type = TaskType.Compute,
                Status = TaskStatus.Pending,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                Data = new Dictionary<string, object> { { "input", "test" } },
                DataJson = "{\"input\":\"test\"}",
                Result = new Dictionary<string, object>(),
                ResultJson = "",
                ErrorMessage = ""
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.Tasks.Add(TaskEntity.FromDomainModel(task));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TaskRepository(context);
                var taskToUpdate = await repository.GetTaskByIdAsync(task.Id);
                taskToUpdate.Status = TaskStatus.Completed;
                taskToUpdate.Result = new Dictionary<string, object> { { "output", "result" } };
                taskToUpdate.CompletedAt = DateTime.UtcNow;

                await repository.UpdateTaskAsync(taskToUpdate);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var updatedTask = await context.Tasks.FindAsync(task.Id);
                Assert.NotNull(updatedTask);
                Assert.Equal(TaskStatus.Completed, updatedTask.Status);
                var repository = new TaskRepository(context);
                var taskModel = await repository.GetTaskByIdAsync(task.Id);
                Assert.Equal("result", taskModel.Result["output"].ToString());
                Assert.NotNull(updatedTask.CompletedAt);
            }
        }
    }
}
