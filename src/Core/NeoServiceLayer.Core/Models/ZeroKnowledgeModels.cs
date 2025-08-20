using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;


namespace NeoServiceLayer.Core.Models;

#region Zero Knowledge Models

/// <summary>
/// Represents a zero-knowledge proof request.
/// </summary>
public class ProofRequest
{
    /// <summary>
    /// Gets or sets the circuit ID to use for proof generation.
    /// </summary>
    [Required]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the private inputs for the proof.
    /// </summary>
    [Required]
    public Dictionary<string, object> PrivateInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the public inputs for the proof.
    /// </summary>
    public Dictionary<string, object> PublicInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof type.
    /// </summary>
    public ProofType ProofType { get; set; } = ProofType.SNARK;
    
    /// <summary>
    /// Gets or sets the proof system (alias for ProofType).
    /// </summary>
    public ProofType ProofSystem 
    { 
        get => ProofType;
        set => ProofType = value;
    }
}

/// <summary>
/// Represents a zero-knowledge proof result.
/// </summary>
public class ProofResult
{
    /// <summary>
    /// Gets or sets the proof ID.
    /// </summary>
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated proof data.
    /// </summary>
    public byte[] ProofData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the public outputs.
    /// </summary>
    public Dictionary<string, object> PublicOutputs { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the proof generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the proof generation time in milliseconds.
    /// </summary>
    public long GenerationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets when the proof was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a proof verification request.
/// </summary>
public class ProofVerification
{
    /// <summary>
    /// Gets or sets the proof data to verify.
    /// </summary>
    [Required]
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public signals for verification.
    /// </summary>
    public Dictionary<string, object> PublicSignals { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof data to verify.
    /// </summary>
    [Required]
    public byte[] ProofData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the public inputs for verification.
    /// </summary>
    [Required]
    public Dictionary<string, object> PublicInputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit ID used for the proof.
    /// </summary>
    [Required]
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents a ZK circuit definition.
/// </summary>
public class ZkCircuitDefinition
{
    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit code.
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the circuit parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets when the circuit was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the circuit is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a compiled ZK circuit.
/// </summary>
public class ZkCircuit
{
    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the compiled circuit code.
    /// </summary>
    public byte[] CompiledCode { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the input schema.
    /// </summary>
    public Dictionary<string, object> InputSchema { get; set; } = new();

    /// <summary>
    /// Gets or sets the output schema.
    /// </summary>
    public Dictionary<string, object> OutputSchema { get; set; } = new();

    /// <summary>
    /// Gets or sets when the circuit was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the circuit is compiled.
    /// </summary>
    public bool IsCompiled { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a ZK computation result.
/// </summary>
public class ZkComputationResult
{
    /// <summary>
    /// Gets or sets the computation ID.
    /// </summary>
    public string ComputationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the computation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the computation result.
    /// </summary>
    public Dictionary<string, object> Result { get; set; } = new();

    /// <summary>
    /// Gets or sets the computation proof.
    /// </summary>
    public byte[] Proof { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets when the computation was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the computation type.
    /// </summary>
    public ComputationType ComputationType { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents cryptographic key information.
/// </summary>
public class CryptoKeyInfo
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the private key data.
    /// </summary>
    public byte[] PrivateKey { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the public key data.
    /// </summary>
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the key type.
    /// </summary>
    public CryptoKeyType KeyType { get; set; }

    /// <summary>
    /// Gets or sets whether the key is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the key metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the key algorithm.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the key size.
    /// </summary>
    public int KeySize { get; set; }
    
    /// <summary>
    /// Gets or sets the key purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when the key expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Represents proof types.
/// </summary>
public enum ProofType
{
    /// <summary>
    /// Succinct Non-Interactive Argument of Knowledge.
    /// </summary>
    SNARK,

    /// <summary>
    /// Scalable Transparent Argument of Knowledge.
    /// </summary>
    STARK,

    /// <summary>
    /// Bulletproofs.
    /// </summary>
    Bulletproof,

    /// <summary>
    /// PLONK proof system.
    /// </summary>
    PLONK,

    /// <summary>
    /// Groth16 proof system.
    /// </summary>
    Groth16
}

/// <summary>
/// Represents computation types.
/// </summary>
public enum ComputationType
{
    /// <summary>
    /// Private computation.
    /// </summary>
    Private,

    /// <summary>
    /// Verifiable computation.
    /// </summary>
    Verifiable,

    /// <summary>
    /// Multi-party computation.
    /// </summary>
    MultiParty,

    /// <summary>
    /// Secure computation.
    /// </summary>
    Secure
}

/// <summary>
/// Represents cryptographic key types.
/// </summary>
public enum CryptoKeyType
{
    /// <summary>
    /// RSA key.
    /// </summary>
    RSA,

    /// <summary>
    /// Elliptic Curve key.
    /// </summary>
    ECC,

    /// <summary>
    /// EdDSA key.
    /// </summary>
    EdDSA,

    /// <summary>
    /// BLS key.
    /// </summary>
    BLS,

    /// <summary>
    /// Symmetric key.
    /// </summary>
    Symmetric
}

#endregion
