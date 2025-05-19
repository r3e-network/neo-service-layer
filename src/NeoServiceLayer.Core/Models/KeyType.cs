using System.Text.Json.Serialization;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the type of a cryptographic key.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KeyType
    {
        /// <summary>
        /// ECDSA key.
        /// </summary>
        ECDSA,

        /// <summary>
        /// ED25519 key.
        /// </summary>
        ED25519,

        /// <summary>
        /// RSA key.
        /// </summary>
        RSA,

        /// <summary>
        /// AES key.
        /// </summary>
        AES,

        /// <summary>
        /// HMAC key.
        /// </summary>
        HMAC
    }
}
