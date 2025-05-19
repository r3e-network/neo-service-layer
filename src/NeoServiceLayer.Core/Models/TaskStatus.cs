using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the status of a task.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskStatus
    {
        /// <summary>
        /// The task is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// The task is being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// The task has been completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The task has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The task has been cancelled.
        /// </summary>
        Cancelled
    }
}
