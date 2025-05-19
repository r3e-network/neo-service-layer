using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Interface for a generic repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its key.
        /// </summary>
        /// <param name="id">The entity key.</param>
        /// <returns>The entity, or null if not found.</returns>
        Task<TEntity> GetByIdAsync(TKey id);

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <returns>All entities.</returns>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Finds entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match.</param>
        /// <returns>The matching entities.</returns>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity.</returns>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The updated entity.</returns>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(TEntity entity);

        /// <summary>
        /// Removes an entity by its key.
        /// </summary>
        /// <param name="id">The entity key.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByIdAsync(TKey id);

        /// <summary>
        /// Gets a paginated list of entities.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>The paginated list of entities and the total count.</returns>
        Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);

        /// <summary>
        /// Gets a paginated list of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>The paginated list of entities and the total count.</returns>
        Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(Expression<Func<TEntity, bool>> predicate, int page, int pageSize);
    }
}
