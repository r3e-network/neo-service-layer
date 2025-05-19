using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Interface for the task repository.
    /// </summary>
    public interface ITaskRepository : IRepository<TaskEntity, string>
    {
        /// <summary>
        /// Gets all tasks for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The tasks for the user.</returns>
        Task<IEnumerable<Core.Models.Task>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Gets a task by its ID.
        /// </summary>
        /// <param name="taskId">The task ID.</param>
        /// <returns>The task, or null if not found.</returns>
        Task<Core.Models.Task> GetTaskByIdAsync(string taskId);

        /// <summary>
        /// Gets all pending tasks.
        /// </summary>
        /// <returns>The pending tasks.</returns>
        Task<IEnumerable<Core.Models.Task>> GetPendingTasksAsync();

        /// <summary>
        /// Adds a task.
        /// </summary>
        /// <param name="task">The task to add.</param>
        /// <returns>The added task.</returns>
        Task<Core.Models.Task> AddTaskAsync(Core.Models.Task task);

        /// <summary>
        /// Updates a task.
        /// </summary>
        /// <param name="task">The task to update.</param>
        /// <returns>The updated task.</returns>
        Task<Core.Models.Task> UpdateTaskAsync(Core.Models.Task task);

        /// <summary>
        /// Deletes a task.
        /// </summary>
        /// <param name="taskId">The task ID.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task DeleteTaskAsync(string taskId);
    }
}
