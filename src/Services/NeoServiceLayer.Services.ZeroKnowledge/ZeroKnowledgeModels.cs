using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ZeroKnowledge;

/// <summary>
/// Represents a zero-knowledge circuit definition.
/// </summary>
public class ZkCircuitDefinition
{
    /// <summary>
    /// Gets or sets the circuit name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit type.
    /// </summary>
    public ZkCircuitType Type { get; set; }

    /// <summary>
    /// Gets or sets the input schema.
    /// </summary>
    public Dictionary<string, object> InputSchema { get; set; } = new();

    /// <summary>
    /// Gets or sets the output schema.
    /// </summary>
    public Dictionary<string, object> OutputSchema { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit constraints.
    /// </summary>
    public string[] Constraints { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the circuit metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a zero-knowledge proof request.
/// </summary>
public class ZkProofRequest
{
    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof inputs.
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof witnesses.
    /// </summary>
    public Dictionary<string, object> Witnesses { get; set; } = new();

    /// <summary>
    /// Gets or sets the proof system to use.
    /// </summary>
    public string ProofSystem { get; set; } = "groth16";

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a zero-knowledge proof result.
/// </summary>
public class ZkProofResult
{
    /// <summary>
    /// Gets or sets the proof ID.
    /// </summary>
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the proof generation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    public string ProofData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public inputs.
    /// </summary>
    public string[] PublicInputs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a compiled zero-knowledge circuit.
/// </summary>
public class ZkCircuit
{
    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit type.
    /// </summary>
    public ZkCircuitType Type { get; set; }

    /// <summary>
    /// Gets or sets the compiled circuit data.
    /// </summary>
    public byte[] CompiledData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the verification key.
    /// </summary>
    public string VerificationKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proving key.
    /// </summary>
    public string ProvingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the circuit is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the compilation timestamp.
    /// </summary>
    public DateTime CompiledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the blockchain type this circuit supports.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
/// Represents a zero-knowledge proof.
/// </summary>
public class ZkProof
{
    /// <summary>
    /// Gets or sets the proof ID.
    /// </summary>
    public string ProofId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit ID.
    /// </summary>
    public string CircuitId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proof data.
    /// </summary>
    public string ProofData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public inputs.
    /// </summary>
    public string[] PublicInputs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether the proof is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the type of zero-knowledge circuit.
/// </summary>
public enum ZkCircuitType
{
    /// <summary>
    /// Membership proof circuit.
    /// </summary>
    Membership,

    /// <summary>
    /// Range proof circuit.
    /// </summary>
    Range,

    /// <summary>
    /// Comparison circuit.
    /// </summary>
    Comparison,

    /// <summary>
    /// Identity verification circuit.
    /// </summary>
    Identity,

    /// <summary>
    /// Computation verification circuit.
    /// </summary>
    Computation,

    /// <summary>
    /// Custom circuit.
    /// </summary>
    Custom
}
