using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Shared.Models.Attestation
{
    /// <summary>
    /// Represents an attestation proof for a trusted execution environment.
    /// </summary>
    public class AttestationProof
    {
        /// <summary>
        /// Gets or sets the type of enclave that generated the attestation proof.
        /// </summary>
        public string EnclaveType { get; set; }

        /// <summary>
        /// Gets or sets the attestation report in base64-encoded format.
        /// </summary>
        public string Report { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the attestation proof was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional data associated with the attestation proof.
        /// </summary>
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}
