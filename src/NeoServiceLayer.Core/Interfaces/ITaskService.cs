using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the task service.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="task">The task to create.</param>
        /// <returns>The created task.</returns>
        Task<Models.Task> CreateTaskAsync(Models.Task task);

        /// <summary>
        /// Gets a task by ID.
        /// </summary>
        /// <param name="taskId">The ID of the task to get.</param>
        /// <returns>The task with the specified ID.</returns>
        Task<Models.Task> GetTaskAsync(string taskId);

        /// <summary>
        /// Gets all tasks for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="type">Optional type filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A list of tasks for the user.</returns>
        Task<(IEnumerable<Models.Task> Tasks, int TotalCount)> GetTasksAsync(string userId, Models.TaskStatus? status = null, Models.TaskType? type = null, int page = 1, int pageSize = 10);

        /// <summary>
        /// Updates a task.
        /// </summary>
        /// <param name="task">The task to update.</param>
        /// <returns>The updated task.</returns>
        Task<Models.Task> UpdateTaskAsync(Models.Task task);

        /// <summary>
        /// Processes a task.
        /// </summary>
        /// <param name="taskId">The ID of the task to process.</param>
        /// <returns>The processed task.</returns>
        Task<Models.Task> ProcessTaskAsync(string taskId);

        /// <summary>
        /// Cancels a task.
        /// </summary>
        /// <param name="taskId">The ID of the task to cancel.</param>
        /// <returns>The cancelled task.</returns>
        Task<Models.Task> CancelTaskAsync(string taskId);
    }
}
