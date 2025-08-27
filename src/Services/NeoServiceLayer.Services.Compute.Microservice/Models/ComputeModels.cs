using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Neo.Compute.Service.Models;

public class ComputeJob
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string JobType { get; set; } // "DataProcessing", "Encryption", "Computation", "ML"
    public required string Algorithm { get; set; }
    public ComputeJobStatus Status { get; set; } = ComputeJobStatus.Pending;
    public ComputeJobPriority Priority { get; set; } = ComputeJobPriority.Normal;
    public required string InputDataHash { get; set; }
    public string? OutputDataHash { get; set; }
    public string? ResultHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan? EstimatedDuration { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    public string Configuration { get; set; } = "{}";
    public string Metadata { get; set; } = "{}";
    
    // SGX specific fields
    public Guid? EnclaveId { get; set; }
    public string? AttestationReport { get; set; }
    public bool RequiresSgx { get; set; }
    
    // Resource allocation
    public int CpuCores { get; set; } = 1;
    public long MemoryMb { get; set; } = 512;
    public long StorageMb { get; set; } = 100;
    
    // Navigation properties
    public SgxEnclave? Enclave { get; set; }
    public List<ComputeJobLog> Logs { get; set; } = new();
}

public class SgxEnclave
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string EnclaveHash { get; set; }
    public required string Version { get; set; }
    public SgxEnclaveStatus Status { get; set; } = SgxEnclaveStatus.Initializing;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string? NodeName { get; set; } // Kubernetes node
    public string? PodName { get; set; }
    public int Port { get; set; } = 8080;
    public long MemoryUsageMb { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ActiveJobs { get; set; }
    public int MaxConcurrentJobs { get; set; } = 5;
    public string Configuration { get; set; } = "{}";
    
    // Attestation information
    public string? Quote { get; set; }
    public string? Certificate { get; set; }
    public DateTime? LastAttestation { get; set; }
    public bool IsAttested { get; set; }
    
    // Navigation properties
    public List<ComputeJob> Jobs { get; set; } = new();
    public List<AttestationResult> AttestationResults { get; set; } = new();
}

public class AttestationResult
{
    public Guid Id { get; set; }
    public required Guid EnclaveId { get; set; }
    public required string Quote { get; set; }
    public required string Certificate { get; set; }
    public AttestationStatus Status { get; set; } = AttestationStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationDetails { get; set; }
    public string? ErrorMessage { get; set; }
    public string TcbLevel { get; set; } = "UpToDate";
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Navigation property
    public SgxEnclave Enclave { get; set; } = null!;
}

public class ComputeJobLog
{
    public Guid Id { get; set; }
    public required Guid JobId { get; set; }
    public LogLevel Level { get; set; } = LogLevel.Information;
    public required string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
    
    // Navigation property
    public ComputeJob Job { get; set; } = null!;
}

public class ResourceAllocation
{
    public Guid Id { get; set; }
    public required Guid JobId { get; set; }
    public required string ResourceType { get; set; } // "CPU", "Memory", "Storage", "Network"
    public decimal AllocatedAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public string Unit { get; set; } = "";
    public DateTime AllocatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public decimal Cost { get; set; }
    
    // Navigation property
    public ComputeJob Job { get; set; } = null!;
}

public class SecureComputationSession
{
    public Guid Id { get; set; }
    public required string SessionToken { get; set; }
    public required Guid EnclaveId { get; set; }
    public required string UserId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? EncryptedData { get; set; }
    public string? DataHash { get; set; }
    
    // Navigation property
    public SgxEnclave Enclave { get; set; } = null!;
}

public enum ComputeJobStatus
{
    Pending = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Timeout = 6
}

public enum ComputeJobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum SgxEnclaveStatus
{
    Initializing = 0,
    Ready = 1,
    Running = 2,
    Busy = 3,
    Error = 4,
    Stopped = 5,
    Crashed = 6
}

public enum AttestationStatus
{
    Pending = 0,
    InProgress = 1,
    Verified = 2,
    Failed = 3,
    Expired = 4,
    Revoked = 5
}

public enum SessionStatus
{
    Active = 0,
    Idle = 1,
    Expired = 2,
    Terminated = 3
}

// Request/Response DTOs
public class CreateComputeJobRequest
{
    [Required]
    [StringLength(50)]
    public required string JobType { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Algorithm { get; set; }
    
    [Required]
    public required string InputDataHash { get; set; }
    
    public ComputeJobPriority Priority { get; set; } = ComputeJobPriority.Normal;
    
    public bool RequiresSgx { get; set; } = true;
    
    [Range(1, 32)]
    public int CpuCores { get; set; } = 1;
    
    [Range(128, 32768)]
    public long MemoryMb { get; set; } = 512;
    
    [Range(10, 10240)]
    public long StorageMb { get; set; } = 100;
    
    public Dictionary<string, object>? Configuration { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ComputeJobResponse
{
    public required string JobId { get; set; }
    public required string Status { get; set; }
    public string? EnclaveId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? EstimatedDuration { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    public int Progress { get; set; } = 0; // 0-100%
    public string? ResultHash { get; set; }
    public string? ErrorMessage { get; set; }
    public ComputeResourceUsage? ResourceUsage { get; set; }
}

public class ComputeResourceUsage
{
    public int CpuCores { get; set; }
    public long MemoryUsedMb { get; set; }
    public long StorageUsedMb { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal Cost { get; set; }
}

public class CreateEnclaveRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [Required]
    public required string EnclaveHash { get; set; }
    
    [Required]
    [StringLength(20)]
    public required string Version { get; set; }
    
    [Range(1, 50)]
    public int MaxConcurrentJobs { get; set; } = 5;
    
    public Dictionary<string, object>? Configuration { get; set; }
}

public class EnclaveResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int ActiveJobs { get; set; }
    public int MaxConcurrentJobs { get; set; }
    public bool IsAttested { get; set; }
    public DateTime? LastAttestation { get; set; }
    public ComputeResourceUsage? ResourceUsage { get; set; }
}

public class AttestationRequest
{
    [Required]
    public required string EnclaveId { get; set; }
    
    [Required]
    public required string Quote { get; set; }
    
    [Required]
    public required string Certificate { get; set; }
}

public class AttestationResponse
{
    public required string AttestationId { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsValid { get; set; }
    public string? TcbLevel { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SecureComputationRequest
{
    [Required]
    public required string Algorithm { get; set; }
    
    [Required]
    public required string EncryptedData { get; set; }
    
    [Required]
    public required string DataHash { get; set; }
    
    public string? EnclaveId { get; set; }
    public TimeSpan? TimeoutDuration { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

public class SecureComputationResponse
{
    public required string SessionId { get; set; }
    public required string EnclaveId { get; set; }
    public required string EncryptedResult { get; set; }
    public required string ResultHash { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string AttestationProof { get; set; } = "";
    public ComputeResourceUsage? ResourceUsage { get; set; }
}

public class ComputeStatistics
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int AvailableEnclaves { get; set; }
    public int BusyEnclaves { get; set; }
    public decimal AverageJobDurationMinutes { get; set; }
    public decimal SuccessRate { get; set; }
    public long TotalCpuHours { get; set; }
    public long TotalMemoryGbHours { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<EnclaveStatistic> EnclaveStats { get; set; } = new();
}

public class EnclaveStatistic
{
    public required string EnclaveId { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; }
    public int JobsProcessed { get; set; }
    public decimal AverageJobDuration { get; set; }
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public DateTime LastActivity { get; set; }
}

public class JobQueueStatus
{
    public int PendingJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int RunningJobs { get; set; }
    public Dictionary<string, int> JobsByPriority { get; set; } = new();
    public Dictionary<string, int> JobsByType { get; set; } = new();
    public decimal AverageWaitTimeMinutes { get; set; }
    public decimal EstimatedProcessingTimeMinutes { get; set; }
}