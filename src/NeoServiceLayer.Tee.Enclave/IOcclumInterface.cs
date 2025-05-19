using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Interface for interacting with the Occlum LibOS.
    /// </summary>
    public interface IOcclumInterface
    {
        /// <summary>
        /// Initializes the Occlum environment.
        /// </summary>
        /// <returns>True if initialization is successful, false otherwise.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Executes a command in the Occlum instance.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="args">The command arguments.</param>
        /// <returns>The command output.</returns>
        Task<string> ExecuteCommandAsync(string command, string[] args);

        /// <summary>
        /// Gets the instance ID of the Occlum instance.
        /// </summary>
        /// <returns>The instance ID.</returns>
        string GetInstanceId();

        /// <summary>
        /// Records execution metrics for a JavaScript function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="gasUsed">The amount of GAS used.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed);

        /// <summary>
        /// Records execution failure for a JavaScript function.
        /// </summary>
        /// <param name="functionId">The function ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage);

        /// <summary>
        /// Verifies the integrity of the Occlum instance.
        /// </summary>
        /// <returns>True if the instance is valid, false otherwise.</returns>
        Task<bool> VerifyIntegrityAsync();

        /// <summary>
        /// Generates a random value using the Occlum random source.
        /// </summary>
        /// <param name="size">The size of the random value in bytes.</param>
        /// <returns>The random bytes.</returns>
        Task<byte[]> GenerateRandomBytesAsync(int size);

        /// <summary>
        /// Terminates the Occlum instance.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TerminateAsync();
    }
}
