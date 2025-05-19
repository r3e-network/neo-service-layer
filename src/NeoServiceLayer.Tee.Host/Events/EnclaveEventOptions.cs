namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Options for the enclave event system.
    /// </summary>
    public class EnclaveEventOptions
    {
        /// <summary>
        /// Gets or sets the maximum size of the event queue.
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the timeout for enqueueing events in milliseconds.
        /// </summary>
        public int EnqueueTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum number of concurrent event handlers.
        /// </summary>
        public int MaxConcurrentHandlers { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of retries for failed event handlers.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable event persistence.
        /// </summary>
        public bool EnablePersistence { get; set; } = false;

        /// <summary>
        /// Gets or sets the directory for event persistence.
        /// </summary>
        public string PersistenceDirectory { get; set; } = "events";

        /// <summary>
        /// Gets or sets whether to enable event batching.
        /// </summary>
        public bool EnableBatching { get; set; } = false;

        /// <summary>
        /// Gets or sets the batch size for events.
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the batch interval in milliseconds.
        /// </summary>
        public int BatchIntervalMs { get; set; } = 1000;
    }
}
