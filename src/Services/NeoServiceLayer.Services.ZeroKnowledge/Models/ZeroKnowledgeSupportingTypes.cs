using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ZeroKnowledge.Models;

/// <summary>
/// Represents a date range for filtering.
/// </summary>
public class DateRange
{
    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets whether the range is valid.
    /// </summary>
    public bool IsValid => StartDate <= EndDate;
}

/// <summary>
/// Compilation options for circuits.
/// </summary>
public class CompilationOptions
{
    /// <summary>
    /// Gets or sets whether to enable debug information.
    /// </summary>
    public bool EnableDebug { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to optimize for size.
    /// </summary>
    public bool OptimizeSize { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable security checks.
    /// </summary>
    public bool EnableSecurityChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets additional compiler flags.
    /// </summary>
    public Dictionary<string, string> CompilerFlags { get; set; } = new();
}

/// <summary>
/// Merkle tree configuration options.
/// </summary>
public class MerkleTreeOptions
{
    /// <summary>
    /// Gets or sets whether to store intermediate nodes.
    /// </summary>
    public bool StoreIntermediateNodes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable batch updates.
    /// </summary>
    public bool EnableBatchUpdates { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum tree depth.
    /// </summary>
    public int MaxDepth { get; set; } = 32;
}

/// <summary>
/// Range specification for proofs.
/// </summary>
public class RangeSpecification
{
    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public string MinValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public string MaxValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bit length.
    /// </summary>
    public int BitLength { get; set; } = 64;
}

/// <summary>
/// Bulletproof generation options.
/// </summary>
public class BulletproofOptions
{
    /// <summary>
    /// Gets or sets whether to use aggregated proofs.
    /// </summary>
    public bool UseAggregatedProofs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to compress the proof.
    /// </summary>
    public bool CompressProof { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of rounds for optimization.
    /// </summary>
    public int OptimizationRounds { get; set; } = 3;
}

// Enums

/// <summary>
/// Proof type enumeration.
/// </summary>
public enum ProofType
{
    /// <summary>
    /// Generic zero-knowledge proof.
    /// </summary>
    Generic,

    /// <summary>
    /// zk-SNARK proof.
    /// </summary>
    ZkSnark,

    /// <summary>
    /// zk-STARK proof.
    /// </summary>
    ZkStark,

    /// <summary>
    /// Bulletproof.
    /// </summary>
    Bulletproof,

    /// <summary>
    /// Range proof.
    /// </summary>
    RangeProof,

    /// <summary>
    /// Merkle proof.
    /// </summary>
    MerkleProof,

    /// <summary>
    /// Commitment proof.
    /// </summary>
    CommitmentProof
}

/// <summary>
/// Circuit language enumeration.
/// </summary>
public enum CircuitLanguage
{
    /// <summary>
    /// Circom circuit language.
    /// </summary>
    Circom,

    /// <summary>
    /// ZoKrates language.
    /// </summary>
    ZoKrates,

    /// <summary>
    /// Cairo language.
    /// </summary>
    Cairo,

    /// <summary>
    /// Noir language.
    /// </summary>
    Noir,

    /// <summary>
    /// Custom assembly.
    /// </summary>
    Assembly
}

/// <summary>
/// Compilation target enumeration.
/// </summary>
public enum CompilationTarget
{
    /// <summary>
    /// Generic R1CS target.
    /// </summary>
    R1CS,

    /// <summary>
    /// Groth16 target.
    /// </summary>
    Groth16,

    /// <summary>
    /// PLONK target.
    /// </summary>
    Plonk,

    /// <summary>
    /// Marlin target.
    /// </summary>
    Marlin,

    /// <summary>
    /// STARK target.
    /// </summary>
    Stark,

    /// <summary>
    /// Custom target.
    /// </summary>
    Custom
}

/// <summary>
/// Commitment scheme enumeration.
/// </summary>
public enum CommitmentScheme
{
    /// <summary>
    /// Pedersen commitment.
    /// </summary>
    Pedersen,

    /// <summary>
    /// SHA256 hash commitment.
    /// </summary>
    SHA256,

    /// <summary>
    /// Blake2b hash commitment.
    /// </summary>
    Blake2b,

    /// <summary>
    /// Poseidon hash commitment.
    /// </summary>
    Poseidon,

    /// <summary>
    /// KZG commitment.
    /// </summary>
    KZG
}

/// <summary>
/// Hash function enumeration.
/// </summary>
public enum HashFunction
{
    /// <summary>
    /// SHA256 hash function.
    /// </summary>
    SHA256,

    /// <summary>
    /// Blake2b hash function.
    /// </summary>
    Blake2b,

    /// <summary>
    /// Keccak256 hash function.
    /// </summary>
    Keccak256,

    /// <summary>
    /// Poseidon hash function.
    /// </summary>
    Poseidon,

    /// <summary>
    /// MiMC hash function.
    /// </summary>
    MiMC
}
