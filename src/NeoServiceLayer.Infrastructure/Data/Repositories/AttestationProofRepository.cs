using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Entities;
using Task = System.Threading.Tasks.Task;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository for attestation proofs.
    /// </summary>
    public class AttestationProofRepository : Repository<AttestationProofEntity, string>, IAttestationProofRepository
    {
        /// <summary>
        /// Initializes a new instance of the AttestationProofRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public AttestationProofRepository(NeoServiceLayerDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<AttestationProof> GetCurrentAsync()
        {
            var entity = await _dbSet
                .Where(a => a.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return entity?.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<AttestationProof> AddAsync(AttestationProof attestationProof)
        {
            var entity = AttestationProofEntity.FromDomainModel(attestationProof);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.ToDomainModel();
        }

        /// <summary>
        /// Adds an attestation proof.
        /// </summary>
        /// <param name="attestationProof">The attestation proof to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddAttestationProofAsync(AttestationProof attestationProof)
        {
            await AddAsync(attestationProof);
        }

        /// <inheritdoc/>
        public new async Task<AttestationProof> GetByIdAsync(string attestationProofId)
        {
            var entity = await _dbSet.FindAsync(attestationProofId);
            return entity?.ToDomainModel();
        }

        /// <summary>
        /// Gets an attestation proof by its ID.
        /// </summary>
        /// <param name="attestationProofId">The attestation proof ID.</param>
        /// <returns>The attestation proof, or null if not found.</returns>
        public async Task<AttestationProof> GetAttestationProofByIdAsync(string attestationProofId)
        {
            return await GetByIdAsync(attestationProofId);
        }

        /// <summary>
        /// Gets the latest attestation proof for a specific MrEnclave value.
        /// </summary>
        /// <param name="mrEnclave">The MrEnclave value.</param>
        /// <returns>The latest attestation proof, or null if not found.</returns>
        public async Task<AttestationProof> GetLatestAttestationProofAsync(string mrEnclave)
        {
            var entity = await _dbSet
                .Where(a => a.MrEnclave == mrEnclave && a.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return entity?.ToDomainModel();
        }
    }
}
