namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the status of a Trusted Execution Environment.
    /// </summary>
    public enum TeeStatus
    {
        /// <summary>
        /// The TEE is not initialized.
        /// </summary>
        NotInitialized = 0,

        /// <summary>
        /// The TEE is initializing.
        /// </summary>
        Initializing = 1,

        /// <summary>
        /// The TEE is running.
        /// </summary>
        Running = 2,

        /// <summary>
        /// The TEE is stopping.
        /// </summary>
        Stopping = 3,

        /// <summary>
        /// The TEE is stopped.
        /// </summary>
        Stopped = 4,

        /// <summary>
        /// The TEE is in an error state.
        /// </summary>
        Error = 5
    }
}
