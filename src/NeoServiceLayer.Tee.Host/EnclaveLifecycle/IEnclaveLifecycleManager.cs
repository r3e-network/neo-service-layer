using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.EnclaveLifecycle
{
    /// <summary>
    /// Interface for managing the lifecycle of enclaves.
    /// </summary>
    public interface IEnclaveLifecycleManager : IDisposable
    {
        /// <summary>
        /// Creates and initializes an enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to create.</param>
        /// <param name="enclavePath">The path to the enclave file.</param>
        /// <param name="simulationMode">Whether to create the enclave in simulation mode.</param>
        /// <returns>The created enclave interface.</returns>
        Task<ITeeInterface> CreateEnclaveAsync(string enclaveId, string enclavePath, bool simulationMode = false);

        /// <summary>
        /// Gets an existing enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to get.</param>
        /// <returns>The enclave interface, or null if the enclave does not exist.</returns>
        Task<ITeeInterface> GetEnclaveAsync(string enclaveId);

        /// <summary>
        /// Terminates and removes an enclave with the specified ID.
        /// </summary>
        /// <param name="enclaveId">The ID of the enclave to terminate.</param>
        /// <returns>True if the enclave was terminated, false if the enclave does not exist.</returns>
        Task<bool> TerminateEnclaveAsync(string enclaveId);

        /// <summary>
        /// Gets all active enclaves.
        /// </summary>
        /// <returns>A dictionary of enclave IDs to enclave interfaces.</returns>
        Task<IReadOnlyDictionary<string, ITeeInterface>> GetAllEnclavesAsync();

        /// <summary>
        /// Terminates all active enclaves.
        /// </summary>
        /// <returns>The number of enclaves terminated.</returns>
        Task<int> TerminateAllEnclavesAsync();
    }
}
