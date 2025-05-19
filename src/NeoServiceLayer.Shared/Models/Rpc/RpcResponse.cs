using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models.Rpc
{
    /// <summary>
    /// Represents a remote procedure call response.
    /// </summary>
    public class RpcResponse
    {
        /// <summary>
        /// Gets or sets the ID of the request that this response is for.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the result of the method call.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Gets or sets the error message if the method call failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the response.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the gas used by the method call.
        /// </summary>
        public long GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the response.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets whether the method call was successful.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}
