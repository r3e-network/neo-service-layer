using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models.Rpc
{
    /// <summary>
    /// Represents a remote procedure call request.
    /// </summary>
    public class RpcRequest
    {
        /// <summary>
        /// Gets or sets the ID of the request.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the method to call.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the method.
        /// </summary>
        public object Parameters { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the request.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the authentication token for the request.
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Gets or sets the user ID for the request.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the roles for the request.
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// Gets or sets the permissions for the request.
        /// </summary>
        public string[] Permissions { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the request.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
