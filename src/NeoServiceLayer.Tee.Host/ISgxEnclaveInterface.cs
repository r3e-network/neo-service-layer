using System;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Interface for interacting with an SGX enclave.
    /// </summary>
    public interface ISgxEnclaveInterface : ITeeInterface
    {
        /// <summary>
        /// Gets the enclave ID.
        /// </summary>
        IntPtr EnclaveId { get; }
    }
}
