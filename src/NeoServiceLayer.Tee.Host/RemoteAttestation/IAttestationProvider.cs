using System;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Attestation;

namespace NeoServiceLayer.Tee.Host.RemoteAttestation
{
    /// <summary>
    /// Interface for attestation providers that can generate and verify attestation proofs.
    /// </summary>
    public interface IAttestationProvider : IDisposable
    {
        /// <summary>
        /// Generates an attestation proof for the enclave.
        /// </summary>
        /// <param name="reportData">Optional data to include in the report.</param>
        /// <returns>An attestation proof containing the report and related data.</returns>
        Task<AttestationProof> GenerateAttestationProofAsync(byte[] reportData = null);

        /// <summary>
        /// Verifies an attestation report.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>True if the attestation proof is valid, false otherwise.</returns>
        Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof);
        
        /// <summary>
        /// Gets an attestation report for the enclave.
        /// </summary>
        /// <param name="reportData">Optional data to include in the report.</param>
        /// <returns>The attestation report.</returns>
        byte[] GetAttestationReport(byte[] reportData);
        
        /// <summary>
        /// Verifies an attestation report.
        /// </summary>
        /// <param name="report">The attestation report to verify.</param>
        /// <returns>True if the report is valid, false otherwise.</returns>
        bool VerifyAttestationReport(byte[] report);
        
        /// <summary>
        /// Seals data using the enclave's sealing key.
        /// </summary>
        /// <param name="data">The data to seal.</param>
        /// <returns>The sealed data.</returns>
        byte[] SealData(byte[] data);
        
        /// <summary>
        /// Unseals data using the enclave's sealing key.
        /// </summary>
        /// <param name="sealedData">The sealed data to unseal.</param>
        /// <returns>The unsealed data.</returns>
        byte[] UnsealData(byte[] sealedData);
        
        /// <summary>
        /// Gets the measurements of the enclave.
        /// </summary>
        /// <returns>The enclave measurements.</returns>
        EnclaveMeasurements GetMeasurements();
        
        /// <summary>
        /// Extracts measurements from an attestation report.
        /// </summary>
        /// <param name="report">The attestation report.</param>
        /// <returns>The enclave measurements.</returns>
        EnclaveMeasurements GetMeasurementsFromReport(byte[] report);
    }
}
