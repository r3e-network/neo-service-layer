namespace NeoServiceLayer.Tee.Host
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
        ITeeInterface CreateOcclumInterface(string enclavePath, bool simulationMode);

        /// <summary>
        /// Creates an SGX enclave interface.
        /// </summary>
        /// <param name="enclavePath">The path to the enclave binary.</param>
        /// <param name="simulationMode">Whether to run in simulation mode.</param>
        /// <returns>The SGX enclave interface.</returns>
        ISgxEnclaveInterface CreateSgxInterface(string enclavePath, bool simulationMode);
    }
}
