using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Generic repository interface for common CRUD operations
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets entity by primary key
        /// </summary>
        Task<TEntity?> GetByIdAsync<TKey>(TKey id);

        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Finds entities matching the predicate
        /// </summary>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Gets single entity matching the predicate
        /// </summary>
        Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds new entity
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Updates existing entity
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Removes entity
        /// </summary>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// Removes entity by ID
        /// </summary>
        Task DeleteAsync<TKey>(TKey id);

        /// <summary>
        /// Checks if entity exists
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Gets count of entities matching predicate
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    }
}