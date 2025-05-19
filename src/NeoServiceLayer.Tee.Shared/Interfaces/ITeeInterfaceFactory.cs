using System;

namespace NeoServiceLayer.Tee.Shared.Interfaces
{
    /// <summary>
    /// Factory for creating TEE interfaces.
    /// </summary>
    public interface ITeeInterfaceFactory
    {
        /// <summary>
        /// Creates a TEE interface.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The TEE interface.</returns>
        ITeeInterface CreateTeeInterface(string enclavePath, bool simulationMode);

        /// <summary>
        /// Creates an Occlum interface.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The Occlum interface.</returns>
        IOcclumInterface CreateOcclumInterface(string enclavePath, bool simulationMode);

        /// <summary>
        /// Creates an SGX interface.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The SGX interface.</returns>
        ISgxEnclaveInterface CreateSgxInterface(string enclavePath, bool simulationMode);
    }
}
