using Microsoft.EntityFrameworkCore;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Models;
using Neo.Storage.Service.Services;
using System.Text.Json;

namespace Neo.Storage.Service.Services;

public class StorageObjectService : IStorageObjectService
{
    private readonly StorageDbContext _context;
    private readonly IShardingService _shardingService;
    private readonly IReplicationService _replicationService;
    private readonly IDistributedHashService _hashService;
    private readonly IBucketService _bucketService;
    private readonly ILogger<StorageObjectService> _logger;

    public StorageObjectService(
        StorageDbContext context,
        IShardingService shardingService,
        IReplicationService replicationService,
        IDistributedHashService hashService,
        IBucketService bucketService,
        ILogger<StorageObjectService> logger)
    {
        _context = context;
        _shardingService = shardingService;
        _replicationService = replicationService;
        _hashService = hashService;
        _bucketService = bucketService;
        _logger = logger;
    }

    public async Task<StorageObjectResponse> UploadObjectAsync(UploadObjectRequest request, string userId)
    {
        try
        {
            // Validate bucket exists and user has access
            var bucket = await _bucketService.GetBucketAsync(request.BucketName, userId);
            if (bucket == null)
            {
                throw new UnauthorizedAccessException($"Bucket not found or access denied: {request.BucketName}");
            }

            // Check if object already exists
            var existingObject = await _context.StorageObjects
                .FirstOrDefaultAsync(o => o.BucketName == request.BucketName && o.Key == request.Key);

            // Calculate content hash
            var contentHash = await _hashService.CalculateHashAsync(request.Content);
            var contentLength = request.Content.Length;

            // Create or update object
            StorageObject storageObject;
            if (existingObject != null)
            {
                // Update existing object
                storageObject = existingObject;
                storageObject.Hash = contentHash;
                storageObject.Size = contentLength;
                storageObject.ContentType = request.ContentType;
                storageObject.ContentEncoding = request.ContentEncoding;
                storageObject.StorageClass = request.StorageClass;
                storageObject.IsEncrypted = request.IsEncrypted;
                storageObject.ExpiresAt = request.ExpiresAt;
                storageObject.UpdatedAt = DateTime.UtcNow;
                storageObject.Status = ObjectStatus.Active;

                // Create new version if bucket has versioning enabled
                if (bucket.IsVersioned)
                {
                    await CreateObjectVersionAsync(storageObject);
                }
            }
            else
            {
                // Create new object
                storageObject = new StorageObject
                {
                    Id = Guid.NewGuid(),
                    Key = request.Key,
                    BucketName = request.BucketName,
                    Size = contentLength,
                    ContentType = request.ContentType,
                    ContentEncoding = request.ContentEncoding,
                    Hash = contentHash,
                    UserId = userId,
                    StorageClass = request.StorageClass,
                    IsEncrypted = request.IsEncrypted,
                    ExpiresAt = request.ExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = ObjectStatus.Active
                };

                if (request.Metadata != null)
                {
                    storageObject.Metadata = JsonSerializer.Serialize(request.Metadata);
                }

                _context.StorageObjects.Add(storageObject);
            }

            await _context.SaveChangesAsync();

            // Create replicas across distributed nodes
            var replicationFactor = bucket.ReplicationFactor;
            await _replicationService.CreateReplicasAsync(storageObject.Id, replicationFactor);

            // Generate ETag
            storageObject.Etag = await _hashService.GenerateEtagAsync(contentHash, storageObject.UpdatedAt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Object uploaded successfully: {BucketName}/{Key} ({ObjectId})", 
                request.BucketName, request.Key, storageObject.Id);

            // Log access
            await LogAccessAsync(storageObject.Id, userId, "PUT", "Object uploaded", 0);

            return await MapToResponseAsync(storageObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object: {BucketName}/{Key}", request.BucketName, request.Key);
            throw;
        }
    }

    public async Task<Stream> DownloadObjectAsync(string bucketName, string key, string userId)
    {
        try
        {
            var storageObject = await GetObjectEntityAsync(bucketName, key, userId);
            if (storageObject == null)
            {
                throw new FileNotFoundException($"Object not found: {bucketName}/{key}");
            }

            // Get available replicas
            var replicas = await _replicationService.GetAvailableReplicasAsync(storageObject.Id);
            if (!replicas.Any())
            {
                throw new InvalidOperationException($"No available replicas for object: {bucketName}/{key}");
            }

            // Try to download from replicas (starting with primary)
            var primaryReplica = await _replicationService.GetPrimaryReplicaAsync(storageObject.Id);
            var replicasToTry = primaryReplica != null 
                ? new[] { primaryReplica }.Concat(replicas.Where(r => r.Id != primaryReplica.Id))
                : replicas;

            foreach (var replica in replicasToTry)
            {
                try
                {
                    var stream = await DownloadFromReplicaAsync(replica);
                    if (stream != null)
                    {
                        // Log successful access
                        await LogAccessAsync(storageObject.Id, userId, "GET", "Object downloaded", storageObject.Size);
                        
                        _logger.LogDebug("Successfully downloaded object from replica {ReplicaId} on node {NodeId}", 
                            replica.Id, replica.NodeId);
                        
                        return stream;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download from replica {ReplicaId}, trying next replica", replica.Id);
                }
            }

            throw new InvalidOperationException($"Failed to download object from any replica: {bucketName}/{key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download object: {BucketName}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<StorageObjectResponse?> GetObjectAsync(string bucketName, string key, string userId)
    {
        try
        {
            var storageObject = await GetObjectEntityAsync(bucketName, key, userId);
            return storageObject != null ? await MapToResponseAsync(storageObject) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object metadata: {BucketName}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<List<StorageObjectResponse>> ListObjectsAsync(string bucketName, string userId, string? prefix = null, int maxKeys = 1000)
    {
        try
        {
            // Validate bucket access
            var bucket = await _bucketService.GetBucketAsync(bucketName, userId);
            if (bucket == null)
            {
                throw new UnauthorizedAccessException($"Bucket not found or access denied: {bucketName}");
            }

            var query = _context.StorageObjects
                .Where(o => o.BucketName == bucketName && o.Status == ObjectStatus.Active);

            // Apply prefix filter if specified
            if (!string.IsNullOrEmpty(prefix))
            {
                query = query.Where(o => o.Key.StartsWith(prefix));
            }

            // For private buckets, filter by user
            if (bucket.Type == BucketType.Private)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var objects = await query
                .OrderBy(o => o.Key)
                .Take(maxKeys)
                .Include(o => o.Replicas)
                .ToListAsync();

            var responses = new List<StorageObjectResponse>();
            foreach (var obj in objects)
            {
                responses.Add(await MapToResponseAsync(obj));
            }

            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list objects in bucket: {BucketName}", bucketName);
            throw;
        }
    }

    public async Task<bool> DeleteObjectAsync(string bucketName, string key, string userId)
    {
        try
        {
            var storageObject = await GetObjectEntityAsync(bucketName, key, userId);
            if (storageObject == null) return false;

            // Check if bucket has versioning enabled
            var bucket = await _bucketService.GetBucketAsync(bucketName, userId);
            if (bucket?.IsVersioned == true)
            {
                // Create delete marker instead of actual deletion
                await CreateDeleteMarkerAsync(storageObject);
            }
            else
            {
                // Perform actual deletion
                storageObject.Status = ObjectStatus.Deleted;
                
                // Delete all replicas
                await _replicationService.DeleteReplicasAsync(storageObject.Id);
            }

            await _context.SaveChangesAsync();

            // Log access
            await LogAccessAsync(storageObject.Id, userId, "DELETE", "Object deleted", 0);

            _logger.LogInformation("Object deleted: {BucketName}/{Key} ({ObjectId})", 
                bucketName, key, storageObject.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object: {BucketName}/{Key}", bucketName, key);
            return false;
        }
    }

    public async Task<StorageObjectResponse> CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, string userId)
    {
        try
        {
            // Get source object
            var sourceObject = await GetObjectEntityAsync(sourceBucket, sourceKey, userId);
            if (sourceObject == null)
            {
                throw new FileNotFoundException($"Source object not found: {sourceBucket}/{sourceKey}");
            }

            // Validate destination bucket
            var destBucket = await _bucketService.GetBucketAsync(destinationBucket, userId);
            if (destBucket == null)
            {
                throw new UnauthorizedAccessException($"Destination bucket not found or access denied: {destinationBucket}");
            }

            // Create new object for destination
            var destinationObject = new StorageObject
            {
                Id = Guid.NewGuid(),
                Key = destinationKey,
                BucketName = destinationBucket,
                Size = sourceObject.Size,
                ContentType = sourceObject.ContentType,
                ContentEncoding = sourceObject.ContentEncoding,
                Hash = sourceObject.Hash,
                UserId = userId,
                StorageClass = sourceObject.StorageClass,
                IsEncrypted = sourceObject.IsEncrypted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = ObjectStatus.Active,
                Metadata = sourceObject.Metadata
            };

            _context.StorageObjects.Add(destinationObject);
            await _context.SaveChangesAsync();

            // Copy replicas
            var sourceReplicas = await _replicationService.GetObjectReplicasAsync(sourceObject.Id);
            await _replicationService.CreateReplicasAsync(destinationObject.Id, sourceReplicas.Count);

            // Generate ETag for destination
            destinationObject.Etag = await _hashService.GenerateEtagAsync(destinationObject.Hash, destinationObject.UpdatedAt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Object copied: {SourceBucket}/{SourceKey} -> {DestBucket}/{DestKey}", 
                sourceBucket, sourceKey, destinationBucket, destinationKey);

            // Log access for both objects
            await LogAccessAsync(sourceObject.Id, userId, "COPY_SOURCE", "Object copied as source", 0);
            await LogAccessAsync(destinationObject.Id, userId, "COPY_DEST", "Object copied as destination", 0);

            return await MapToResponseAsync(destinationObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy object: {SourceBucket}/{SourceKey} -> {DestBucket}/{DestKey}", 
                sourceBucket, sourceKey, destinationBucket, destinationKey);
            throw;
        }
    }

    public async Task<List<ObjectVersion>> GetObjectVersionsAsync(string bucketName, string key, string userId)
    {
        try
        {
            var storageObject = await GetObjectEntityAsync(bucketName, key, userId);
            if (storageObject == null)
            {
                return new List<ObjectVersion>();
            }

            return await _context.ObjectVersions
                .Where(v => v.ObjectId == storageObject.Id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get object versions: {BucketName}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<bool> RestoreObjectAsync(string bucketName, string key, string versionId, string userId)
    {
        try
        {
            var storageObject = await GetObjectEntityAsync(bucketName, key, userId);
            if (storageObject == null) return false;

            var version = await _context.ObjectVersions
                .FirstOrDefaultAsync(v => v.ObjectId == storageObject.Id && v.VersionId == versionId);

            if (version == null) return false;

            // Restore object to this version
            storageObject.Size = version.Size;
            storageObject.Hash = version.Hash;
            storageObject.Etag = version.Etag;
            storageObject.UpdatedAt = DateTime.UtcNow;
            storageObject.Status = ObjectStatus.Active;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Object restored to version: {BucketName}/{Key} -> {VersionId}", 
                bucketName, key, versionId);

            // Log access
            await LogAccessAsync(storageObject.Id, userId, "RESTORE", $"Object restored to version {versionId}", 0);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore object: {BucketName}/{Key} to version {VersionId}", 
                bucketName, key, versionId);
            return false;
        }
    }

    public async Task<string> GeneratePresignedUrlAsync(string bucketName, string key, TimeSpan expiry, string action = "GET")
    {
        try
        {
            // In a real implementation, this would generate a signed URL
            // with temporary credentials for secure access
            var expiryTime = DateTime.UtcNow.Add(expiry);
            var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{bucketName}/{key}:{action}:{expiryTime:yyyy-MM-ddTHH:mm:ssZ}"));
            
            var presignedUrl = $"https://storage.neo-service-layer.com/{bucketName}/{key}?action={action}&expires={expiryTime:yyyy-MM-ddTHH:mm:ssZ}&signature={signature}";
            
            _logger.LogInformation("Generated presigned URL for {BucketName}/{Key} (action: {Action}, expires: {Expiry})", 
                bucketName, key, action, expiryTime);

            await Task.CompletedTask; // Maintain async signature
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL: {BucketName}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<StorageStatistics> GetStorageStatisticsAsync(string userId)
    {
        try
        {
            var userObjects = _context.StorageObjects.Where(o => o.UserId == userId);
            
            var totalObjects = await userObjects.CountAsync();
            var totalSize = await userObjects.SumAsync(o => o.Size);
            
            var buckets = await _context.StorageBuckets
                .Where(b => b.UserId == userId)
                .CountAsync();

            var objectsByStorageClass = await userObjects
                .GroupBy(o => o.StorageClass)
                .ToDictionaryAsync(g => g.Key.ToString(), g => (long)g.Count());

            var transactions = await _context.StorageTransactions
                .Where(t => t.UserId == userId)
                .CountAsync();

            var totalCost = await _context.StorageTransactions
                .Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed)
                .SumAsync(t => t.Cost);

            return new StorageStatistics
            {
                TotalObjects = totalObjects,
                TotalSize = totalSize,
                TotalBuckets = buckets,
                TotalTransactions = transactions,
                TotalCost = totalCost,
                ObjectsByStorageClass = objectsByStorageClass,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage statistics for user: {UserId}", userId);
            throw;
        }
    }

    private async Task<StorageObject?> GetObjectEntityAsync(string bucketName, string key, string userId)
    {
        // First check if bucket exists and user has access
        var bucket = await _bucketService.GetBucketAsync(bucketName, userId);
        if (bucket == null) return null;

        var query = _context.StorageObjects
            .Where(o => o.BucketName == bucketName && o.Key == key && o.Status == ObjectStatus.Active)
            .Include(o => o.Replicas)
            .ThenInclude(r => r.Node);

        // For private buckets, filter by user
        if (bucket.Type == BucketType.Private)
        {
            query = query.Where(o => o.UserId == userId);
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<StorageObjectResponse> MapToResponseAsync(StorageObject obj)
    {
        var replicas = obj.Replicas.Select(r => new ReplicaInfo
        {
            NodeId = r.NodeId.ToString(),
            NodeName = r.Node?.Name ?? "Unknown",
            Region = r.Node?.Region ?? "Unknown",
            Status = r.Status,
            LastVerified = r.LastVerified,
            IsPrimary = r.IsPrimary
        }).ToList();

        var metadata = string.IsNullOrEmpty(obj.Metadata) 
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(obj.Metadata) ?? new Dictionary<string, string>();

        return new StorageObjectResponse
        {
            Id = obj.Id.ToString(),
            Key = obj.Key,
            BucketName = obj.BucketName,
            Size = obj.Size,
            ContentType = obj.ContentType,
            ContentEncoding = obj.ContentEncoding,
            Hash = obj.Hash,
            Etag = obj.Etag,
            CreatedAt = obj.CreatedAt,
            UpdatedAt = obj.UpdatedAt,
            ExpiresAt = obj.ExpiresAt,
            StorageClass = obj.StorageClass,
            Status = obj.Status,
            ReplicationCount = obj.Replicas.Count(r => r.Status == ReplicaStatus.Active),
            Metadata = metadata,
            Replicas = replicas
        };
    }

    private async Task CreateObjectVersionAsync(StorageObject obj)
    {
        var version = new ObjectVersion
        {
            Id = Guid.NewGuid(),
            ObjectId = obj.Id,
            VersionId = await _hashService.GenerateVersionIdAsync(),
            Size = obj.Size,
            Hash = obj.Hash,
            Etag = obj.Etag,
            CreatedAt = DateTime.UtcNow,
            IsLatest = false // The current object is the latest
        };

        // Mark previous versions as not latest
        var previousVersions = await _context.ObjectVersions
            .Where(v => v.ObjectId == obj.Id && v.IsLatest)
            .ToListAsync();

        foreach (var prev in previousVersions)
        {
            prev.IsLatest = false;
        }

        _context.ObjectVersions.Add(version);
    }

    private async Task CreateDeleteMarkerAsync(StorageObject obj)
    {
        var deleteMarker = new ObjectVersion
        {
            Id = Guid.NewGuid(),
            ObjectId = obj.Id,
            VersionId = await _hashService.GenerateVersionIdAsync(),
            Size = 0,
            Hash = "",
            CreatedAt = DateTime.UtcNow,
            IsLatest = true,
            IsDeleted = true,
            DeleteMarker = "DELETE_MARKER"
        };

        // Mark current object as not latest
        obj.Status = ObjectStatus.Deleted;

        _context.ObjectVersions.Add(deleteMarker);
    }

    private async Task<Stream?> DownloadFromReplicaAsync(StorageReplica replica)
    {
        // In a real implementation, this would make an HTTP request to the storage node
        // to download the actual file content. For simulation, we'll return a mock stream.
        
        try
        {
            // Simulate network delay
            await Task.Delay(50);
            
            // Return a mock stream with some content
            var mockContent = $"Mock content for object {replica.ObjectId} from replica {replica.Id}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(mockContent);
            
            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download from replica {ReplicaId}", replica.Id);
            return null;
        }
    }

    private async Task LogAccessAsync(Guid objectId, string userId, string action, string details, long bytesTransferred)
    {
        try
        {
            var accessLog = new AccessLog
            {
                Id = Guid.NewGuid(),
                ObjectId = objectId,
                UserId = userId,
                Action = action,
                IpAddress = "127.0.0.1", // Would come from HTTP context in real implementation
                UserAgent = "Neo-Storage-Service/1.0",
                Timestamp = DateTime.UtcNow,
                BytesTransferred = bytesTransferred,
                ResponseTime = TimeSpan.FromMilliseconds(100), // Would be actual response time
                StatusCode = 200
            };

            _context.AccessLogs.Add(accessLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log access for object {ObjectId}", objectId);
        }
    }
}