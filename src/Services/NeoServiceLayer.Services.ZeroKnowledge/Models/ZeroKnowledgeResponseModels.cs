using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Services.ZeroKnowledge.Models;

/// <summary>
/// Result model for zero-knowledge proof generation.
/// </summary>
public class ProofResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the generated proof.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof data as bytes.
    /// </summary>
    [Required]
    public byte[] ProofData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the proof as a string (for interface compatibility).
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public outputs from the proof generation.
    /// </summary>
    [Required]
    public Dictionary<string, object> PublicOutputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the public signals (for interface compatibility).
    /// </summary>
    public string[] PublicSignals { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether the proof generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if proof generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the proof was generated.
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata for the proof.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the verification key for this proof.
    /// </summary>
    public string VerificationKey { get; set; } = string.Empty;
}

/// <summary>
/// Result model for circuit compilation.
/// </summary>
public class CompileCircuitResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the compiled circuit.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the compilation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets compilation errors if any occurred.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets compilation warnings if any occurred.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets when the circuit was compiled.
    /// </summary>
    [Required]
    public DateTime CompiledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the verification key for the compiled circuit.
    /// </summary>
    public string VerificationKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proving key for the compiled circuit.
    /// </summary>
    public string ProvingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets compilation statistics.
    /// </summary>
    public Dictionary<string, object> Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the compiler identifier used.
    /// </summary>
    public string CompilerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the compiled circuit.
    /// </summary>
    public long CircuitSize { get; set; }

    /// <summary>
    /// Gets or sets the number of constraints in the circuit.
    /// </summary>
    public int ConstraintCount { get; set; }
}

/// <summary>
/// Result model for proof verification.
/// </summary>
public class VerifyProofResult
{
    /// <summary>
    /// Gets or sets the proof identifier that was verified.
    /// </summary>
    [StringLength(64)]
    public string? ProofId { get; set; }

    /// <summary>
    /// Gets or sets whether the proof is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets when the verification was performed.
    /// </summary>
    [Required]
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the verification time in milliseconds.
    /// </summary>
    public long VerificationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets verification details and metadata.
    /// </summary>
    public Dictionary<string, object> VerificationDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the verifier identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string VerifierIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error messages from verification.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result model for commitment creation.
/// </summary>
public class CreateCommitmentResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the commitment.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CommitmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commitment value.
    /// </summary>
    [Required]
    public string CommitmentValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blinding factor used (may be generated).
    /// </summary>
    [Required]
    public string BlindingFactor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the commitment was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the commitment expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the commitment scheme used.
    /// </summary>
    [Required]
    public CommitmentScheme Scheme { get; set; }

    /// <summary>
    /// Gets or sets the committer identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string CommitterIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional commitment metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for commitment revelation.
/// </summary>
public class RevealCommitmentResult
{
    /// <summary>
    /// Gets or sets the commitment identifier that was revealed.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CommitmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the revelation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the revealed value.
    /// </summary>
    public string RevealedValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the commitment was revealed.
    /// </summary>
    [Required]
    public DateTime RevealedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the revealer identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RevealerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets verification details.
    /// </summary>
    public Dictionary<string, object> VerificationDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets any error messages from revelation.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result model for Merkle tree creation.
/// </summary>
public class CreateMerkleTreeResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the Merkle tree.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string TreeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root hash of the tree.
    /// </summary>
    [Required]
    public string RootHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of leaves in the tree.
    /// </summary>
    public int LeafCount { get; set; }

    /// <summary>
    /// Gets or sets the tree depth.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets when the tree was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the hash function used.
    /// </summary>
    [Required]
    public HashFunction HashFunction { get; set; }

    /// <summary>
    /// Gets or sets the tree builder identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string BuilderIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets tree configuration options used.
    /// </summary>
    public MerkleTreeOptions Options { get; set; } = new();
}

/// <summary>
/// Result model for Merkle proof generation.
/// </summary>
public class GenerateMerkleProofResult
{
    /// <summary>
    /// Gets or sets the Merkle tree identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string TreeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the leaf index that was proven.
    /// </summary>
    public int LeafIndex { get; set; }

    /// <summary>
    /// Gets or sets the leaf value (if included).
    /// </summary>
    public string? LeafValue { get; set; }

    /// <summary>
    /// Gets or sets the Merkle proof path.
    /// </summary>
    [Required]
    public List<string> ProofPath { get; set; } = new();

    /// <summary>
    /// Gets or sets the root hash for verification.
    /// </summary>
    [Required]
    public string RootHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the proof was generated.
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the proof requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the proof was compressed.
    /// </summary>
    public bool IsCompressed { get; set; }
}

/// <summary>
/// Result model for range proof creation.
/// </summary>
public class CreateRangeProofResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the range proof.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the range proof data.
    /// </summary>
    [Required]
    public byte[] ProofData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the commitment to the proven value.
    /// </summary>
    [Required]
    public string ValueCommitment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the range specification that was proven.
    /// </summary>
    [Required]
    public RangeSpecification Range { get; set; } = new();

    /// <summary>
    /// Gets or sets when the proof was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the prover identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ProverIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional proof metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result model for bulletproof generation.
/// </summary>
public class GenerateBulletproofResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the bulletproof.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bulletproof data.
    /// </summary>
    [Required]
    public byte[] ProofData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the commitments to the proven values.
    /// </summary>
    [Required]
    public List<string> ValueCommitments { get; set; } = new();

    /// <summary>
    /// Gets or sets the ranges that were proven.
    /// </summary>
    [Required]
    public List<RangeSpecification> Ranges { get; set; } = new();

    /// <summary>
    /// Gets or sets when the proof was generated.
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the prover identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ProverIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bulletproof options used.
    /// </summary>
    public BulletproofOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof size in bytes.
    /// </summary>
    public int ProofSize { get; set; }
}

/// <summary>
/// Result model for batch proof verification.
/// </summary>
public class BatchVerifyProofsResult
{
    /// <summary>
    /// Gets or sets the overall batch verification result.
    /// </summary>
    public bool AllProofsValid { get; set; }

    /// <summary>
    /// Gets or sets individual verification results.
    /// </summary>
    [Required]
    public List<VerifyProofResult> IndividualResults { get; set; } = new();

    /// <summary>
    /// Gets or sets when the batch verification was performed.
    /// </summary>
    [Required]
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total verification time in milliseconds.
    /// </summary>
    public long TotalVerificationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of proofs that were valid.
    /// </summary>
    public int ValidProofCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of proofs verified.
    /// </summary>
    public int TotalProofCount { get; set; }

    /// <summary>
    /// Gets or sets the batch verifier identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string VerifierIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether parallel verification was used.
    /// </summary>
    public bool UsedParallelVerification { get; set; }
}

/// <summary>
/// Result model for zero-knowledge statistics.
/// </summary>
public class ZkStatisticsResult
{
    /// <summary>
    /// Gets or sets the time range for the statistics.
    /// </summary>
    public DateRange? TimeRange { get; set; }

    /// <summary>
    /// Gets or sets proof generation statistics.
    /// </summary>
    public ProofGenerationStatistics? ProofStats { get; set; }

    /// <summary>
    /// Gets or sets verification statistics.
    /// </summary>
    public VerificationStatistics? VerificationStats { get; set; }

    /// <summary>
    /// Gets or sets circuit compilation statistics.
    /// </summary>
    public CircuitCompilationStatistics? CircuitStats { get; set; }

    /// <summary>
    /// Gets or sets performance metrics.
    /// </summary>
    public PerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets when the statistics were generated.
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the statistics requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;
}

/// <summary>
/// Statistics for proof generation operations.
/// </summary>
public class ProofGenerationStatistics
{
    /// <summary>
    /// Gets or sets the total number of proofs generated.
    /// </summary>
    public int TotalProofs { get; set; }

    /// <summary>
    /// Gets or sets the number of successful proof generations.
    /// </summary>
    public int SuccessfulProofs { get; set; }

    /// <summary>
    /// Gets or sets the number of failed proof generations.
    /// </summary>
    public int FailedProofs { get; set; }

    /// <summary>
    /// Gets or sets the average proof generation time in milliseconds.
    /// </summary>
    public double AverageGenerationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets proof generation statistics by type.
    /// </summary>
    public Dictionary<ProofType, int> ProofsByType { get; set; } = new();
}

/// <summary>
/// Statistics for proof verification operations.
/// </summary>
public class VerificationStatistics
{
    /// <summary>
    /// Gets or sets the total number of verifications performed.
    /// </summary>
    public int TotalVerifications { get; set; }

    /// <summary>
    /// Gets or sets the number of valid proofs verified.
    /// </summary>
    public int ValidProofs { get; set; }

    /// <summary>
    /// Gets or sets the number of invalid proofs detected.
    /// </summary>
    public int InvalidProofs { get; set; }

    /// <summary>
    /// Gets or sets the average verification time in milliseconds.
    /// </summary>
    public double AverageVerificationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets verification statistics by type.
    /// </summary>
    public Dictionary<ProofType, int> VerificationsByType { get; set; } = new();
}

/// <summary>
/// Statistics for circuit compilation operations.
/// </summary>
public class CircuitCompilationStatistics
{
    /// <summary>
    /// Gets or sets the total number of circuits compiled.
    /// </summary>
    public int TotalCompilations { get; set; }

    /// <summary>
    /// Gets or sets the number of successful compilations.
    /// </summary>
    public int SuccessfulCompilations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed compilations.
    /// </summary>
    public int FailedCompilations { get; set; }

    /// <summary>
    /// Gets or sets the average compilation time in milliseconds.
    /// </summary>
    public double AverageCompilationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets compilation statistics by language.
    /// </summary>
    public Dictionary<CircuitLanguage, int> CompilationsByLanguage { get; set; } = new();
}

/// <summary>
/// Performance metrics for zero-knowledge operations.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Gets or sets memory usage statistics.
    /// </summary>
    public MemoryUsageMetrics MemoryUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets CPU usage statistics.
    /// </summary>
    public CpuUsageMetrics CpuUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets throughput metrics.
    /// </summary>
    public ThroughputMetrics Throughput { get; set; } = new();
}

/// <summary>
/// Memory usage metrics.
/// </summary>
public class MemoryUsageMetrics
{
    /// <summary>
    /// Gets or sets the average memory usage in bytes.
    /// </summary>
    public long AverageMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in bytes.
    /// </summary>
    public long PeakMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the minimum memory usage in bytes.
    /// </summary>
    public long MinMemoryUsage { get; set; }
}

/// <summary>
/// CPU usage metrics.
/// </summary>
public class CpuUsageMetrics
{
    /// <summary>
    /// Gets or sets the average CPU usage percentage.
    /// </summary>
    public double AverageCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the peak CPU usage percentage.
    /// </summary>
    public double PeakCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the minimum CPU usage percentage.
    /// </summary>
    public double MinCpuUsage { get; set; }
}

/// <summary>
/// Throughput metrics.
/// </summary>
public class ThroughputMetrics
{
    /// <summary>
    /// Gets or sets the proofs generated per second.
    /// </summary>
    public double ProofsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the verifications performed per second.
    /// </summary>
    public double VerificationsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the circuits compiled per second.
    /// </summary>
    public double CompilationsPerSecond { get; set; }
}