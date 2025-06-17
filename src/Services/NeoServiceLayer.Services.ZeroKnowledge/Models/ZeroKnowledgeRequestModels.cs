using System.ComponentModel.DataAnnotations;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ZeroKnowledge.Models;

/// <summary>
/// Request model for generating zero-knowledge proofs.
/// </summary>
public class GenerateProofRequest
{
    /// <summary>
    /// Gets or sets the circuit identifier to use for proof generation.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the private witness inputs for the proof.
    /// </summary>
    [Required]
    public Dictionary<string, string> PrivateInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the public inputs for the proof.
    /// </summary>
    [Required]
    public Dictionary<string, string> PublicInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof type to generate.
    /// </summary>
    [Required]
    public ProofType ProofType { get; set; }

    /// <summary>
    /// Gets or sets the prover identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ProverIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata for the proof.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the nonce for randomness.
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the commitment blinding factor.
    /// </summary>
    public string? BlindingFactor { get; set; }
}

/// <summary>
/// Request model for verifying zero-knowledge proofs.
/// </summary>
public class VerifyProofRequest
{
    /// <summary>
    /// Gets or sets the proof identifier.
    /// </summary>
    [StringLength(64)]
    public string? ProofId { get; set; }

    /// <summary>
    /// Gets or sets the proof data to verify.
    /// </summary>
    [Required]
    public string ProofData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit identifier used for the proof.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public inputs for verification.
    /// </summary>
    [Required]
    public Dictionary<string, string> PublicInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof type.
    /// </summary>
    [Required]
    public ProofType ProofType { get; set; }

    /// <summary>
    /// Gets or sets the verifier identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string VerifierIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to perform fast verification (if available).
    /// </summary>
    public bool FastVerification { get; set; } = false;
}

/// <summary>
/// Request model for compiling circuits.
/// </summary>
public class CompileCircuitRequest
{
    /// <summary>
    /// Gets or sets the circuit name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CircuitName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit source code.
    /// </summary>
    [Required]
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit language.
    /// </summary>
    [Required]
    public CircuitLanguage Language { get; set; }

    /// <summary>
    /// Gets or sets the compilation target.
    /// </summary>
    [Required]
    public CompilationTarget Target { get; set; }

    /// <summary>
    /// Gets or sets compilation options.
    /// </summary>
    public CompilationOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the compiler identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string CompilerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optimization level.
    /// </summary>
    [Range(0, 3)]
    public int OptimizationLevel { get; set; } = 1;
}

/// <summary>
/// Request model for creating commitments.
/// </summary>
public class CreateCommitmentRequest
{
    /// <summary>
    /// Gets or sets the value to commit to.
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commitment scheme type.
    /// </summary>
    [Required]
    public CommitmentScheme Scheme { get; set; }

    /// <summary>
    /// Gets or sets the blinding factor (optional, will be generated if not provided).
    /// </summary>
    public string? BlindingFactor { get; set; }

    /// <summary>
    /// Gets or sets the committer identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string CommitterIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional commitment parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the commitment expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for revealing commitments.
/// </summary>
public class RevealCommitmentRequest
{
    /// <summary>
    /// Gets or sets the commitment identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CommitmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original value.
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blinding factor used in commitment.
    /// </summary>
    [Required]
    public string BlindingFactor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revealer identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RevealerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional verification data.
    /// </summary>
    public Dictionary<string, string> VerificationData { get; set; } = new();
}

/// <summary>
/// Request model for creating Merkle trees.
/// </summary>
public class CreateMerkleTreeRequest
{
    /// <summary>
    /// Gets or sets the tree name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TreeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the leaf values for the tree.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> LeafValues { get; set; } = new();

    /// <summary>
    /// Gets or sets the hash function to use.
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
    /// Gets or sets tree configuration options.
    /// </summary>
    public MerkleTreeOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable proof generation optimization.
    /// </summary>
    public bool OptimizeForProofs { get; set; } = true;
}

/// <summary>
/// Request model for generating Merkle proofs.
/// </summary>
public class GenerateMerkleProofRequest
{
    /// <summary>
    /// Gets or sets the Merkle tree identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string TreeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the leaf index to prove.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int LeafIndex { get; set; }

    /// <summary>
    /// Gets or sets the leaf value to prove (optional verification).
    /// </summary>
    public string? LeafValue { get; set; }

    /// <summary>
    /// Gets or sets the proof requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include the leaf value in the proof.
    /// </summary>
    public bool IncludeLeafValue { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to compress the proof.
    /// </summary>
    public bool CompressProof { get; set; } = false;
}

/// <summary>
/// Request model for creating range proofs.
/// </summary>
public class CreateRangeProofRequest
{
    /// <summary>
    /// Gets or sets the value to prove is in range.
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum value of the range.
    /// </summary>
    [Required]
    public string MinValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum value of the range.
    /// </summary>
    [Required]
    public string MaxValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bit length for the range proof.
    /// </summary>
    [Range(8, 256)]
    public int BitLength { get; set; } = 64;

    /// <summary>
    /// Gets or sets the blinding factor.
    /// </summary>
    public string? BlindingFactor { get; set; }

    /// <summary>
    /// Gets or sets the prover identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ProverIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional proof parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Request model for generating bulletproofs.
/// </summary>
public class GenerateBulletproofRequest
{
    /// <summary>
    /// Gets or sets the values to prove (for batch proofs).
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// Gets or sets the range specifications for each value.
    /// </summary>
    [Required]
    public List<RangeSpecification> Ranges { get; set; } = new();

    /// <summary>
    /// Gets or sets the blinding factors for each value.
    /// </summary>
    public List<string>? BlindingFactors { get; set; }

    /// <summary>
    /// Gets or sets the bit length for the bulletproof.
    /// </summary>
    [Range(8, 64)]
    public int BitLength { get; set; } = 32;

    /// <summary>
    /// Gets or sets the prover identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string ProverIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets bulletproof generation options.
    /// </summary>
    public BulletproofOptions Options { get; set; } = new();
}

/// <summary>
/// Request model for getting proof information.
/// </summary>
public class GetProofRequest
{
    /// <summary>
    /// Gets or sets the proof identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include proof data.
    /// </summary>
    public bool IncludeProofData { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include verification details.
    /// </summary>
    public bool IncludeVerificationDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;
}

/// <summary>
/// Request model for getting circuit information.
/// </summary>
public class GetCircuitRequest
{
    /// <summary>
    /// Gets or sets the circuit identifier.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include the source code.
    /// </summary>
    public bool IncludeSourceCode { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include compilation details.
    /// </summary>
    public bool IncludeCompilationDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;
}

/// <summary>
/// Request model for batch verifying proofs.
/// </summary>
public class BatchVerifyProofsRequest
{
    /// <summary>
    /// Gets or sets the list of proof verification requests.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<VerifyProofRequest> ProofRequests { get; set; } = new();

    /// <summary>
    /// Gets or sets the batch verifier identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string VerifierIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use parallel verification.
    /// </summary>
    public bool UseParallelVerification { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to fail fast on first verification failure.
    /// </summary>
    public bool FailFast { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum verification time per proof in milliseconds.
    /// </summary>
    [Range(1000, 300000)]
    public int MaxVerificationTimeMs { get; set; } = 30000;
}

/// <summary>
/// Request model for zero-knowledge statistics.
/// </summary>
public class ZkStatisticsRequest
{
    /// <summary>
    /// Gets or sets the time range for statistics.
    /// </summary>
    public DateRange? TimeRange { get; set; }

    /// <summary>
    /// Gets or sets whether to include proof generation statistics.
    /// </summary>
    public bool IncludeProofStats { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include verification statistics.
    /// </summary>
    public bool IncludeVerificationStats { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include circuit compilation statistics.
    /// </summary>
    public bool IncludeCircuitStats { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include performance metrics.
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets the statistics requester identifier.
    /// </summary>
    [Required]
    [StringLength(66)]
    public string RequesterIdentifier { get; set; } = string.Empty;
} 