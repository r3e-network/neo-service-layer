using System.Threading.Tasks;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Entities;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Interface for the attestation proof repository.
    /// </summary>
    public interface IAttestationProofRepository : IRepository<AttestationProofEntity, string>
    {
        /// <summary>
        /// Gets the current attestation proof.
        /// </summary>
        /// <returns>The current attestation proof, or null if not found.</returns>
        Task<AttestationProof> GetCurrentAsync();

        /// <summary>
        /// Adds an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to add.</param>
        /// <returns>The added attestation proof.</returns>
        Task<AttestationProof> AddAsync(AttestationProof attestationProof);

        /// <summary>
        /// Gets an attestation proof by its ID.
        /// </summary>
        /// <param name="attestationProofId">The attestation proof ID.</param>
        /// <returns>The attestation proof, or null if not found.</returns>
        new Task<AttestationProof> GetByIdAsync(string attestationProofId);

        /// <summary>
        /// Adds an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAttestationProofAsync(AttestationProof attestationProof);

        /// <summary>
        /// Gets an attestation proof by its ID.
        /// </summary>
        /// <param name="attestationProofId">The attestation proof ID.</param>
        /// <returns>The attestation proof, or null if not found.</returns>
        Task<AttestationProof> GetAttestationProofByIdAsync(string attestationProofId);

        /// <summary>
        /// Gets the latest attestation proof for a specific MrEnclave value.
        /// </summary>
        /// <param name="mrEnclave">The MrEnclave value.</param>
        /// <returns>The latest attestation proof, or null if not found.</returns>
        Task<AttestationProof> GetLatestAttestationProofAsync(string mrEnclave);
    }
}
