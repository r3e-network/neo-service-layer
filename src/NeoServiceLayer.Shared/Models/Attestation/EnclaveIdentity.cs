namespace NeoServiceLayer.Shared.Models.Attestation
{
    /// <summary>
    /// Represents the identity of an enclave.
    /// </summary>
    public class EnclaveIdentity
    {
        /// <summary>
        /// Gets or sets the MRENCLAVE value, which is a hash of the enclave's code and data.
        /// </summary>
        public byte[] MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value, which is a hash of the public key used to sign the enclave.
        /// </summary>
        public byte[] MrSigner { get; set; }

        /// <summary>
        /// Gets or sets the product ID of the enclave.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the security version of the enclave.
        /// </summary>
        public int SecurityVersion { get; set; }
    }
}
