using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region SGX Confidential Computing Entities

/// <summary>
/// Represents sealed data items stored in SGX confidential storage
/// Unified storage for all services with confidential data requirements
/// </summary>
public class SealedDataItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(64)]
    public string StorageId { get; set; } = string.Empty;
    
    [Required]
    public byte[] SealedData { get; set; } = Array.Empty<byte>();
    
    public int OriginalSize { get; set; }
    
    public int SealedSize { get; set; }
    
    [MaxLength(255)]
    public string Fingerprint { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string PolicyType { get; set; } = string.Empty; // MRSIGNER, MRENCLAVE, etc.
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? LastAccessed { get; set; }
    
    public int AccessCount { get; set; } = 0;
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; } // JSON metadata
    
    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    
    public bool IsActive => !IsExpired;
    
    public string Service => ServiceName;
    
    // Navigation properties
    public virtual SealingPolicy? Policy { get; set; }
    public virtual ICollection<EnclaveAttestation> Attestations { get; set; } = new List<EnclaveAttestation>();
}

/// <summary>
/// Enclave attestation records for verifying SGX environment integrity
/// </summary>
public class EnclaveAttestation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? SealedDataItemId { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string AttestationId { get; set; } = string.Empty;
    
    [Required]
    public byte[] Quote { get; set; } = Array.Empty<byte>();
    
    [Required]
    public byte[] Report { get; set; } = Array.Empty<byte>();
    
    [MaxLength(64)]
    public string MRENCLAVE { get; set; } = string.Empty;
    
    [MaxLength(64)]
    public string MRSIGNER { get; set; } = string.Empty;
    
    public int ISVProdID { get; set; }
    
    public int ISVSvn { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Valid, Invalid, Expired
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? VerifiedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    [MaxLength(1000)]
    public string? VerificationResult { get; set; }
    
    // Navigation properties
    public virtual SealedDataItem? SealedDataItem { get; set; }
}

/// <summary>
/// Sealing policies for controlling data access and lifecycle
/// </summary>
public class SealingPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string PolicyType { get; set; } = string.Empty; // MRSIGNER, MRENCLAVE, CUSTOM
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public int ExpirationHours { get; set; } = 24;
    
    public bool AllowUnseal { get; set; } = true;
    
    public bool RequireAttestation { get; set; } = true;
    
    [Column(TypeName = "jsonb")]
    public string? PolicyRules { get; set; } // JSON policy rules
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<SealedDataItem> SealedDataItems { get; set; } = new List<SealedDataItem>();
}

#endregion