using System.Threading.Tasks;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.ZeroKnowledge;

/// <summary>
/// Interface for zero-knowledge proof services.
/// </summary>
public interface IZeroKnowledgeService : IEnclaveService, IBlockchainService
{
    /// <summary>
    /// Generates a zero-knowledge proof.
    /// </summary>
    /// <param name="request">The proof generation request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The proof result.</returns>
    Task<ProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a zero-knowledge proof.
    /// </summary>
    /// <param name="verification">The proof verification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the proof is valid.</returns>
    Task<bool> VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType);
}