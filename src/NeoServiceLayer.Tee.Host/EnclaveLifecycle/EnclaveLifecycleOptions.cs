namespace NeoServiceLayer.Tee.Host.EnclaveLifecycle
{
    /// <summary>
    /// Options for the enclave lifecycle manager.
    /// </summary>
    public class EnclaveLifecycleOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent enclave creations.
        /// </summary>
        public int MaxConcurrentEnclaveCreations { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to automatically terminate enclaves when they are idle.
        /// </summary>
        public bool AutoTerminateIdleEnclaves { get; set; } = false;

        /// <summary>
        /// Gets or sets the idle timeout in minutes before an enclave is automatically terminated.
        /// </summary>
        public int IdleTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to use simulation mode by default.
        /// </summary>
        public bool DefaultSimulationMode { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable debug mode for enclaves.
        /// </summary>
        public bool EnableDebugMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the default enclave path.
        /// </summary>
        public string DefaultEnclavePath { get; set; } = null;
    }
}
