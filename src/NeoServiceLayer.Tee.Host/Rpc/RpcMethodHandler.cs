using System;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Rpc;

namespace NeoServiceLayer.Tee.Host.Rpc
{
    /// <summary>
    /// Represents a handler for an RPC method.
    /// </summary>
    public class RpcMethodHandler
    {
        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the handler for the method.
        /// </summary>
        public Func<RpcRequest, Task<RpcResponse>> Handler { get; set; }

        /// <summary>
        /// Gets or sets whether authentication is required for the method.
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets whether encryption is required for the method.
        /// </summary>
        public bool RequireEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the required roles for the method.
        /// </summary>
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the required permissions for the method.
        /// </summary>
        public string[] RequiredPermissions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the timeout for the method in milliseconds.
        /// </summary>
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries for the method.
        /// </summary>
        public int? MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public int? RetryDelayMs { get; set; }
    }
}
