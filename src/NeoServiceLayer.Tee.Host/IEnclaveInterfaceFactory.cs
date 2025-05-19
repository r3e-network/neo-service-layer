namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Factory for creating enclave interfaces
    /// </summary>
    public interface IEnclaveInterfaceFactory
    {
        /// <summary>
        /// Creates an enclave interface
        /// </summary>
        /// <returns>The enclave interface</returns>
        IEnclaveInterface CreateEnclaveInterface();
    }
}
