using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.SgxEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories
{
    /// <summary>
    /// Sealed data repository interface for SGX enclave storage.
    /// </summary>
    public interface ISealedDataRepository
    {
        Task<SealedDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<SealedDataItem?> GetByKeyAsync(string key, string serviceName, CancellationToken cancellationToken = default);
        Task<IEnumerable<SealedDataItem>> GetByServiceAsync(string serviceName, CancellationToken cancellationToken = default);
        Task<IEnumerable<SealedDataItem>> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<SealedDataItem> StoreAsync(string key, string serviceName, byte[] sealedData, SealingPolicyType policyType, DateTime expiresAt, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
        Task<SealedDataItem> UpdateAsync(SealedDataItem item, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task DeleteByKeyAsync(string key, string serviceName, CancellationToken cancellationToken = default);
        Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
        Task<long> GetStorageUsageAsync(string? serviceName = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sealing policy types for SGX data storage.
    /// </summary>
    public enum SealingPolicyType
    {
        MRENCLAVE = 0,  // Seal to specific enclave
        MRSIGNER = 1    // Seal to signer identity
    }

    /// <summary>
    /// PostgreSQL implementation of sealed data repository for SGX enclave storage.
    /// </summary>
    public class SealedDataRepository : ISealedDataRepository
    {
        private readonly NeoServiceLayerDbContext _context;
        private readonly ILogger<SealedDataRepository> _logger;

        public SealedDataRepository(
            NeoServiceLayerDbContext context,
            ILogger<SealedDataRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SealedDataItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

                if (item != null)
                {
                    // Update access tracking
                    item.AccessCount++;
                    item.LastAccessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sealed data item by ID {Id}", id);
                throw;
            }
        }

        public async Task<SealedDataItem?> GetByKeyAsync(string key, string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName && s.IsActive 
                        && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow), cancellationToken);

                if (item != null)
                {
                    // Update access tracking
                    item.AccessCount++;
                    item.LastAccessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sealed data item by key {Key} for service {ServiceName}", key, serviceName);
                throw;
            }
        }

        public async Task<IEnumerable<SealedDataItem>> GetByServiceAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SealedDataItems
                    .Where(s => s.ServiceName == serviceName && s.IsActive 
                        && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow))
                    .OrderBy(s => s.Key)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sealed data items for service {ServiceName}", serviceName);
                throw;
            }
        }

        public async Task<IEnumerable<SealedDataItem>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SealedDataItems
                    .Where(s => s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow))
                    .OrderBy(s => s.ServiceName)
                    .ThenBy(s => s.Key)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active sealed data items");
                throw;
            }
        }

        public async Task<SealedDataItem> StoreAsync(string key, string serviceName, byte[] sealedData, SealingPolicyType policyType, DateTime expiresAt, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if item already exists
                var existing = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName, cancellationToken);

                if (existing != null)
                {
                    // Update existing item
                    existing.SealedData = sealedData;
                    existing.SealingPolicy = (int)policyType;
                    existing.ExpiresAt = expiresAt;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.IsActive = true;
                    existing.Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null;

                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Updated sealed data item {Key} for service {ServiceName}", key, serviceName);
                    return existing;
                }
                else
                {
                    // Create new item
                    var item = new SealedDataItem
                    {
                        Id = Guid.NewGuid(),
                        Key = key,
                        ServiceName = serviceName,
                        SealedData = sealedData,
                        SealingPolicy = (int)policyType,
                        Version = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        AccessCount = 0,
                        IsActive = true,
                        Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null
                    };

                    _context.SealedDataItems.Add(item);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Stored sealed data item {Key} for service {ServiceName}", key, serviceName);
                    return item;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store sealed data item {Key} for service {ServiceName}", key, serviceName);
                throw;
            }
        }

        public async Task<SealedDataItem> UpdateAsync(SealedDataItem item, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Id == item.Id, cancellationToken);

                if (existing == null)
                {
                    throw new InvalidOperationException($"Sealed data item with ID {item.Id} not found");
                }

                // Update properties
                existing.SealedData = item.SealedData;
                existing.SealingPolicy = item.SealingPolicy;
                existing.Version = item.Version;
                existing.ExpiresAt = item.ExpiresAt;
                existing.IsActive = item.IsActive;
                existing.Metadata = item.Metadata;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated sealed data item {Key} for service {ServiceName}", existing.Key, existing.ServiceName);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update sealed data item {Id}", item.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

                if (item != null)
                {
                    // Soft delete
                    item.IsActive = false;
                    item.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Deleted sealed data item {Key} for service {ServiceName}", item.Key, item.ServiceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete sealed data item {Id}", id);
                throw;
            }
        }

        public async Task DeleteByKeyAsync(string key, string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _context.SealedDataItems
                    .FirstOrDefaultAsync(s => s.Key == key && s.ServiceName == serviceName, cancellationToken);

                if (item != null)
                {
                    // Soft delete
                    item.IsActive = false;
                    item.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Deleted sealed data item {Key} for service {ServiceName}", key, serviceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete sealed data item {Key} for service {ServiceName}", key, serviceName);
                throw;
            }
        }

        public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredItems = await _context.SealedDataItems
                    .Where(s => s.ExpiresAt != null && s.ExpiresAt <= now && s.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var item in expiredItems)
                {
                    item.IsActive = false;
                    item.UpdatedAt = now;
                }

                if (expiredItems.Any())
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cleaned up {Count} expired sealed data items", expiredItems.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired sealed data items");
                throw;
            }
        }

        public async Task<long> GetStorageUsageAsync(string? serviceName = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.SealedDataItems
                    .Where(s => s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));

                if (!string.IsNullOrEmpty(serviceName))
                {
                    query = query.Where(s => s.ServiceName == serviceName);
                }

                var totalSize = await query.SumAsync(s => (long)s.SealedData.Length, cancellationToken);
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage usage for service {ServiceName}", serviceName ?? "all");
                throw;
            }
        }
    }
}