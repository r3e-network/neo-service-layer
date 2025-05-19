using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Shared.Interfaces
{
    /// <summary>
    /// Interface for Occlum LibOS specific operations.
    /// This interface extends ITeeInterface to provide Occlum specific functionality.
    /// </summary>
    public interface IOcclumInterface : ITeeInterface
    {
        /// <summary>
        /// Gets the product ID of the enclave.
        /// </summary>
        int ProductId { get; }

        /// <summary>
        /// Gets the security version of the enclave.
        /// </summary>
        int SecurityVersion { get; }

        /// <summary>
        /// Gets the attributes of the enclave.
        /// </summary>
        int Attributes { get; }

        /// <summary>
        /// Initializes Occlum in the enclave.
        /// </summary>
        /// <param name="instanceDir">The Occlum instance directory.</param>
        /// <param name="logLevel">The log level for Occlum.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeOcclumAsync(string instanceDir, string logLevel);

        /// <summary>
        /// Executes a command in Occlum.
        /// </summary>
        /// <param name="path">The path to the executable.</param>
        /// <param name="args">The command arguments.</param>
        /// <param name="env">The environment variables.</param>
        /// <returns>The exit code of the command.</returns>
        Task<int> ExecuteOcclumCommandAsync(string path, string[] args, string[] env);

        /// <summary>
        /// Gets the Occlum version.
        /// </summary>
        /// <returns>The Occlum version.</returns>
        string GetOcclumVersion();

        /// <summary>
        /// Checks if Occlum support is enabled.
        /// </summary>
        /// <returns>True if Occlum support is enabled, false otherwise.</returns>
        bool IsOcclumSupportEnabled();

        /// <summary>
        /// Gets the enclave configuration.
        /// </summary>
        /// <returns>The enclave configuration as a JSON string.</returns>
        string GetEnclaveConfiguration();

        /// <summary>
        /// Updates the enclave configuration.
        /// </summary>
        /// <param name="configuration">The new configuration as a JSON string.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateEnclaveConfigurationAsync(string configuration);
    }
}
