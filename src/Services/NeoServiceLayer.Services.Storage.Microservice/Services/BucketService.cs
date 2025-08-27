using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Security.Cryptography;
using System.Text.Json;

namespace Neo.Storage.Service.Services;

public class BucketService : IBucketService
{
    private readonly StorageDbContext _context;
    private readonly ILogger<BucketService> _logger;

    public BucketService(
        StorageDbContext context,
        ILogger<BucketService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BucketResponse> CreateBucketAsync(CreateBucketRequest request, string userId)
    {
        try
        {
            // Validate bucket name
            if (!IsValidBucketName(request.Name))
            {
                throw new ArgumentException("Invalid bucket name. Bucket names must be 3-63 characters, lowercase, and contain only letters, numbers, and hyphens.");
            }

            // Check if bucket already exists
            var existingBucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == request.Name);

            if (existingBucket != null)
            {
                throw new InvalidOperationException($"Bucket '{request.Name}' already exists");
            }

            var bucket = new StorageBucket
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                OwnerId = userId,
                Region = request.Region ?? "default",
                StorageClass = request.StorageClass,
                CreatedAt = DateTime.UtcNow,
                IsVersioningEnabled = false,
                IsPublic = false,
                Status = BucketStatus.Active,
                MaxObjectSize = request.MaxObjectSize ?? 5L * 1024 * 1024 * 1024, // 5GB default
                ObjectCount = 0,
                TotalSize = 0
            };

            // Set encryption if provided
            if (!string.IsNullOrEmpty(request.EncryptionKey))
            {
                bucket.EncryptionKey = HashEncryptionKey(request.EncryptionKey);
                bucket.IsEncrypted = true;
            }

            _context.StorageBuckets.Add(bucket);
            await _context.SaveChangesAsync();

            // Create default bucket policy if specified
            if (request.DefaultPolicy != null)
            {
                await SetBucketPolicyAsync(bucket.Name, request.DefaultPolicy, userId);
            }

            _logger.LogInformation("Created bucket {BucketName} for user {UserId}", bucket.Name, userId);

            return MapToResponse(bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bucket {BucketName} for user {UserId}", request.Name, userId);
            throw;
        }
    }

    public async Task<BucketResponse?> GetBucketAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .Include(b => b.Policies)
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null)
            {
                return null;
            }

            // Check access permissions
            if (bucket.OwnerId != userId && !await HasBucketAccessAsync(bucket, userId, "READ"))
            {
                return null; // Return null instead of throwing to maintain security
            }

            return MapToResponse(bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket {BucketName} for user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<List<BucketResponse>> ListBucketsAsync(string userId)
    {
        try
        {
            var ownedBuckets = await _context.StorageBuckets
                .Where(b => b.OwnerId == userId && b.Status == BucketStatus.Active)
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Also include buckets where user has read access through policies
            var accessibleBuckets = await _context.StorageBuckets
                .Where(b => b.OwnerId != userId && b.Status == BucketStatus.Active)
                .Where(b => b.Policies.Any(p => 
                    (p.PrincipalId == userId || p.PrincipalType == "AllUsers") &&
                    (p.Actions.Contains("s3:ListBucket") || p.Actions.Contains("*"))))
                .OrderBy(b => b.Name)
                .ToListAsync();

            var allBuckets = ownedBuckets.Concat(accessibleBuckets).ToList();

            return allBuckets.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list buckets for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteBucketAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .Include(b => b.Objects)
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            // Check if bucket has objects
            var hasObjects = await _context.StorageObjects
                .AnyAsync(o => o.BucketName == bucketName && o.Status != ObjectStatus.Deleted);

            if (hasObjects)
            {
                throw new InvalidOperationException("Cannot delete bucket that contains objects. Delete all objects first.");
            }

            // Soft delete the bucket
            bucket.Status = BucketStatus.Deleted;
            bucket.DeletedAt = DateTime.UtcNow;

            // Delete associated policies
            var policies = await _context.BucketPolicies
                .Where(p => p.BucketName == bucketName)
                .ToListAsync();

            _context.BucketPolicies.RemoveRange(policies);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted bucket {BucketName} for user {UserId}", bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bucket {BucketName} for user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<bool> SetBucketPolicyAsync(string bucketName, BucketPolicy policy, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            // Validate policy
            if (!IsValidPolicy(policy))
            {
                throw new ArgumentException("Invalid bucket policy");
            }

            policy.Id = Guid.NewGuid();
            policy.BucketName = bucketName;
            policy.CreatedAt = DateTime.UtcNow;

            _context.BucketPolicies.Add(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Set bucket policy for bucket {BucketName} by user {UserId}", bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bucket policy for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<List<BucketPolicy>> GetBucketPoliciesAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return new List<BucketPolicy>();
            }

            return await _context.BucketPolicies
                .Where(p => p.BucketName == bucketName)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket policies for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<bool> DeleteBucketPolicyAsync(string bucketName, Guid policyId, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            var policy = await _context.BucketPolicies
                .FirstOrDefaultAsync(p => p.Id == policyId && p.BucketName == bucketName);

            if (policy == null)
            {
                return false;
            }

            _context.BucketPolicies.Remove(policy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted bucket policy {PolicyId} for bucket {BucketName} by user {UserId}", 
                policyId, bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bucket policy {PolicyId} for bucket {BucketName} by user {UserId}", 
                policyId, bucketName, userId);
            throw;
        }
    }

    public async Task<bool> EnableVersioningAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            bucket.IsVersioningEnabled = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Enabled versioning for bucket {BucketName} by user {UserId}", bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable versioning for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<bool> DisableVersioningAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            bucket.IsVersioningEnabled = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Disabled versioning for bucket {BucketName} by user {UserId}", bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable versioning for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<bool> SetBucketEncryptionAsync(string bucketName, string encryptionKey, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return false;
            }

            if (string.IsNullOrEmpty(encryptionKey))
            {
                bucket.IsEncrypted = false;
                bucket.EncryptionKey = null;
            }
            else
            {
                bucket.IsEncrypted = true;
                bucket.EncryptionKey = HashEncryptionKey(encryptionKey);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated encryption settings for bucket {BucketName} by user {UserId}", bucketName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bucket encryption for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    public async Task<StorageStatistics> GetBucketStatisticsAsync(string bucketName, string userId)
    {
        try
        {
            var bucket = await _context.StorageBuckets
                .FirstOrDefaultAsync(b => b.Name == bucketName);

            if (bucket == null || bucket.OwnerId != userId)
            {
                return new StorageStatistics();
            }

            var statistics = await _context.StorageObjects
                .Where(o => o.BucketName == bucketName && o.Status == ObjectStatus.Active)
                .GroupBy(o => 1)
                .Select(g => new 
                {
                    ObjectCount = g.Count(),
                    TotalSize = g.Sum(o => o.Size),
                    AverageSize = g.Average(o => o.Size)
                })
                .FirstOrDefaultAsync();

            var objectsByStorageClass = await _context.StorageObjects
                .Where(o => o.BucketName == bucketName && o.Status == ObjectStatus.Active)
                .GroupBy(o => o.StorageClass)
                .Select(g => new 
                {
                    StorageClass = g.Key,
                    Count = g.Count(),
                    Size = g.Sum(o => o.Size)
                })
                .ToListAsync();

            var recentActivity = await _context.AccessLogs
                .Where(l => l.BucketName == bucketName && l.Timestamp >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            return new StorageStatistics
            {
                BucketName = bucketName,
                ObjectCount = statistics?.ObjectCount ?? 0,
                TotalSize = statistics?.TotalSize ?? 0,
                AverageObjectSize = statistics?.AverageSize ?? 0,
                StorageClassBreakdown = objectsByStorageClass.ToDictionary(
                    x => x.StorageClass.ToString(),
                    x => new StorageClassStat
                    {
                        ObjectCount = x.Count,
                        TotalSize = x.Size
                    }),
                RecentActivityCount = recentActivity,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bucket statistics for bucket {BucketName} by user {UserId}", bucketName, userId);
            throw;
        }
    }

    private static bool IsValidBucketName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 63)
        {
            return false;
        }

        // Must start and end with lowercase letter or number
        if (!char.IsLetterOrDigit(name[0]) || !char.IsLetterOrDigit(name[^1]))
        {
            return false;
        }

        // Can only contain lowercase letters, numbers, and hyphens
        return name.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
    }

    private static bool IsValidPolicy(BucketPolicy policy)
    {
        if (string.IsNullOrEmpty(policy.PrincipalId) && policy.PrincipalType != "AllUsers")
        {
            return false;
        }

        if (policy.Actions == null || policy.Actions.Count == 0)
        {
            return false;
        }

        if (policy.Resources == null || policy.Resources.Count == 0)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> HasBucketAccessAsync(StorageBucket bucket, string userId, string action)
    {
        var policies = await _context.BucketPolicies
            .Where(p => p.BucketName == bucket.Name)
            .Where(p => p.PrincipalId == userId || p.PrincipalType == "AllUsers")
            .Where(p => p.Actions.Contains(action) || p.Actions.Contains("*"))
            .AnyAsync();

        return policies;
    }

    private static string HashEncryptionKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hashBytes);
    }

    private static BucketResponse MapToResponse(StorageBucket bucket)
    {
        return new BucketResponse
        {
            Name = bucket.Name,
            Region = bucket.Region,
            StorageClass = bucket.StorageClass,
            CreatedAt = bucket.CreatedAt,
            IsVersioningEnabled = bucket.IsVersioningEnabled,
            IsEncrypted = bucket.IsEncrypted,
            IsPublic = bucket.IsPublic,
            ObjectCount = bucket.ObjectCount,
            TotalSize = bucket.TotalSize,
            MaxObjectSize = bucket.MaxObjectSize
        };
    }
}