using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Integration.Tests.Models
{
    /// <summary>
    /// Represents the health status of the service.
    /// </summary>
    public class HealthStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the service is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the status of the service.
        /// </summary>
        public string Status { get; set; } = "healthy";

        /// <summary>
        /// Gets or sets the timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the version of the service.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the uptime of the service.
        /// </summary>
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// Represents a detailed health status of the service.
    /// </summary>
    public class DetailedHealthStatus : HealthStatus
    {
        /// <summary>
        /// Gets or sets the status of the database.
        /// </summary>
        public bool DatabaseStatus { get; set; }

        /// <summary>
        /// Gets or sets the status of the TEE.
        /// </summary>
        public bool TeeStatus { get; set; }

        /// <summary>
        /// Gets or sets the status of the blockchain.
        /// </summary>
        public bool BlockchainStatus { get; set; }

        /// <summary>
        /// Gets or sets the detailed component statuses.
        /// </summary>
        public Dictionary<string, ComponentStatus> Components { get; set; }
    }

    /// <summary>
    /// Represents the status of a component.
    /// </summary>
    public class ComponentStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the component is healthy.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the status of the component.
        /// </summary>
        public string Status { get; set; } = "healthy";

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the status check.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
