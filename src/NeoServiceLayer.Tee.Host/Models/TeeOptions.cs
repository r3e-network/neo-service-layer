namespace NeoServiceLayer.Tee.Host.Models
{
    /// <summary>
    /// Options for the TEE.
    /// </summary>
    public class TeeOptions
    {
        /// <summary>
        /// Gets or sets the path to the enclave.
        /// </summary>
        public string EnclavePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use simulation mode.
        /// </summary>
        public bool SimulationMode { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries for TEE operations.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry delay in milliseconds.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the timeout for TEE operations in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }
}
