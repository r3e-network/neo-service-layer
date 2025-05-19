namespace NeoServiceLayer.Tee.Host.Rpc
{
    /// <summary>
    /// Options for the secure RPC system.
    /// </summary>
    public class SecureRpcOptions
    {
        /// <summary>
        /// Gets or sets the event type for RPC requests.
        /// </summary>
        public string RequestEventType { get; set; } = "RpcRequest";

        /// <summary>
        /// Gets or sets the event type for RPC responses.
        /// </summary>
        public string ResponseEventType { get; set; } = "RpcResponse";

        /// <summary>
        /// Gets or sets the default timeout for RPC calls in milliseconds.
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the maximum number of concurrent RPC requests.
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to enable authentication for RPC calls.
        /// </summary>
        public bool EnableAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable encryption for RPC calls.
        /// </summary>
        public bool EnableEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable compression for RPC calls.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum size of an RPC request in bytes.
        /// </summary>
        public int MaxRequestSizeBytes { get; set; } = 1024 * 1024; // 1 MB

        /// <summary>
        /// Gets or sets the maximum size of an RPC response in bytes.
        /// </summary>
        public int MaxResponseSizeBytes { get; set; } = 1024 * 1024; // 1 MB

        /// <summary>
        /// Gets or sets the maximum number of retries for failed RPC calls.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
    }
}
