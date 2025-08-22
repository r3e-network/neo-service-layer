using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.ServiceFramework.Models;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

/// <summary>
/// PostgreSQL repository for SGX sealed data storage
/// Provides unified persistence for confidential data across all services
/// </summary>
public class PostgreSQLSealedDataRepository : ISealedDataRepository
{
    private readonly NeoServiceLayerDbContext _context;
    private readonly ILogger<PostgreSQLSealedDataRepository> _logger;

    public PostgreSQLSealedDataRepository(
        NeoServiceLayerDbContext context,
        ILogger<PostgreSQLSealedDataRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Stores sealed data item in PostgreSQL database
    /// </summary>
    public async Task<SealedDataItem> StoreAsync(
        string key,
        string serviceName,
        byte[] sealedData,
        SealingPolicyType policyType,
        DateTime expiresAt,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Storing sealed data item with key {Key} for service {ServiceName}", key, serviceName);

            // Check if item already exists
            var existingItem = await _context.SealedDataItems
                .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

            if (existingItem != null)
            {
                // Update existing item
                existingItem.SealedData = sealedData;
                existingItem.SealedSize = sealedData.Length;
                existingItem.PolicyType = policyType.ToString();
                existingItem.ExpiresAt = expiresAt;
                existingItem.Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null;
                existingItem.AccessCount = 0; // Reset access count on update
                
                _context.SealedDataItems.Update(existingItem);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Updated existing sealed data item with key {Key}", key);
                return MapToModel(existingItem);
            }

            // Create new item
            var dbItem = new SealedDataItem
            {
                Id = Guid.NewGuid(),
                Key = key,
                ServiceName = serviceName,
                StorageId = $"seal_{Guid.NewGuid():N}",
                SealedData = sealedData,
                OriginalSize = 0, // Will be set by caller if known
                SealedSize = sealedData.Length,
                Fingerprint = ComputeFingerprint(sealedData),
                PolicyType = policyType.ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                AccessCount = 0,
                Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null
            };

            _context.SealedDataItems.Add(dbItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Stored new sealed data item with key {Key} and ID {Id}", key, dbItem.Id);
            
            return MapToModel(dbItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store sealed data item with key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Retrieves sealed data item by key
    /// </summary>
    public async Task<SealedDataItem?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving sealed data item with key {Key}", key);

            var dbItem = await _context.SealedDataItems
                .FirstOrDefaultAsync(x => x.Key == key && !x.IsExpired, cancellationToken);

            if (dbItem == null)
            {
                _logger.LogDebug("Sealed data item with key {Key} not found or expired", key);
                return null;
            }

            // Update access tracking
            dbItem.LastAccessed = DateTime.UtcNow;
            dbItem.AccessCount++;
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Retrieved sealed data item with key {Key}, access count: {AccessCount}", key, dbItem.AccessCount);
            
            return MapToModel(dbItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve sealed data item with key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Lists sealed data items for a service with pagination
    /// </summary>
    public async Task<(IEnumerable<SealedDataItem> Items, int TotalCount)> ListByServiceAsync(
        string serviceName,
        int page = 1,
        int pageSize = 50,
        string? keyPrefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing sealed data items for service {ServiceName}, page {Page}", serviceName, page);

            var query = _context.SealedDataItems
                .Where(x => x.ServiceName == serviceName && !x.IsExpired);

            if (!string.IsNullOrEmpty(keyPrefix))
            {
                query = query.Where(x => x.Key.StartsWith(keyPrefix));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var models = items.Select(MapToModel);

            _logger.LogDebug("Retrieved {ItemCount} sealed data items for service {ServiceName}", items.Count, serviceName);
            
            return (models, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list sealed data items for service {ServiceName}", serviceName);
            throw;
        }
    }

    /// <summary>
    /// Deletes sealed data item by key
    /// </summary>
    public async Task<bool> DeleteByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting sealed data item with key {Key}", key);

            var dbItem = await _context.SealedDataItems
                .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

            if (dbItem == null)
            {
                _logger.LogDebug("Sealed data item with key {Key} not found for deletion", key);
                return false;
            }

            // Securely overwrite data before deletion
            SecurelyOverwriteData(dbItem.SealedData);

            _context.SealedDataItems.Remove(dbItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Deleted sealed data item with key {Key}", key);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sealed data item with key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Cleans up expired sealed data items
    /// </summary>
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting cleanup of expired sealed data items");

            var expiredItems = await _context.SealedDataItems
                .Where(x => x.IsExpired)
                .ToListAsync(cancellationToken);

            if (!expiredItems.Any())
            {
                _logger.LogDebug("No expired sealed data items found for cleanup");
                return 0;
            }

            // Securely overwrite data before deletion
            foreach (var item in expiredItems)
            {
                SecurelyOverwriteData(item.SealedData);
            }

            _context.SealedDataItems.RemoveRange(expiredItems);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} expired sealed data items", expiredItems.Count);
            
            return expiredItems.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sealed data items");
            throw;
        }
    }

    /// <summary>
    /// Gets storage statistics for all services
    /// </summary>
    public async Task<Dictionary<string, ServiceStorageInfo>> GetStorageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving storage statistics for all services");

            var statistics = await _context.SealedDataItems
                .Where(x => !x.IsExpired)
                .GroupBy(x => x.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    ItemCount = g.Count(),
                    TotalSize = g.Sum(x => (long)x.SealedSize)
                })
                .ToListAsync(cancellationToken);

            var result = statistics.ToDictionary(
                s => s.ServiceName,
                s => new ServiceStorageInfo
                {
                    ServiceName = s.ServiceName,
                    ItemCount = s.ItemCount,
                    TotalSize = s.TotalSize,
                    QuotaUsedPercent = 0 // Will be calculated by caller if quota is known
                });

            _logger.LogDebug("Retrieved storage statistics for {ServiceCount} services", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve storage statistics");
            throw;
        }
    }

    /// <summary>
    /// Maps database entity to domain model
    /// </summary>
    private static SealedDataItem MapToModel(SealedDataItem dbItem)
    {
        var metadata = !string.IsNullOrEmpty(dbItem.Metadata) 
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dbItem.Metadata)
            : null;

        return new SealedDataItem
        {
            Key = dbItem.Key,
            StorageId = dbItem.StorageId,
            SealedData = dbItem.SealedData,
            OriginalSize = dbItem.OriginalSize,
            SealedSize = dbItem.SealedSize,
            Fingerprint = dbItem.Fingerprint,
            Service = dbItem.ServiceName,
            PolicyType = Enum.Parse<SealingPolicyType>(dbItem.PolicyType),
            CreatedAt = dbItem.CreatedAt,
            ExpiresAt = dbItem.ExpiresAt,
            LastAccessed = dbItem.LastAccessed,
            Metadata = metadata,
            AccessCount = dbItem.AccessCount
        };
    }

    /// <summary>
    /// Computes SHA256 fingerprint for data integrity
    /// </summary>
    private static string ComputeFingerprint(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Securely overwrites sensitive data before deletion
    /// </summary>
    private static void SecurelyOverwriteData(byte[] data)
    {
        if (data == null || data.Length == 0) return;

        // Overwrite with random data
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(data);
        
        // Clear with zeros
        Array.Clear(data, 0, data.Length);
    }
}

/// <summary>
/// Repository interface for sealed data operations
/// </summary>
public interface ISealedDataRepository
{
    Task<SealedDataItem> StoreAsync(string key, string serviceName, byte[] sealedData, SealingPolicyType policyType, DateTime expiresAt, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);
    Task<SealedDataItem?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<(IEnumerable<SealedDataItem> Items, int TotalCount)> ListByServiceAsync(string serviceName, int page = 1, int pageSize = 50, string? keyPrefix = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, ServiceStorageInfo>> GetStorageStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service storage information for statistics
/// </summary>
public class ServiceStorageInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public long TotalSize { get; set; }
    public double QuotaUsedPercent { get; set; }
}