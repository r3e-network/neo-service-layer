using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Interfaces
{
    /// <summary>
    /// Generic repository interface.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its ID.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <returns>The entity, or null if not found.</returns>
        Task<TEntity?> GetByIdAsync(TKey id);

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <returns>All entities.</returns>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Finds entities matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity.</returns>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The updated entity.</returns>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Deletes an entity by its ID.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <returns>True if the entity was deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(TKey id);

        /// <summary>
        /// Checks if an entity with the specified ID exists.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <returns>True if the entity exists, false otherwise.</returns>
        Task<bool> ExistsAsync(TKey id);
    }
}
