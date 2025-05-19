using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Generic repository implementation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly NeoServiceLayerDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        /// <summary>
        /// Initializes a new instance of the Repository class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public Repository(NeoServiceLayerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        /// <inheritdoc/>
        public virtual async Task<TEntity> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <inheritdoc/>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <inheritdoc/>
        public virtual async Task RemoveAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public virtual async Task RemoveByIdAsync(TKey id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await RemoveAsync(entity);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var totalCount = await _dbSet.CountAsync();
            var items = await _dbSet
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(Expression<Func<TEntity, bool>> predicate, int page, int pageSize)
        {
            var query = _dbSet.Where(predicate);
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
