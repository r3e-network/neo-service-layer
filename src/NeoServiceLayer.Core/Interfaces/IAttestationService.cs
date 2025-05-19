using System.Threading.Tasks;
using NeoServiceLayer.Core.Models;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Interface for the attestation service.
    /// </summary>
    public interface IAttestationService
    {
        /// <summary>
        /// Generates an attestation proof.
        /// </summary>
        /// <returns>The generated attestation proof.</returns>
        Task<AttestationProof> GenerateAttestationProofAsync();

        /// <summary>
        /// Verifies an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to verify.</param>
        /// <returns>True if the attestation proof is valid, false otherwise.</returns>
        Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof);

        /// <summary>
        /// Gets the current attestation proof.
        /// </summary>
        /// <returns>The current attestation proof.</returns>
        Task<AttestationProof> GetCurrentAttestationProofAsync();

        /// <summary>
        /// Gets an attestation proof by ID.
        /// </summary>
        /// <param name="attestationProofId">The ID of the attestation proof to get.</param>
        /// <returns>The attestation proof with the specified ID.</returns>
        Task<AttestationProof> GetAttestationProofAsync(string attestationProofId);
    }
}
