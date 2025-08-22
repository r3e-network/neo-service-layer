using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Generic repository implementation for PostgreSQL with Entity Framework Core
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly NeoServiceLayerDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(NeoServiceLayerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        /// <summary>
        /// Gets entity by primary key
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync<TKey>(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Gets all entities
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// Finds entities matching the predicate
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// Gets single entity matching the predicate
        /// </summary>
        public virtual async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.SingleOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Adds new entity
        /// </summary>
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        /// <summary>
        /// Updates existing entity
        /// </summary>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        /// <summary>
        /// Removes entity
        /// </summary>
        public virtual async Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Removes entity by ID
        /// </summary>
        public virtual async Task DeleteAsync<TKey>(TKey id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
        }

        /// <summary>
        /// Checks if entity exists
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// Gets count of entities matching predicate
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            if (predicate == null)
            {
                return await _dbSet.CountAsync();
            }

            return await _dbSet.CountAsync(predicate);
        }
    }
}