using NeoServiceLayer.Core;
using NeoServiceLayer.Services.ZeroKnowledge.Models;
using ProofRequest = NeoServiceLayer.Services.ZeroKnowledge.Models.GenerateProofRequest;
using ServiceProofResult = NeoServiceLayer.Services.ZeroKnowledge.Models.ProofResult;
using ProofVerification = NeoServiceLayer.Core.Models.ProofVerification;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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
    Task<ServiceProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Generates a zero-knowledge proof using Core ProofRequest.
    /// </summary>
    /// <param name="request">The core proof request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>The proof result.</returns>
    Task<ServiceProofResult> GenerateProofAsync(NeoServiceLayer.Core.Models.ProofRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Verifies a zero-knowledge proof.
    /// </summary>
    /// <param name="verification">The proof verification request.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if the proof is valid.</returns>
    Task<bool> VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType);
}
