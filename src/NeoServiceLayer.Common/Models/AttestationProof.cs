using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoServiceLayer.Common.Models
{
    /// <summary>
    /// Represents an attestation proof for a Trusted Execution Environment (TEE).
    /// </summary>
    public class AttestationProof
    {
        /// <summary>
        /// Gets or sets the unique identifier for this attestation proof.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the timestamp when this attestation proof was created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the type of TEE (e.g., "SGX", "OpenEnclave").
        /// </summary>
        [JsonPropertyName("teeType")]
        public string TeeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the TEE.
        /// </summary>
        [JsonPropertyName("teeVersion")]
        public string TeeVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the enclave ID.
        /// </summary>
        [JsonPropertyName("enclaveId")]
        public string EnclaveId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MRENCLAVE value (measurement of the enclave code).
        /// </summary>
        [JsonPropertyName("mrenclave")]
        public string MrEnclave { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MRSIGNER value (measurement of the enclave signer).
        /// </summary>
        [JsonPropertyName("mrsigner")]
        public string MrSigner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product ID.
        /// </summary>
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the security version.
        /// </summary>
        [JsonPropertyName("securityVersion")]
        public string SecurityVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attributes of the enclave.
        /// </summary>
        [JsonPropertyName("attributes")]
        public string Attributes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the report data (user data included in the attestation).
        /// </summary>
        [JsonPropertyName("reportData")]
        public string ReportData { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attestation report in base64 format.
        /// </summary>
        [JsonPropertyName("report")]
        public string Report { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the signature of the attestation report.
        /// </summary>
        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional properties for the attestation proof.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
