using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL
{
    /// <summary>
    /// Entity representing a registered computation
    /// </summary>
    public class ComputationEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ComputationType { get; set; } = string.Empty; // JavaScript, WebAssembly, Python
        
        [Required]
        public string Code { get; set; } = string.Empty; // The actual computation code
        
        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = "1.0.0";
        
        [MaxLength(255)]
        public string? Author { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string BlockchainType { get; set; } = string.Empty; // NeoN3, NeoX
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; } // Additional metadata as JSON
        
        // Navigation properties
        public virtual ICollection<ComputationStatusEntity> Statuses { get; set; } = new List<ComputationStatusEntity>();
        public virtual ICollection<ComputationResultEntity> Results { get; set; } = new List<ComputationResultEntity>();
    }

    /// <summary>
    /// Entity representing the status of a computation execution
    /// </summary>
    public class ComputationStatusEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ComputationId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Pending, InProgress, Completed, Failed
        
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndTime { get; set; }
        
        [MaxLength(50)]
        public string? BlockchainType { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? Parameters { get; set; } // Execution parameters as JSON
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ComputationEntity? Computation { get; set; }
        public virtual ICollection<ComputationResultEntity> Results { get; set; } = new List<ComputationResultEntity>();
    }

    /// <summary>
    /// Entity representing the result of a computation execution
    /// </summary>
    public class ComputationResultEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ComputationId { get; set; } = string.Empty;
        
        public Guid? StatusId { get; set; }
        
        [Required]
        [Column(TypeName = "jsonb")]
        public string Result { get; set; } = "{}"; // Computation result as JSON
        
        [Required]
        [MaxLength(256)]
        public string Hash { get; set; } = string.Empty; // Hash of the result for verification
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool Success { get; set; } = true;
        
        [MaxLength(1000)]
        public string? ErrorDetails { get; set; }
        
        // Optional SGX attestation data
        [MaxLength(64)]
        public string? AttestationId { get; set; }
        
        public byte[]? EnclaveQuote { get; set; }
        
        [MaxLength(64)]
        public string? MRENCLAVE { get; set; }
        
        [MaxLength(64)]
        public string? MRSIGNER { get; set; }
        
        // Navigation properties
        public virtual ComputationEntity? Computation { get; set; }
        public virtual ComputationStatusEntity? Status { get; set; }
    }

    /// <summary>
    /// Entity for tracking computation resource usage
    /// </summary>
    public class ComputationResourceUsageEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ComputationId { get; set; } = string.Empty;
        
        public Guid? ExecutionId { get; set; } // Links to ComputationStatusEntity
        
        public long CpuTimeMs { get; set; } // CPU time in milliseconds
        
        public long MemoryUsedBytes { get; set; } // Memory used in bytes
        
        public long NetworkBytesIn { get; set; }
        
        public long NetworkBytesOut { get; set; }
        
        public int GasUsed { get; set; } // For blockchain operations
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        
        [Column(TypeName = "jsonb")]
        public string? AdditionalMetrics { get; set; } // Additional metrics as JSON
    }

    /// <summary>
    /// Entity for computation access control and permissions
    /// </summary>
    public class ComputationPermissionEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ComputationId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string Principal { get; set; } = string.Empty; // User, address, or role
        
        [Required]
        [MaxLength(50)]
        public string Permission { get; set; } = string.Empty; // Execute, Read, Write, Delete
        
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        [MaxLength(255)]
        public string? GrantedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [Column(TypeName = "jsonb")]
        public string? Conditions { get; set; } // Additional conditions as JSON
    }
}