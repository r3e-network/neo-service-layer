using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a service configuration entity in the database.
    /// </summary>
    public class ServiceConfigurationEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the service is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the configuration JSON.
        /// </summary>
        public string? Configuration { get; set; }

        /// <summary>
        /// Gets or sets when the configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the configuration was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
