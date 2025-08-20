using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Services.KeyManagement.Queries;
using NeoServiceLayer.Services.KeyManagement.QueryHandlers;
using NeoServiceLayer.Services.KeyManagement.ReadModels;
using NeoServiceLayer.ServiceFramework;


namespace NeoServiceLayer.Services.KeyManagement.Infrastructure
{
    /// <summary>
    /// In-memory implementation of key store (for development/testing)
    /// </summary>
    public class InMemoryKeyStore : IKeyStore
    {
        private readonly ConcurrentDictionary<string, KeyData> _keys = new();

        public Task<KeyData?> GetKeyDataAsync(string keyId, CancellationToken cancellationToken = default)
        {
            _keys.TryGetValue(keyId, out var keyData);
            return Task.FromResult(keyData);
        }

        public Task SaveKeyDataAsync(string keyId, KeyData keyData, CancellationToken cancellationToken = default)
        {
            _keys[keyId] = keyData;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// In-memory implementation of key read model store (for development/testing)
    /// </summary>
    public class InMemoryKeyReadModelStore : IKeyReadModelStore
    {
        private readonly ConcurrentDictionary<string, KeyReadModel> _models = new();
        private readonly ConcurrentDictionary<string, KeyUsageStatistics> _statistics = new();

        public Task<KeyReadModel?> GetByIdAsync(string keyId, CancellationToken cancellationToken = default)
        {
            _models.TryGetValue(keyId, out var model);
            return Task.FromResult(model);
        }

        public Task<IEnumerable<KeyReadModel>> GetByTypeAsync(string keyType, CancellationToken cancellationToken = default)
        {
            var results = _models.Values.Where(m => m.KeyType == keyType);
            return Task.FromResult(results);
        }

        public Task<IEnumerable<KeyReadModel>> GetActiveKeysAsync(int? limit, CancellationToken cancellationToken = default)
        {
            var query = _models.Values.Where(m => m.Status == "Active");

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return Task.FromResult(query.AsEnumerable());
        }

        public Task<IEnumerable<KeyReadModel>> GetExpiringKeysAsync(DateTime before, CancellationToken cancellationToken = default)
        {
            var results = _models.Values.Where(m =>
                m.ExpiresAt.HasValue &&
                m.ExpiresAt.Value <= before &&
                m.Status != "Revoked");

            return Task.FromResult(results);
        }

        public Task<IEnumerable<KeyReadModel>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var results = _models.Values.Where(m =>
                m.AuthorizedUsers.Contains(userId) ||
                m.CreatedBy == userId);

            return Task.FromResult(results);
        }

        public Task<KeyUsageStatistics> GetUsageStatisticsAsync(
            string keyId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            if (!_statistics.TryGetValue(keyId, out var stats))
            {
                stats = new KeyUsageStatistics
                {
                    KeyId = keyId,
                    TotalUsageCount = 0,
                    SignOperations = 0,
                    VerifyOperations = 0,
                    EncryptOperations = 0,
                    DecryptOperations = 0,
                    UsageByUser = new Dictionary<string, int>(),
                    DailyUsage = new Dictionary<DateTime, int>()
                };
            }

            // Filter by date range (in a real implementation)
            return Task.FromResult(stats);
        }

        public Task<IEnumerable<KeyReadModel>> SearchAsync(
            string? searchTerm,
            string? keyType,
            string? algorithm,
            KeyStatusFilter? status,
            int offset,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var query = _models.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    m.KeyId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.KeyType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    m.Algorithm.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(keyType))
                query = query.Where(m => m.KeyType == keyType);

            if (!string.IsNullOrWhiteSpace(algorithm))
                query = query.Where(m => m.Algorithm == algorithm);

            if (status.HasValue && status != KeyStatusFilter.All)
            {
                query = status.Value switch
                {
                    KeyStatusFilter.Active => query.Where(m => m.Status == "Active"),
                    KeyStatusFilter.Inactive => query.Where(m => m.Status == "Created"),
                    KeyStatusFilter.Revoked => query.Where(m => m.Status == "Revoked"),
                    KeyStatusFilter.Expired => query.Where(m => m.IsExpired),
                    _ => query
                };
            }

            var results = query
                .OrderByDescending(m => m.CreatedAt)
                .Skip(offset)
                .Take(limit);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task SaveAsync(KeyReadModel model, CancellationToken cancellationToken = default)
        {
            _models[model.KeyId] = model;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(KeyReadModel model, CancellationToken cancellationToken = default)
        {
            _models[model.KeyId] = model;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string keyId, CancellationToken cancellationToken = default)
        {
            _models.TryRemove(keyId, out _);
            _statistics.TryRemove(keyId, out _);
            return Task.CompletedTask;
        }
    }
}