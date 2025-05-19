using System;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Shared.Models
{
    /// <summary>
    /// Represents a message exchanged between the host and the enclave.
    /// </summary>
    public class TeeMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier of the message.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public TeeMessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the data in the message.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the signature of the message.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the time when the message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Creates a new instance of the TeeMessage class.
        /// </summary>
        public TeeMessage()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new instance of the TeeMessage class with the specified type and data.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <param name="data">The data in the message.</param>
        /// <returns>A new instance of the TeeMessage class.</returns>
        public static TeeMessage Create(TeeMessageType type, string data)
        {
            return new TeeMessage
            {
                Type = type,
                Data = data
            };
        }
    }

    /// <summary>
    /// Represents the type of a TEE message.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TeeMessageType
    {
        /// <summary>
        /// Task execution message.
        /// </summary>
        TaskExecution,

        /// <summary>
        /// Key management message.
        /// </summary>
        KeyManagement,

        /// <summary>
        /// Attestation message.
        /// </summary>
        Attestation,

        /// <summary>
        /// Randomness message.
        /// </summary>
        Randomness,

        /// <summary>
        /// Compliance message.
        /// </summary>
        Compliance,

        /// <summary>
        /// Event message.
        /// </summary>
        Event,

        /// <summary>
        /// Heartbeat message.
        /// </summary>
        Heartbeat,

        /// <summary>
        /// Error message.
        /// </summary>
        Error,

        /// <summary>
        /// JavaScript execution message.
        /// </summary>
        JavaScriptExecution
    }
}
