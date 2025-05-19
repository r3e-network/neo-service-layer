using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository for tasks.
    /// </summary>
    public class TaskRepository : Repository<TaskEntity, string>, ITaskRepository
    {
        /// <summary>
        /// Initializes a new instance of the TaskRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public TaskRepository(NeoServiceLayerDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Core.Models.Task>> GetByUserIdAsync(string userId)
        {
            var entities = await _dbSet
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel());
        }

        /// <inheritdoc/>
        public async Task<Core.Models.Task> GetTaskByIdAsync(string taskId)
        {
            var entity = await _dbSet.FindAsync(taskId);
            return entity?.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Core.Models.Task>> GetPendingTasksAsync()
        {
            var entities = await _dbSet
                .Where(t => t.Status == Core.Models.TaskStatus.Pending)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel());
        }

        /// <inheritdoc/>
        public async Task<Core.Models.Task> AddTaskAsync(Core.Models.Task task)
        {
            var entity = TaskEntity.FromDomainModel(task);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<Core.Models.Task> UpdateTaskAsync(Core.Models.Task task)
        {
            var existingEntity = await _dbSet.FindAsync(task.Id);
            if (existingEntity == null)
            {
                return null;
            }

            // Update properties
            existingEntity.Type = task.Type;
            existingEntity.Status = task.Status;
            existingEntity.UserId = task.UserId;
            existingEntity.StartedAt = task.StartedAt;
            existingEntity.CompletedAt = task.CompletedAt;
            existingEntity.ErrorMessage = task.ErrorMessage ?? "";

            if (task.Data != null && task.Data.Count > 0)
            {
                existingEntity.DataJson = System.Text.Json.JsonSerializer.Serialize(task.Data);
            }
            else
            {
                existingEntity.DataJson = "{}";
            }

            if (task.Result != null && task.Result.Count > 0)
            {
                existingEntity.ResultJson = System.Text.Json.JsonSerializer.Serialize(task.Result);
            }
            else
            {
                existingEntity.ResultJson = "{}";
            }

            await _context.SaveChangesAsync();
            return existingEntity.ToDomainModel();
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task DeleteTaskAsync(string taskId)
        {
            var entity = await _dbSet.FindAsync(taskId);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
