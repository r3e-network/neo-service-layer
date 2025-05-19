using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Infrastructure.Data.Entities
{
    /// <summary>
    /// Entity class for tasks.
    /// </summary>
    public class TaskEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier of the task.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the task.
        /// </summary>
        [Required]
        public TaskType Type { get; set; }

        /// <summary>
        /// Gets or sets the status of the task.
        /// </summary>
        [Required]
        public Core.Models.TaskStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the task.
        /// </summary>
        [Required]
        public string DataJson { get; set; } = "";

        /// <summary>
        /// Gets or sets the result of the task.
        /// </summary>
        [Required]
        public string ResultJson { get; set; } = "";

        /// <summary>
        /// Gets or sets the ID of the user who created the task.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the time when the task was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the task was started.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the task was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if the task failed.
        /// </summary>
        [Required]
        public string ErrorMessage { get; set; } = "";

        /// <summary>
        /// Converts the entity to a domain model.
        /// </summary>
        /// <returns>The domain model.</returns>
        public Core.Models.Task ToDomainModel()
        {
            var task = new Core.Models.Task
            {
                Id = Id,
                Type = Type,
                Status = Status,
                UserId = UserId,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                CompletedAt = CompletedAt,
                ErrorMessage = ErrorMessage
            };

            if (!string.IsNullOrEmpty(DataJson))
            {
                task.Data = JsonSerializer.Deserialize<Dictionary<string, object>>(DataJson);
            }
            else
            {
                task.Data = new Dictionary<string, object>();
            }

            if (!string.IsNullOrEmpty(ResultJson))
            {
                task.Result = JsonSerializer.Deserialize<Dictionary<string, object>>(ResultJson);
            }
            else
            {
                task.Result = new Dictionary<string, object>();
            }

            return task;
        }

        /// <summary>
        /// Creates an entity from a domain model.
        /// </summary>
        /// <param name="task">The domain model.</param>
        /// <returns>The entity.</returns>
        public static TaskEntity FromDomainModel(Core.Models.Task task)
        {
            var entity = new TaskEntity
            {
                Id = task.Id,
                Type = task.Type,
                Status = task.Status,
                UserId = task.UserId,
                CreatedAt = task.CreatedAt,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt,
                ErrorMessage = task.ErrorMessage
            };

            if (task.Data != null && task.Data.Count > 0)
            {
                entity.DataJson = JsonSerializer.Serialize(task.Data);
            }
            else
            {
                entity.DataJson = "{}";
            }

            if (task.Result != null && task.Result.Count > 0)
            {
                entity.ResultJson = JsonSerializer.Serialize(task.Result);
            }
            else
            {
                entity.ResultJson = "{}";
            }

            if (string.IsNullOrEmpty(entity.ErrorMessage))
            {
                entity.ErrorMessage = "";
            }

            return entity;
        }
    }
}
