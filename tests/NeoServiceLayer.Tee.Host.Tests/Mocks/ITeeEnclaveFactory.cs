using System;
using NeoServiceLayer.Tee.Enclave;

namespace NeoServiceLayer.Tee.Host.Tests.Mocks
{
    /// <summary>
    /// Interface for creating TEE enclave interfaces.
    /// </summary>
    public interface ITeeEnclaveFactory
    {
        /// <summary>
        /// Creates an enclave.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The enclave interface.</returns>
        ITeeEnclaveInterface CreateEnclave(string enclavePath, bool simulationMode);
    }
}
