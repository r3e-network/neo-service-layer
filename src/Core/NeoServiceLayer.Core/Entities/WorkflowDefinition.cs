using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a workflow definition in the database.
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the workflow name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the workflow description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the workflow definition as JSON.
        /// </summary>
        public string Definition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the workflow version.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets whether the workflow is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the workflow trigger type.
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the workflow was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the workflow was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
