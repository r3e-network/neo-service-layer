using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Shared.Models.Logging
{
    /// <summary>
    /// Represents a secure log entry.
    /// </summary>
    public class SecureLogEntry
    {
        /// <summary>
        /// Gets or sets the ID of the log entry.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ID of the enclave that generated the log entry.
        /// </summary>
        public string EnclaveId { get; set; }

        /// <summary>
        /// Gets or sets the source of the log entry.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the exception that was logged.
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Gets or sets the additional properties for the log entry.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
