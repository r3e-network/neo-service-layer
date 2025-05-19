namespace NeoServiceLayer.Tee.Host.Occlum
{
    /// <summary>
    /// Options for the Occlum manager.
    /// </summary>
    public class OcclumOptions
    {
        /// <summary>
        /// Gets or sets the directory where the Occlum instance is located.
        /// </summary>
        public string InstanceDir { get; set; } = "/tmp/occlum_instance";

        /// <summary>
        /// Gets or sets the log level for Occlum.
        /// </summary>
        public string LogLevel { get; set; } = "info";

        /// <summary>
        /// Gets or sets the path to the Node.js executable.
        /// </summary>
        public string NodeJsPath { get; set; } = "/usr/bin/node";

        /// <summary>
        /// Gets or sets the directory for temporary files.
        /// </summary>
        public string TempDir { get; set; } = "/tmp";

        /// <summary>
        /// Gets or sets whether to enable debug mode for Occlum.
        /// </summary>
        public bool EnableDebugMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum memory size for the Occlum instance in MB.
        /// </summary>
        public int MaxMemoryMB { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the maximum number of threads for the Occlum instance.
        /// </summary>
        public int MaxThreads { get; set; } = 32;

        /// <summary>
        /// Gets or sets the maximum number of processes for the Occlum instance.
        /// </summary>
        public int MaxProcesses { get; set; } = 16;

        /// <summary>
        /// Gets or sets the maximum execution time for a command in seconds.
        /// </summary>
        public int MaxExecutionTimeSeconds { get; set; } = 60;
    }
}
