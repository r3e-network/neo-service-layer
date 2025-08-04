using System;

namespace NeoServiceLayer.Core
{
    /// <summary>
    /// Represents a service health check record in the database.
    /// </summary>
    public class ServiceHealthCheck
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
        /// Gets or sets the health status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        public int ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets additional check details as JSON.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets when the check was performed.
        /// </summary>
        public DateTime CheckTime { get; set; }
    }
}
