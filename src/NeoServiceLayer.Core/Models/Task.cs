using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents a computation task in the Neo Service Layer.
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Gets or sets the unique identifier of the task.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the task.
        /// </summary>
        public TaskType Type { get; set; }

        /// <summary>
        /// Gets or sets the status of the task.
        /// </summary>
        public TaskStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the task.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets the data associated with the task as a JSON string.
        /// </summary>
        public string DataJson { get; set; }

        /// <summary>
        /// Gets or sets the result of the task.
        /// </summary>
        public Dictionary<string, object> Result { get; set; }

        /// <summary>
        /// Gets or sets the result of the task as a JSON string.
        /// </summary>
        public string ResultJson { get; set; }

        /// <summary>
        /// Gets or sets the error message if the task failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the URL to call when the task is completed.
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the task.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the time when the task was created.
        /// </summary>
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
        /// Gets or sets the attestation proof associated with the task.
        /// </summary>
        public string AttestationProof { get; set; }

        /// <summary>
        /// Creates a new instance of the Task class.
        /// </summary>
        public Task()
        {
            Id = Guid.NewGuid().ToString();
            Status = TaskStatus.Pending;
            Data = new Dictionary<string, object>();
            Result = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
