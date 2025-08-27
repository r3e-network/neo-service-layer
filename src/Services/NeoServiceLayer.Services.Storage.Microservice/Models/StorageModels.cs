using System.ComponentModel.DataAnnotations;

namespace Neo.Storage.Service.Models;

public class StorageObject
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string BucketName { get; set; }
    public long Size { get; set; }
    public required string ContentType { get; set; }
    public string? ContentEncoding { get; set; }
    public required string Hash { get; set; }
    public string? Etag { get; set; }
    public required string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKey { get; set; }
    public StorageClass StorageClass { get; set; } = StorageClass.Standard;
    public ObjectStatus Status { get; set; } = ObjectStatus.Active;
    public string Metadata { get; set; } = "{}";
    public int ReplicationCount { get; set; } = 3;
    public string? CompressionType { get; set; }
    
    // Distributed storage properties
    public List<StorageReplica> Replicas { get; set; } = new();
    public List<ObjectVersion> Versions { get; set; } = new();
    public List<AccessLog> AccessLogs { get; set; } = new();
}

public class StorageBucket
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public BucketType Type { get; set; } = BucketType.Private;
    public required string Region { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsVersioned { get; set; }
    public bool IsEncrypted { get; set; }
    public string? DefaultEncryptionKey { get; set; }
    public StorageClass DefaultStorageClass { get; set; } = StorageClass.Standard;
    public long TotalSize { get; set; }
    public long ObjectCount { get; set; }
    public int ReplicationFactor { get; set; } = 3;
    public string Configuration { get; set; } = "{}";
    
    // Access control
    public List<BucketPolicy> Policies { get; set; } = new();
    public List<StorageObject> Objects { get; set; } = new();
}

public class StorageReplica
{
    public Guid Id { get; set; }
    public required Guid ObjectId { get; set; }
    public required Guid NodeId { get; set; }
    public required string PhysicalPath { get; set; }
    public ReplicaStatus Status { get; set; } = ReplicaStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime LastVerified { get; set; }
    public string? Hash { get; set; }
    public long Size { get; set; }
    public bool IsPrimary { get; set; }
    
    // Navigation properties
    public StorageObject Object { get; set; } = null!;
    public StorageNode Node { get; set; } = null!;
}

public class StorageNode
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Endpoint { get; set; }
    public required string Region { get; set; }
    public required string Zone { get; set; }
    public NodeStatus Status { get; set; } = NodeStatus.Active;
    public NodeType Type { get; set; } = NodeType.Standard;
    public DateTime CreatedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public long TotalCapacity { get; set; }
    public long UsedCapacity { get; set; }
    public long AvailableCapacity { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double NetworkLatency { get; set; }
    public int Priority { get; set; } = 100;
    public string Configuration { get; set; } = "{}";
    
    // Navigation properties
    public List<StorageReplica> Replicas { get; set; } = new();
    public List<NodeHealth> HealthChecks { get; set; } = new();
}

public class ObjectVersion
{
    public Guid Id { get; set; }
    public required Guid ObjectId { get; set; }
    public required string VersionId { get; set; }
    public long Size { get; set; }
    public required string Hash { get; set; }
    public string? Etag { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLatest { get; set; }
    public bool IsDeleted { get; set; }
    public string? DeleteMarker { get; set; }
    
    // Navigation property
    public StorageObject Object { get; set; } = null!;
}

public class BucketPolicy
{
    public Guid Id { get; set; }
    public required Guid BucketId { get; set; }
    public required string Principal { get; set; } // User/Role/Group
    public required string Effect { get; set; } // Allow/Deny
    public required string[] Actions { get; set; }
    public string[]? Resources { get; set; }
    public string? Condition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public StorageBucket Bucket { get; set; } = null!;
}

public class AccessLog
{
    public Guid Id { get; set; }
    public required Guid ObjectId { get; set; }
    public required string UserId { get; set; }
    public required string Action { get; set; } // GET, PUT, DELETE, etc.
    public required string IpAddress { get; set; }
    public required string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public long BytesTransferred { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation property
    public StorageObject Object { get; set; } = null!;
}

public class NodeHealth
{
    public Guid Id { get; set; }
    public required Guid NodeId { get; set; }
    public HealthStatus Status { get; set; }
    public DateTime CheckedAt { get; set; }
    public double ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string HealthData { get; set; } = "{}";
    
    // Navigation property
    public StorageNode Node { get; set; } = null!;
}

public class ReplicationJob
{
    public Guid Id { get; set; }
    public required Guid ObjectId { get; set; }
    public required Guid SourceNodeId { get; set; }
    public required Guid TargetNodeId { get; set; }
    public ReplicationJobStatus Status { get; set; } = ReplicationJobStatus.Pending;
    public ReplicationJobType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long BytesTransferred { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int Priority { get; set; } = 100;
}

public class StorageTransaction
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Operations { get; set; } = "[]"; // JSON array of operations
    public string? ErrorMessage { get; set; }
    public long TotalBytes { get; set; }
    public decimal Cost { get; set; }
}

// Enums
public enum StorageClass
{
    Standard = 0,
    Infrequent = 1,
    Archive = 2,
    DeepArchive = 3,
    Intelligent = 4
}

public enum ObjectStatus
{
    Active = 0,
    Deleted = 1,
    Archived = 2,
    Corrupted = 3,
    Migrating = 4
}

public enum BucketType
{
    Private = 0,
    Public = 1,
    Shared = 2
}

public enum ReplicaStatus
{
    Active = 0,
    Syncing = 1,
    OutOfSync = 2,
    Failed = 3,
    Deleted = 4
}

public enum NodeStatus
{
    Active = 0,
    Inactive = 1,
    Maintenance = 2,
    Failed = 3,
    Decommissioned = 4
}

public enum NodeType
{
    Standard = 0,
    HighPerformance = 1,
    Archive = 2,
    Cache = 3
}

public enum HealthStatus
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Unknown = 3
}

public enum ReplicationJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Retrying = 5
}

public enum ReplicationJobType
{
    InitialReplication = 0,
    RepairReplication = 1,
    MigrationReplication = 2,
    BackupReplication = 3
}

public enum TransactionType
{
    Upload = 0,
    Download = 1,
    Delete = 2,
    Copy = 3,
    Move = 4,
    Restore = 5
}

public enum TransactionStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

// Request/Response DTOs
public class CreateBucketRequest
{
    [Required]
    [StringLength(63, MinimumLength = 3)]
    public required string Name { get; set; }
    
    [Required]
    public required string Region { get; set; }
    
    public BucketType Type { get; set; } = BucketType.Private;
    public bool IsVersioned { get; set; } = false;
    public bool IsEncrypted { get; set; } = true;
    public StorageClass DefaultStorageClass { get; set; } = StorageClass.Standard;
    public int ReplicationFactor { get; set; } = 3;
    public Dictionary<string, object>? Configuration { get; set; }
}

public class UploadObjectRequest
{
    [Required]
    public required string BucketName { get; set; }
    
    [Required]
    public required string Key { get; set; }
    
    [Required]
    public required Stream Content { get; set; }
    
    public string ContentType { get; set; } = "application/octet-stream";
    public string? ContentEncoding { get; set; }
    public StorageClass StorageClass { get; set; } = StorageClass.Standard;
    public Dictionary<string, string>? Metadata { get; set; }
    public bool IsEncrypted { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

public class StorageObjectResponse
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public required string BucketName { get; set; }
    public long Size { get; set; }
    public required string ContentType { get; set; }
    public string? ContentEncoding { get; set; }
    public required string Hash { get; set; }
    public string? Etag { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public StorageClass StorageClass { get; set; }
    public ObjectStatus Status { get; set; }
    public int ReplicationCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<ReplicaInfo> Replicas { get; set; } = new();
}

public class BucketResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public BucketType Type { get; set; }
    public required string Region { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVersioned { get; set; }
    public bool IsEncrypted { get; set; }
    public StorageClass DefaultStorageClass { get; set; }
    public long TotalSize { get; set; }
    public long ObjectCount { get; set; }
    public int ReplicationFactor { get; set; }
}

public class ReplicaInfo
{
    public required string NodeId { get; set; }
    public required string NodeName { get; set; }
    public required string Region { get; set; }
    public ReplicaStatus Status { get; set; }
    public DateTime LastVerified { get; set; }
    public bool IsPrimary { get; set; }
}

public class StorageStatistics
{
    public long TotalObjects { get; set; }
    public long TotalSize { get; set; }
    public int TotalBuckets { get; set; }
    public int ActiveNodes { get; set; }
    public int FailedNodes { get; set; }
    public double AverageReplicationHealth { get; set; }
    public long TotalTransactions { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, long> ObjectsByStorageClass { get; set; } = new();
    public Dictionary<string, long> SizeByRegion { get; set; } = new();
    public List<NodeStatistic> NodeStats { get; set; } = new();
}

public class NodeStatistic
{
    public required string NodeId { get; set; }
    public required string Name { get; set; }
    public required string Region { get; set; }
    public NodeStatus Status { get; set; }
    public long TotalCapacity { get; set; }
    public long UsedCapacity { get; set; }
    public double UtilizationPercent { get; set; }
    public int ReplicaCount { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime LastHeartbeat { get; set; }
}

public class ReplicationHealthReport
{
    public int TotalObjects { get; set; }
    public int HealthyObjects { get; set; }
    public int UnderReplicatedObjects { get; set; }
    public int OverReplicatedObjects { get; set; }
    public int CorruptedObjects { get; set; }
    public double OverallHealthScore { get; set; }
    public List<ReplicationIssue> Issues { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class ReplicationIssue
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string IssueType { get; set; }
    public required string Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public DateTime DetectedAt { get; set; }
    public string? RecommendedAction { get; set; }
}