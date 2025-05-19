using System;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Represents an event in the enclave system.
    /// </summary>
    public class EnclaveEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event.
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
        /// Gets or sets the timestamp for the event.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
