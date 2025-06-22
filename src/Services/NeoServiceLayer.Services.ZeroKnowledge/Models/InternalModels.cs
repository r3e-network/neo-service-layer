namespace NeoServiceLayer.Services.ZeroKnowledge.Models;

/// <summary>
/// Parsed circuit representation.
/// </summary>
internal class ParsedCircuit
{
    public string Name { get; set; } = string.Empty;
    public List<CircuitConstraint> Constraints { get; set; } = new();
    public List<CircuitVariable> Variables { get; set; } = new();
    public List<string> PublicInputs { get; set; } = new();
    public List<string> PrivateInputs { get; set; } = new();
}

/// <summary>
/// Circuit constraint definition.
/// </summary>
internal class CircuitConstraint
{
    public string Expression { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = new();
}

/// <summary>
/// Circuit variable definition.
/// </summary>
internal class CircuitVariable
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsPrivate { get; set; }
}

/// <summary>
/// R1CS (Rank-1 Constraint System) representation.
/// </summary>
internal class R1CS
{
    public int NumVariables { get; set; }
    public int NumConstraints { get; set; }
    public double[][] A { get; set; } = Array.Empty<double[]>();
    public double[][] B { get; set; } = Array.Empty<double[]>();
    public double[][] C { get; set; } = Array.Empty<double[]>();
}

/// <summary>
/// Proving keys for zero-knowledge proofs.
/// </summary>
internal class ProvingKeys
{
    public byte[] ProvingKey { get; set; } = Array.Empty<byte>();
    public byte[] VerifyingKey { get; set; } = Array.Empty<byte>();
    public string CircuitHash { get; set; } = string.Empty;
}

/// <summary>
/// Compiled circuit data.
/// </summary>
internal class CompiledCircuit
{
    public ZkCircuitDefinition Definition { get; set; } = new();
    public R1CS R1CS { get; set; } = new();
    public ProvingKeys Keys { get; set; } = new();
    public DateTime CompiledAt { get; set; }
}

/// <summary>
/// Zero-knowledge witness.
/// </summary>
internal class ZkWitness
{
    public string CircuitId { get; set; } = string.Empty;
    public double[] Values { get; set; } = Array.Empty<double>();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Zero-knowledge proof.
/// </summary>
internal class ZkProof
{
    public string CircuitId { get; set; } = string.Empty;
    public byte[] ProofData { get; set; } = Array.Empty<byte>();
    public Dictionary<string, object> PublicInputs { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Deserialized proof components.
/// </summary>
internal class DeserializedProof
{
    public byte[] A { get; set; } = Array.Empty<byte>();
    public byte[] B { get; set; } = Array.Empty<byte>();
    public byte[] C { get; set; } = Array.Empty<byte>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Verification result.
/// </summary>
internal class VerificationResult
{
    public bool IsValid { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string CircuitId { get; set; } = string.Empty;
    public string PublicInputHash { get; set; } = string.Empty;
}

/// <summary>
/// Secure computation context.
/// </summary>
internal class SecureComputationContext
{
    public string ContextId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, object> Variables { get; set; } = new();
    public Dictionary<string, object> IntermediateValues { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Computation witness.
/// </summary>
internal class ComputationWitness
{
    public string ComputationId { get; set; } = string.Empty;
    public Dictionary<string, object> Inputs { get; set; } = new();
    public Dictionary<string, object> Outputs { get; set; } = new();
    public Dictionary<string, object> IntermediateValues { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cryptographic proof for computation.
/// </summary>
internal class CryptographicProof
{
    public string ProofId { get; set; } = Guid.NewGuid().ToString();
    public string ComputationId { get; set; } = string.Empty;
    public byte[] ProofData { get; set; } = Array.Empty<byte>();
    public string Algorithm { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
