using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Infrastructure.Data.Entities;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository for verification results.
    /// </summary>
    public class VerificationResultRepository : Repository<VerificationResultEntity, string>, IVerificationResultRepository
    {
        /// <summary>
        /// Initializes a new instance of the VerificationResultRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public VerificationResultRepository(NeoServiceLayerDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<Shared.Models.VerificationResult> GetVerificationResultByIdAsync(string verificationId)
        {
            var entity = await _dbSet.FindAsync(verificationId);
            return entity?.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Shared.Models.VerificationResult>> GetByStatusAsync(string status)
        {
            var entities = await _dbSet
                .Where(v => v.Status == status)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel());
        }

        /// <inheritdoc/>
        public async Task<Shared.Models.VerificationResult> AddVerificationResultAsync(Shared.Models.VerificationResult verificationResult, string verificationType, string identityData)
        {
            var entity = VerificationResultEntity.FromDomainModel(verificationResult, verificationType, identityData);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<Shared.Models.VerificationResult> UpdateVerificationResultAsync(Shared.Models.VerificationResult verificationResult)
        {
            var entity = await _dbSet.FindAsync(verificationResult.VerificationId);
            if (entity == null)
            {
                return null;
            }

            entity.Status = verificationResult.Status;
            entity.Score = verificationResult.Score;
            entity.Reason = verificationResult.Reason;
            entity.ProcessedAt = verificationResult.ProcessedAt;

            if (verificationResult.Metadata != null && verificationResult.Metadata.Count > 0)
            {
                entity.MetadataJson = System.Text.Json.JsonSerializer.Serialize(verificationResult.Metadata);
            }
            else
            {
                entity.MetadataJson = "{}";
            }

            entity.Verified = verificationResult.Verified;

            await _context.SaveChangesAsync();
            return entity.ToDomainModel();
        }
    }
}
