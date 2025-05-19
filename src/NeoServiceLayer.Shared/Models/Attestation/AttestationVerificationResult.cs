namespace NeoServiceLayer.Shared.Models.Attestation
{
    /// <summary>
    /// Represents the result of verifying attestation evidence.
    /// </summary>
    public class AttestationVerificationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the evidence is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the enclave-held data that was included in the evidence.
        /// </summary>
        public byte[] EnclaveHeldData { get; set; }

        /// <summary>
        /// Gets or sets the enclave identity.
        /// </summary>
        public EnclaveIdentity EnclaveIdentity { get; set; }
    }
}
