using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Occlum
{
    /// <summary>
    /// Interface for managing Occlum instances within an enclave.
    /// </summary>
    public interface IOcclumManager : IDisposable
    {
        /// <summary>
        /// Synchronously initializes the Occlum instance.
        /// </summary>
        void Init();

        /// <summary>
        /// Asynchronously initializes the Occlum instance.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Initializes the Occlum instance with a specific directory and log level.
        /// </summary>
        /// <param name="instanceDir">The directory to initialize the Occlum instance in.</param>
        /// <param name="logLevel">The log level for the Occlum instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InitializeInstanceAsync(string instanceDir, string logLevel);

        /// <summary>
        /// Executes a command in the Occlum instance.
        /// </summary>
        /// <param name="path">The path to the executable.</param>
        /// <param name="args">The arguments for the executable.</param>
        /// <param name="env">The environment variables for the executable.</param>
        /// <returns>The exit code of the command.</returns>
        Task<int> ExecuteCommandAsync(string path, string[] args, string[] env = null);

        /// <summary>
        /// Executes a JavaScript file in the Occlum instance using Node.js.
        /// </summary>
        /// <param name="scriptPath">The path to the JavaScript file.</param>
        /// <param name="args">The arguments for the JavaScript file.</param>
        /// <param name="env">The environment variables for the JavaScript file.</param>
        /// <returns>The exit code of the command.</returns>
        Task<int> ExecuteJavaScriptFileAsync(string scriptPath, string[] args, string[] env = null);

        /// <summary>
        /// Executes JavaScript code in the Occlum instance using Node.js.
        /// </summary>
        /// <param name="code">The JavaScript code to execute.</param>
        /// <param name="args">The arguments for the JavaScript code.</param>
        /// <param name="env">The environment variables for the JavaScript code.</param>
        /// <returns>The exit code of the command.</returns>
        Task<int> ExecuteJavaScriptCodeAsync(string code, string[] args, string[] env = null);

        /// <summary>
        /// Checks if Occlum support is enabled in the enclave.
        /// </summary>
        /// <returns>True if Occlum support is enabled, false otherwise.</returns>
        bool IsSupported();
        
        /// <summary>
        /// Gets the version of Occlum.
        /// </summary>
        /// <returns>The version of Occlum.</returns>
        string GetVersion();
        
        /// <summary>
        /// Updates the configuration of the Occlum instance.
        /// </summary>
        /// <param name="options">The new options to apply to the Occlum instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateConfigurationAsync(OcclumOptions options);
    }
}
