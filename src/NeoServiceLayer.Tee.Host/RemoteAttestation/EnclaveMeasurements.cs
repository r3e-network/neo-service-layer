using System;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Contains the measurements of an enclave.
    /// </summary>
    public class EnclaveMeasurements
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

        /// <summary>
        /// Gets or sets the attributes of the enclave.
        /// </summary>
        public int Attributes { get; set; }

        /// <summary>
        /// Gets the MRENCLAVE value as a hexadecimal string.
        /// </summary>
        public string MrEnclaveHex => MrEnclave != null ? BitConverter.ToString(MrEnclave).Replace("-", "") : null;

        /// <summary>
        /// Gets the MRSIGNER value as a hexadecimal string.
        /// </summary>
        public string MrSignerHex => MrSigner != null ? BitConverter.ToString(MrSigner).Replace("-", "") : null;

        /// <summary>
        /// Returns a string representation of the enclave measurements.
        /// </summary>
        public override string ToString()
        {
            return $"MRENCLAVE: {MrEnclaveHex}, MRSIGNER: {MrSignerHex}, ProductId: {ProductId}, SecurityVersion: {SecurityVersion}, Attributes: {Attributes:X}";
        }
    }
}
