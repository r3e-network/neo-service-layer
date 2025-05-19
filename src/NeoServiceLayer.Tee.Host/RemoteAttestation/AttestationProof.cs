using System;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Represents a proof of attestation for a trusted execution environment.
    /// </summary>
    public class AttestationProof
    {
        /// <summary>
        /// Gets or sets the unique identifier for the attestation.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of attestation.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the attestation report.
        /// </summary>
        public byte[] Report { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the attestation was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the signature for the attestation.
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// Gets or sets the public key used for verification.
        /// </summary>
        public byte[] PublicKey { get; set; }

        /// <summary>
        /// Gets or sets additional data for the attestation.
        /// </summary>
        public string AdditionalData { get; set; }
    }
}
