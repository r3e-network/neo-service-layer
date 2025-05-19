using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models.Events
{
    /// <summary>
    /// Represents an event in the enclave.
    /// </summary>
    public class EnclaveEvent
    {
        /// <summary>
        /// Gets or sets the ID of the event.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the data for the event.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the target of the event.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID of the event.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the event.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
