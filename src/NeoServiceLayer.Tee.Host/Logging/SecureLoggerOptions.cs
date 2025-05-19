using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Tee.Host.Logging
{
    /// <summary>
    /// Options for the secure logger.
    /// </summary>
    public class SecureLoggerOptions
    {
        /// <summary>
        /// Gets or sets the directory where the log files are stored.
        /// </summary>
        public string LogDirectory { get; set; } = "logs";

        /// <summary>
        /// Gets or sets the minimum log level to include.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the maximum size of the log queue.
        /// </summary>
        public int MaxQueueSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the timeout for enqueueing log entries in milliseconds.
        /// </summary>
        public int EnqueueTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable file logging.
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable event logging.
        /// </summary>
        public bool EnableEventLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable standard logging.
        /// </summary>
        public bool EnableStandardLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable sealing of log entries.
        /// </summary>
        public bool EnableSealing { get; set; } = true;

        /// <summary>
        /// Gets or sets the source of the log entries.
        /// </summary>
        public string Source { get; set; } = "Enclave";

        /// <summary>
        /// Gets or sets the maximum size of a log file in bytes.
        /// </summary>
        public long MaxLogFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Gets or sets the maximum number of log files to keep.
        /// </summary>
        public int MaxLogFiles { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to include sensitive information in log entries.
        /// </summary>
        public bool IncludeSensitiveInformation { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include stack traces in log entries.
        /// </summary>
        public bool IncludeStackTraces { get; set; } = true;
    }
}
