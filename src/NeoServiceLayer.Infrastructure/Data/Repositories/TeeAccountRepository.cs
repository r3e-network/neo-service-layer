using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data.Entities;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository for TEE accounts.
    /// </summary>
    public class TeeAccountRepository : Repository<TeeAccountEntity, string>, ITeeAccountRepository
    {
        /// <summary>
        /// Initializes a new instance of the TeeAccountRepository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public TeeAccountRepository(NeoServiceLayerDbContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TeeAccount>> GetByUserIdAsync(string userId)
        {
            var entities = await _dbSet
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel());
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> GetAccountByIdAsync(string accountId)
        {
            var entity = await _dbSet.FindAsync(accountId);
            return entity?.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> AddAccountAsync(TeeAccount account)
        {
            var entity = TeeAccountEntity.FromDomainModel(account);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.ToDomainModel();
        }

        /// <inheritdoc/>
        public async Task<TeeAccount> UpdateAccountAsync(TeeAccount account)
        {
            var existingEntity = await _dbSet.FindAsync(account.Id);
            if (existingEntity == null)
            {
                return null;
            }

            // Update properties
            existingEntity.Name = account.Name;
            existingEntity.Type = account.Type;
            existingEntity.PublicKey = account.PublicKey;
            existingEntity.Address = account.Address;
            existingEntity.UserId = account.UserId;
            existingEntity.UpdatedAt = account.UpdatedAt;
            existingEntity.IsExportable = account.IsExportable;
            existingEntity.AttestationProof = account.AttestationProof ?? "";

            if (account.Metadata != null && account.Metadata.Count > 0)
            {
                existingEntity.MetadataJson = System.Text.Json.JsonSerializer.Serialize(account.Metadata);
            }
            else
            {
                existingEntity.MetadataJson = "{}";
            }

            await _context.SaveChangesAsync();
            return existingEntity.ToDomainModel();
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task DeleteAccountAsync(string accountId)
        {
            var entity = await _dbSet.FindAsync(accountId);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
