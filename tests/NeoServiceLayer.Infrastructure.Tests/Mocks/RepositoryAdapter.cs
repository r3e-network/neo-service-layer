using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Interfaces;

namespace NeoServiceLayer.Infrastructure.Tests.Mocks
{
    /// <summary>
    /// Adapter class that implements Core.Interfaces.IRepository and delegates to Infrastructure.Data.Repositories.IRepository.
    /// </summary>
    public class RepositoryAdapter<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
    {
        private readonly NeoServiceLayer.Infrastructure.Data.Repositories.IRepository<TEntity, TKey> _repository;

        /// <summary>
        /// Initializes a new instance of the RepositoryAdapter class.
        /// </summary>
        /// <param name="repository">The Infrastructure.Data.Repositories.IRepository implementation.</param>
        public RepositoryAdapter(NeoServiceLayer.Infrastructure.Data.Repositories.IRepository<TEntity, TKey> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public Task<TEntity> AddAsync(TEntity entity)
        {
            return _repository.AddAsync(entity);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _repository.FindAsync(predicate);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        /// <inheritdoc/>
        public Task<TEntity?> GetByIdAsync(TKey id)
        {
            return _repository.GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public Task<TEntity> UpdateAsync(TEntity entity)
        {
            return _repository.UpdateAsync(entity);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(TKey id)
        {
            try
            {
                await _repository.RemoveByIdAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(TKey id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity != null;
        }
    }
}
