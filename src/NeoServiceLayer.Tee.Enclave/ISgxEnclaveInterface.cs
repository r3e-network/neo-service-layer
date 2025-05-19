using System;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Interface for SGX enclave operations (for backward compatibility)
    /// This interface extends ITeeEnclaveInterface to provide a unified abstraction
    /// while maintaining backward compatibility with existing SGX code.
    /// </summary>
    public interface ISgxEnclaveInterface : ITeeEnclaveInterface
    {
        /// <summary>
        /// Gets the SGX enclave ID.
        /// </summary>
        /// <returns>The SGX enclave ID.</returns>
        new IntPtr GetEnclaveId();

        /// <summary>
        /// Gets random bytes from the SGX enclave.
        /// </summary>
        /// <param name="length">The number of random bytes to get.</param>
        /// <returns>The random bytes.</returns>
        new byte[] GetRandomBytes(int length);

        /// <summary>
        /// Signs data using the SGX enclave's private key.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <returns>The signature.</returns>
        new byte[] SignData(byte[] data);

        /// <summary>
        /// Verifies a signature using the SGX enclave's public key.
        /// </summary>
        /// <param name="data">The data that was signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        new bool VerifySignature(byte[] data, byte[] signature);

        /// <summary>
        /// Seals data using the SGX enclave's sealing key.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        new byte[] SealData(byte[] data);

        /// <summary>
        /// Unseals data using the SGX enclave's sealing key.
        /// </summary>
        /// <param name="sealedData">The sealed data.</param>
        /// <returns>The unsealed data.</returns>
        new byte[] UnsealData(byte[] sealedData);
    }
}
