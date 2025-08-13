using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.KeyManagement.Queries;
using NeoServiceLayer.Services.KeyManagement.ReadModels;

namespace NeoServiceLayer.Services.KeyManagement.QueryHandlers
{
    public class GetKeyByIdQueryHandler : IQueryHandler<GetKeyByIdQuery, KeyReadModel?>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetKeyByIdQueryHandler> _logger;

        public GetKeyByIdQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetKeyByIdQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<KeyReadModel?> HandleAsync(
            GetKeyByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetByIdAsync(query.KeyId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get key {KeyId}", query.KeyId);
                throw;
            }
        }
    }

    public class GetKeysByTypeQueryHandler : IQueryHandler<GetKeysByTypeQuery, IEnumerable<KeyReadModel>>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetKeysByTypeQueryHandler> _logger;

        public GetKeysByTypeQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetKeysByTypeQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<KeyReadModel>> HandleAsync(
            GetKeysByTypeQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetByTypeAsync(query.KeyType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get keys of type {KeyType}", query.KeyType);
                throw;
            }
        }
    }

    public class GetActiveKeysQueryHandler : IQueryHandler<GetActiveKeysQuery, IEnumerable<KeyReadModel>>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetActiveKeysQueryHandler> _logger;

        public GetActiveKeysQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetActiveKeysQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<KeyReadModel>> HandleAsync(
            GetActiveKeysQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetActiveKeysAsync(query.Limit, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active keys");
                throw;
            }
        }
    }

    public class GetKeysExpiringBeforeQueryHandler : IQueryHandler<GetKeysExpiringBeforeQuery, IEnumerable<KeyReadModel>>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetKeysExpiringBeforeQueryHandler> _logger;

        public GetKeysExpiringBeforeQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetKeysExpiringBeforeQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<KeyReadModel>> HandleAsync(
            GetKeysExpiringBeforeQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetExpiringKeysAsync(query.ExpiryDate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get keys expiring before {ExpiryDate}", query.ExpiryDate);
                throw;
            }
        }
    }

    public class GetKeysByUserQueryHandler : IQueryHandler<GetKeysByUserQuery, IEnumerable<KeyReadModel>>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetKeysByUserQueryHandler> _logger;

        public GetKeysByUserQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetKeysByUserQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<KeyReadModel>> HandleAsync(
            GetKeysByUserQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetByUserAsync(query.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get keys for user {UserId}", query.UserId);
                throw;
            }
        }
    }

    public class GetKeyUsageStatisticsQueryHandler : IQueryHandler<GetKeyUsageStatisticsQuery, KeyUsageStatistics>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<GetKeyUsageStatisticsQueryHandler> _logger;

        public GetKeyUsageStatisticsQueryHandler(
            IKeyReadModelStore store,
            ILogger<GetKeyUsageStatisticsQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<KeyUsageStatistics> HandleAsync(
            GetKeyUsageStatisticsQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _store.GetUsageStatisticsAsync(
                    query.KeyId,
                    query.From,
                    query.To,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get usage statistics for key {KeyId} from {From} to {To}",
                    query.KeyId, query.From, query.To);
                throw;
            }
        }
    }

    public class SearchKeysQueryHandler : IQueryHandler<SearchKeysQuery, IEnumerable<KeyReadModel>>
    {
        private readonly IKeyReadModelStore _store;
        private readonly ILogger<SearchKeysQueryHandler> _logger;

        public SearchKeysQueryHandler(
            IKeyReadModelStore store,
            ILogger<SearchKeysQueryHandler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<KeyReadModel>> HandleAsync(
            SearchKeysQuery query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await _store.SearchAsync(
                    query.SearchTerm,
                    query.KeyType,
                    query.Algorithm,
                    query.Status,
                    query.Offset,
                    query.Limit,
                    cancellationToken);

                _logger.LogInformation(
                    "Search returned {Count} keys for query {SearchTerm}",
                    results.Count(), query.SearchTerm);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search keys with term {SearchTerm}", query.SearchTerm);
                throw;
            }
        }
    }

    /// <summary>
    /// Interface for key read model storage
    /// </summary>
    public interface IKeyReadModelStore
    {
        Task<KeyReadModel?> GetByIdAsync(string keyId, CancellationToken cancellationToken = default);
        Task<IEnumerable<KeyReadModel>> GetByTypeAsync(string keyType, CancellationToken cancellationToken = default);
        Task<IEnumerable<KeyReadModel>> GetActiveKeysAsync(int? limit, CancellationToken cancellationToken = default);
        Task<IEnumerable<KeyReadModel>> GetExpiringKeysAsync(DateTime before, CancellationToken cancellationToken = default);
        Task<IEnumerable<KeyReadModel>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<KeyUsageStatistics> GetUsageStatisticsAsync(string keyId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<IEnumerable<KeyReadModel>> SearchAsync(
            string? searchTerm,
            string? keyType,
            string? algorithm,
            KeyStatusFilter? status,
            int offset,
            int limit,
            CancellationToken cancellationToken = default);
        Task SaveAsync(KeyReadModel model, CancellationToken cancellationToken = default);
        Task UpdateAsync(KeyReadModel model, CancellationToken cancellationToken = default);
        Task DeleteAsync(string keyId, CancellationToken cancellationToken = default);
    }
}