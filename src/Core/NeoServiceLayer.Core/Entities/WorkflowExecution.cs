using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a workflow execution instance in the database.
    /// </summary>
    public class WorkflowExecution
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the workflow definition ID.
        /// </summary>
        public Guid WorkflowDefinitionId { get; set; }

        /// <summary>
        /// Gets or sets the execution status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input data as JSON.
        /// </summary>
        public string? InputData { get; set; }

        /// <summary>
        /// Gets or sets the output data as JSON.
        /// </summary>
        public string? OutputData { get; set; }

        /// <summary>
        /// Gets or sets the current step.
        /// </summary>
        public string? CurrentStep { get; set; }

        /// <summary>
        /// Gets or sets the execution state as JSON.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets any error message.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets when the execution started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the execution completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the workflow definition navigation property.
        /// </summary>
        public WorkflowDefinition? WorkflowDefinition { get; set; }
    }
}
