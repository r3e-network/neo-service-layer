using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region Monitoring & Analytics Entities

/// <summary>
/// Generic metric records for all services
/// </summary>
public class MetricRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string MetricType { get; set; } = string.Empty; // Counter, Gauge, Histogram, Summary
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Value { get; set; }
    
    [MaxLength(50)]
    public string? Unit { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "jsonb")]
    public string? Tags { get; set; } // JSON key-value pairs for dimensions
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}

/// <summary>
/// Performance metrics for services and operations
/// </summary>
public class PerformanceMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string OperationName { get; set; } = string.Empty;
    
    public TimeSpan ResponseTime { get; set; }
    
    public TimeSpan? CpuTime { get; set; }
    
    public long? MemoryUsage { get; set; } // in bytes
    
    public int? ThreadCount { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? CpuUtilization { get; set; } // percentage
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MemoryUtilization { get; set; } // percentage
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Success"; // Success, Error, Timeout
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? AdditionalMetrics { get; set; }
}

/// <summary>
/// Security events and alerts
/// </summary>
public class SecurityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // Authentication, Authorization, Intrusion, etc.
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low, Info
    
    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty; // Service name or component
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open"; // Open, Investigating, Resolved, FalsePositive
    
    public DateTime? ResolvedAt { get; set; }
    
    public Guid? ResolvedBy { get; set; }
    
    [MaxLength(1000)]
    public string? Resolution { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? EventData { get; set; }
    
    // Navigation properties
    public virtual User? ResolvedByUser { get; set; }
}

/// <summary>
/// Comprehensive audit logging for all system actions
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Create, Read, Update, Delete, Execute, etc.
    
    [MaxLength(100)]
    public string? ResourceType { get; set; }
    
    [MaxLength(255)]
    public string? ResourceId { get; set; }
    
    public Guid? UserId { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(20)]
    public string Result { get; set; } = string.Empty; // Success, Failure, Denied
    
    [MaxLength(1000)]
    public string? Details { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? BeforeState { get; set; } // State before the action (for updates/deletes)
    
    [Column(TypeName = "jsonb")]
    public string? AfterState { get; set; } // State after the action (for creates/updates)
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
}

#endregion