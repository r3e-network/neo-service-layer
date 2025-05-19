using System;

namespace NeoServiceLayer.Core.Models
{
    /// <summary>
    /// Represents the result of an attestation verification.
    /// </summary>
    public class AttestationVerificationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the attestation is valid.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Gets or sets the reason for the verification result.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the enclave identity.
        /// </summary>
        public EnclaveIdentity EnclaveIdentity { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the verification.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Creates a new instance of the AttestationVerificationResult class.
        /// </summary>
        public AttestationVerificationResult()
        {
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents the identity of an enclave.
    /// </summary>
    public class EnclaveIdentity
    {
        /// <summary>
        /// Gets or sets the MRENCLAVE value.
        /// </summary>
        public string MrEnclave { get; set; }

        /// <summary>
        /// Gets or sets the MRSIGNER value.
        /// </summary>
        public string MrSigner { get; set; }

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the security version number.
        /// </summary>
        public string SecurityVersion { get; set; }
    }
}
