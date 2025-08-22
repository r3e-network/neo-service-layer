using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Entities.OracleEntities;

namespace NeoServiceLayer.Infrastructure.Persistence.PostgreSQL;

#region Service-Specific Entities

#region Key Management

/// <summary>
/// Cryptographic keys managed by the Key Management service
/// </summary>
public class CryptographicKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string KeyId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string KeyType { get; set; } = string.Empty; // RSA, ECDSA, AES, etc.
    
    public int KeySize { get; set; }
    
    [Required]
    public byte[] EncryptedKeyMaterial { get; set; } = Array.Empty<byte>();
    
    [MaxLength(255)]
    public string? Purpose { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Revoked, Expired, Pending
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? LastUsedAt { get; set; }
    
    public int UsageCount { get; set; } = 0;
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<KeyRotationEvent> RotationEvents { get; set; } = new List<KeyRotationEvent>();
    public virtual ICollection<KeyAccessAudit> AccessAudits { get; set; } = new List<KeyAccessAudit>();
}

/// <summary>
/// Key rotation events for tracking key lifecycle
/// </summary>
public class KeyRotationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid KeyId { get; set; }
    
    [MaxLength(100)]
    public string? OldKeyId { get; set; }
    
    [MaxLength(100)]
    public string? NewKeyId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string RotationType { get; set; } = string.Empty; // Scheduled, Emergency, Manual
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public DateTime RotatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? RotatedBy { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Pending, Completed, Failed
    
    // Navigation properties
    public virtual CryptographicKey Key { get; set; } = null!;
    public virtual User? RotatedByUser { get; set; }
}

/// <summary>
/// Audit log for key access events
/// </summary>
public class KeyAccessAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid KeyId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AccessType { get; set; } = string.Empty; // Read, Use, Export, etc.
    
    [MaxLength(100)]
    public string? ServiceName { get; set; }
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(20)]
    public string Result { get; set; } = "Success"; // Success, Failure, Denied
    
    [MaxLength(500)]
    public string? Details { get; set; }
    
    // Navigation properties
    public virtual CryptographicKey Key { get; set; } = null!;
}

#endregion

#region Oracle Services

/// <summary>
/// Service-oriented Oracle data feeds configuration and metadata
/// Note: This is different from OracleEntities.OracleDataFeed which is domain-specific
/// </summary>
public class ServiceOracleDataFeed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string FeedId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string DataSource { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty; // Price, Weather, Random, etc.
    
    public int UpdateFrequency { get; set; } // in seconds
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? LastValue { get; set; }
    
    public DateTime? LastUpdated { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "jsonb")]
    public string? Configuration { get; set; }
    
    // Navigation properties
    public virtual ICollection<OracleRequest> Requests { get; set; } = new List<OracleRequest>();
    public virtual ICollection<DataSourceAttestation> Attestations { get; set; } = new List<DataSourceAttestation>();
}

/// <summary>
/// Oracle data requests from external services
/// </summary>
public class OracleRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid DataFeedId { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string RequestId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string RequesterService { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Parameters { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    
    public int Priority { get; set; } = 1; // 1 = Highest, 5 = Lowest
    
    // Navigation properties
    public virtual OracleDataFeed DataFeed { get; set; } = null!;
    public virtual OracleResponse? Response { get; set; }
}

/// <summary>
/// Oracle responses with attested data
/// </summary>
public class OracleResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid RequestId { get; set; }
    
    [Required]
    public byte[] ResponseData { get; set; } = Array.Empty<byte>();
    
    [MaxLength(255)]
    public string? DataSignature { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? NumericValue { get; set; }
    
    [MaxLength(1000)]
    public string? TextValue { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Valid";
    
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
    
    // Navigation properties
    public virtual OracleRequest Request { get; set; } = null!;
}

/// <summary>
/// Data source attestations for oracle integrity
/// </summary>
public class DataSourceAttestation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid DataFeedId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string AttestationHash { get; set; } = string.Empty;
    
    [Required]
    public byte[] AttestationData { get; set; } = Array.Empty<byte>();
    
    [MaxLength(255)]
    public string? AttesterIdentity { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? VerifiedAt { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    
    [MaxLength(500)]
    public string? VerificationResult { get; set; }
    
    // Navigation properties
    public virtual OracleDataFeed DataFeed { get; set; } = null!;
}

#endregion

#region Voting & Governance

/// <summary>
/// Governance proposals for decentralized voting
/// </summary>
public class Proposal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public Guid ProposerId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ProposalType { get; set; } = string.Empty; // Parameter, Upgrade, Election, etc.
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Active, Passed, Rejected, Executed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime VotingStartTime { get; set; }
    
    public DateTime VotingDeadline { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal QuorumThreshold { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal PassingThreshold { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? ProposalData { get; set; }
    
    // Voting results
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalVotesFor { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalVotesAgainst { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalVotingPower { get; set; }
    
    // Navigation properties
    public virtual User Proposer { get; set; } = null!;
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

/// <summary>
/// Individual votes cast on proposals
/// </summary>
public class Vote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ProposalId { get; set; }
    
    public Guid VoterId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string VoteChoice { get; set; } = string.Empty; // For, Against, Abstain
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal VotingPower { get; set; }
    
    public DateTime CastAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(255)]
    public string? VoteSignature { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    // Navigation properties
    public virtual Proposal Proposal { get; set; } = null!;
    public virtual User Voter { get; set; } = null!;
}

/// <summary>
/// Voting power calculations for users
/// </summary>
public class VotingPower
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Power { get; set; }
    
    [MaxLength(50)]
    public string PowerType { get; set; } = string.Empty; // Token, Delegated, Reputation, etc.
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? CalculationDetails { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}

#endregion

#region Cross-Chain & Bridge

/// <summary>
/// Cross-chain transactions for interoperability
/// </summary>
public class CrossChainTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(64)]
    public string TransactionHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string SourceChain { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string DestinationChain { get; set; } = string.Empty;
    
    [MaxLength(64)]
    public string? SourceTransactionHash { get; set; }
    
    [MaxLength(64)]
    public string? DestinationTransactionHash { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Amount { get; set; }
    
    [MaxLength(10)]
    public string? Asset { get; set; }
    
    [MaxLength(42)]
    public string? FromAddress { get; set; }
    
    [MaxLength(42)]
    public string? ToAddress { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Failed, Completed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public int ConfirmationCount { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? TransactionData { get; set; }
}

/// <summary>
/// Bridge operations for cross-chain asset transfers
/// </summary>
public class BridgeOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? CrossChainTransactionId { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string OperationId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string OperationType { get; set; } = string.Empty; // Lock, Unlock, Mint, Burn
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Amount { get; set; }
    
    [MaxLength(10)]
    public string Asset { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExecutedAt { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public virtual CrossChainTransaction? CrossChainTransaction { get; set; }
}

/// <summary>
/// Chain state tracking for cross-chain operations
/// </summary>
public class ChainState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string ChainName { get; set; } = string.Empty;
    
    public long BlockHeight { get; set; }
    
    [MaxLength(64)]
    public string? BlockHash { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Syncing, Offline
    
    [Column(TypeName = "jsonb")]
    public string? ChainInfo { get; set; }
}

#endregion

#endregion