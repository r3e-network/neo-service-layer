# Neo Service Layer - Zero-Knowledge Service

## Overview

The Zero-Knowledge Service provides privacy-preserving computation and verification capabilities for the Neo N3 and NeoX blockchains. It leverages Intel SGX with Occlum LibOS enclaves to generate and verify zero-knowledge proofs, enabling private transactions, confidential computations, and selective disclosure while maintaining verifiability.

## Features

- **zk-SNARK Proof Generation**: Generate succinct non-interactive zero-knowledge proofs
- **zk-STARK Verification**: Verify transparent zero-knowledge proofs without trusted setup
- **Private Set Intersection**: Find common elements without revealing full sets
- **Confidential Voting**: Enable private voting with public verification
- **Selective Disclosure**: Prove specific attributes without revealing full data
- **Private Computations**: Execute computations on encrypted data
- **Multi-Blockchain Support**: Supports both Neo N3 and NeoX blockchains

## Architecture

The Zero-Knowledge Service consists of the following components:

### Service Layer

- **IZeroKnowledgeService**: Interface defining the Zero-Knowledge service operations
- **ZeroKnowledgeService**: Implementation of the service, inheriting from EnclaveBlockchainServiceBase

### Enclave Layer

- **Proof Generator**: C++ code running within Intel SGX with Occlum LibOS enclaves to generate proofs
- **Verification Engine**: Verifies zero-knowledge proofs within the secure enclave
- **Circuit Compiler**: Compiles high-level constraints into arithmetic circuits

### Blockchain Integration

- **Neo N3 Integration**: Integration with Neo N3 blockchain for proof verification
- **NeoX Integration**: Integration with NeoX blockchain (EVM-compatible) for proof verification

## Zero-Knowledge Proof Types

### 1. zk-SNARKs (Zero-Knowledge Succinct Non-Interactive Arguments of Knowledge)
- **Succinct**: Proofs are small and fast to verify
- **Non-Interactive**: No interaction required between prover and verifier
- **Trusted Setup**: Requires initial trusted setup ceremony
- **Use Cases**: Private transactions, confidential voting, identity verification

### 2. zk-STARKs (Zero-Knowledge Scalable Transparent Arguments of Knowledge)
- **Transparent**: No trusted setup required
- **Scalable**: Proof generation scales better with computation size
- **Post-Quantum Secure**: Resistant to quantum computer attacks
- **Use Cases**: Large-scale computations, blockchain scalability, audit trails

### 3. Bulletproofs
- **Range Proofs**: Prove values are within specific ranges without revealing them
- **No Trusted Setup**: Transparent and secure
- **Logarithmic Size**: Proof size grows logarithmically with range
- **Use Cases**: Confidential transactions, private asset amounts

## API Reference

### IZeroKnowledgeService Interface

```csharp
public interface IZeroKnowledgeService : IEnclaveService, IBlockchainService
{
    Task<ProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType);
    Task<bool> VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType);
    Task<string> CompileCircuitAsync(CircuitDefinition circuit, BlockchainType blockchainType);
    Task<PrivateSetIntersectionResult> ComputePrivateSetIntersectionAsync(PSIRequest request, BlockchainType blockchainType);
    Task<VotingProofResult> GenerateVotingProofAsync(VotingRequest request, BlockchainType blockchainType);
    Task<SelectiveDisclosureProof> GenerateSelectiveDisclosureProofAsync(DisclosureRequest request, BlockchainType blockchainType);
    Task<RangeProofResult> GenerateRangeProofAsync(RangeProofRequest request, BlockchainType blockchainType);
    Task<bool> VerifyRangeProofAsync(RangeProofVerification verification, BlockchainType blockchainType);
    Task<IEnumerable<CircuitTemplate>> GetAvailableCircuitsAsync(BlockchainType blockchainType);
    Task<ProofMetadata> GetProofMetadataAsync(string proofId, BlockchainType blockchainType);
}
```

#### Methods

- **GenerateProofAsync**: Generates a zero-knowledge proof for given inputs
  - Parameters:
    - `request`: Proof generation request with circuit and inputs
    - `blockchainType`: The blockchain type
  - Returns: Generated proof and metadata

- **VerifyProofAsync**: Verifies a zero-knowledge proof
  - Parameters:
    - `verification`: Proof verification request
    - `blockchainType`: The blockchain type
  - Returns: True if the proof is valid

- **CompileCircuitAsync**: Compiles a high-level circuit definition
  - Parameters:
    - `circuit`: Circuit definition in high-level language
    - `blockchainType`: The blockchain type
  - Returns: Compiled circuit identifier

- **ComputePrivateSetIntersectionAsync**: Computes intersection of private sets
  - Parameters:
    - `request`: Private set intersection request
    - `blockchainType`: The blockchain type
  - Returns: Intersection result without revealing full sets

- **GenerateVotingProofAsync**: Generates proof for confidential voting
  - Parameters:
    - `request`: Voting proof request
    - `blockchainType`: The blockchain type
  - Returns: Voting proof that hides vote while proving validity

- **GenerateSelectiveDisclosureProofAsync**: Generates selective disclosure proof
  - Parameters:
    - `request`: Selective disclosure request
    - `blockchainType`: The blockchain type
  - Returns: Proof of specific attributes without revealing others

- **GenerateRangeProofAsync**: Generates range proof for confidential amounts
  - Parameters:
    - `request`: Range proof request
    - `blockchainType`: The blockchain type
  - Returns: Proof that value is within range without revealing value

### Data Models

#### ProofRequest Class

```csharp
public class ProofRequest
{
    public string CircuitId { get; set; }
    public ProofType Type { get; set; }
    public Dictionary<string, object> PublicInputs { get; set; }
    public Dictionary<string, object> PrivateInputs { get; set; }
    public string ProofSystem { get; set; } // "groth16", "plonk", "stark"
    public ProofParameters Parameters { get; set; }
}
```

#### ProofResult Class

```csharp
public class ProofResult
{
    public string ProofId { get; set; }
    public string Proof { get; set; }
    public string[] PublicSignals { get; set; }
    public DateTime GeneratedAt { get; set; }
    public ProofMetadata Metadata { get; set; }
    public string VerificationKey { get; set; }
}
```

#### VotingRequest Class

```csharp
public class VotingRequest
{
    public string VotingId { get; set; }
    public string Vote { get; set; } // Encrypted vote
    public string VoterCommitment { get; set; }
    public string[] ValidOptions { get; set; }
    public string NullifierHash { get; set; }
}
```

#### SelectiveDisclosureRequest Class

```csharp
public class DisclosureRequest
{
    public string CredentialId { get; set; }
    public string[] AttributesToReveal { get; set; }
    public Dictionary<string, object> Predicates { get; set; }
    public string IssuerSignature { get; set; }
    public string Schema { get; set; }
}
```

#### RangeProofRequest Class

```csharp
public class RangeProofRequest
{
    public decimal Value { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public string Commitment { get; set; }
    public string BlindingFactor { get; set; }
}
```

#### Enums

```csharp
public enum ProofType
{
    SNARK,
    STARK,
    Bulletproof,
    PLONK,
    Groth16
}

public enum CircuitType
{
    Membership,
    RangeProof,
    Voting,
    Identity,
    Computation,
    Custom
}
```

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace ZeroKnowledgeConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class ZeroKnowledgeConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 ZKContractAddress = default;

        // Verify a zero-knowledge proof
        public static bool VerifyProof(string proof, string[] publicSignals, string verificationKey)
        {
            var result = (bool)Contract.Call(ZKContractAddress, "verifyProof", CallFlags.All, 
                new object[] { proof, publicSignals, verificationKey });
            return result;
        }

        // Verify membership proof (user is in a group without revealing identity)
        public static bool VerifyMembership(string membershipProof, string groupCommitment)
        {
            var result = (bool)Contract.Call(ZKContractAddress, "verifyMembership", CallFlags.All, 
                new object[] { membershipProof, groupCommitment });
            return result;
        }

        // Verify range proof (value is within range without revealing value)
        public static bool VerifyRange(string rangeProof, string commitment, int minValue, int maxValue)
        {
            var result = (bool)Contract.Call(ZKContractAddress, "verifyRange", CallFlags.All, 
                new object[] { rangeProof, commitment, minValue, maxValue });
            return result;
        }

        // Private voting with zero-knowledge proofs
        public static bool CastPrivateVote(string votingId, string voteProof, string nullifierHash)
        {
            // Verify the vote is valid without revealing the actual vote
            var isValid = (bool)Contract.Call(ZKContractAddress, "verifyVotingProof", CallFlags.All, 
                new object[] { votingId, voteProof, nullifierHash });
            
            if (isValid)
            {
                // Record the nullifier to prevent double voting
                Storage.Put(Storage.CurrentContext, "nullifier_" + nullifierHash, true);
                return true;
            }
            return false;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IZeroKnowledgeConsumer {
    function verifyProof(
        string calldata proof,
        string[] calldata publicSignals,
        string calldata verificationKey
    ) external view returns (bool);
    
    function verifyMembership(
        string calldata membershipProof,
        string calldata groupCommitment
    ) external view returns (bool);
    
    function verifyRange(
        string calldata rangeProof,
        string calldata commitment,
        uint256 minValue,
        uint256 maxValue
    ) external view returns (bool);
}

contract ZeroKnowledgeConsumer {
    address private zkContract;
    mapping(string => bool) private usedNullifiers;
    
    event PrivateVoteCast(string votingId, string nullifierHash);
    event ProofVerified(string proofType, bool isValid);
    
    constructor(address _zkContract) {
        zkContract = _zkContract;
    }
    
    // Verify a zero-knowledge proof
    function verifyProof(
        string calldata proof,
        string[] calldata publicSignals,
        string calldata verificationKey
    ) external view returns (bool) {
        return IZeroKnowledgeConsumer(zkContract).verifyProof(proof, publicSignals, verificationKey);
    }
    
    // Verify membership proof (user is in a group without revealing identity)
    function verifyMembership(
        string calldata membershipProof,
        string calldata groupCommitment
    ) external view returns (bool) {
        return IZeroKnowledgeConsumer(zkContract).verifyMembership(membershipProof, groupCommitment);
    }
    
    // Verify range proof (value is within range without revealing value)
    function verifyRange(
        string calldata rangeProof,
        string calldata commitment,
        uint256 minValue,
        uint256 maxValue
    ) external view returns (bool) {
        return IZeroKnowledgeConsumer(zkContract).verifyRange(rangeProof, commitment, minValue, maxValue);
    }
    
    // Private voting with zero-knowledge proofs
    function castPrivateVote(
        string calldata votingId,
        string calldata voteProof,
        string calldata nullifierHash
    ) external returns (bool) {
        require(!usedNullifiers[nullifierHash], "Vote already cast");
        
        // Verify the vote is valid without revealing the actual vote
        bool isValid = IZeroKnowledgeConsumer(zkContract).verifyProof(
            voteProof,
            new string[](0),
            ""
        );
        
        if (isValid) {
            usedNullifiers[nullifierHash] = true;
            emit PrivateVoteCast(votingId, nullifierHash);
            return true;
        }
        return false;
    }
}
```

## Use Cases

### Privacy-Preserving DeFi
- **Private Transactions**: Hide transaction amounts while proving validity
- **Confidential Trading**: Trade without revealing positions or strategies
- **Anonymous Lending**: Prove creditworthiness without revealing identity
- **Private Auctions**: Conduct sealed-bid auctions with verifiable results

### Identity and Credentials
- **Age Verification**: Prove age without revealing exact birthdate
- **Income Verification**: Prove income range without revealing exact amount
- **Education Credentials**: Prove qualifications without revealing full transcript
- **Professional Licensing**: Prove professional status without revealing details

### Governance and Voting
- **Anonymous Voting**: Vote privately while preventing double voting
- **Weighted Voting**: Prove voting power without revealing token holdings
- **Delegation Privacy**: Delegate votes without revealing delegation relationships
- **Proposal Privacy**: Submit proposals anonymously with proof of eligibility

### Compliance and Regulation
- **KYC Privacy**: Prove compliance without revealing personal information
- **Tax Reporting**: Prove tax compliance without revealing full financial data
- **Audit Trails**: Create verifiable audit trails while maintaining privacy
- **Regulatory Reporting**: Report to regulators while protecting user privacy

## Security Considerations

- **Enclave Security**: All proof generation occurs within secure Intel SGX with Occlum LibOS enclaves
- **Trusted Setup**: Secure trusted setup ceremonies for zk-SNARKs
- **Circuit Security**: Formal verification of arithmetic circuits
- **Side-Channel Protection**: Protection against timing and power analysis attacks
- **Proof Integrity**: Cryptographic verification of all proofs

## Deployment

The Zero-Knowledge Service is deployed as part of the Neo Service Layer:

- **Service Layer**: Deployed as a .NET service with high availability
- **Enclave Layer**: Deployed within Intel SGX with Occlum LibOS enclaves
- **Circuit Library**: Pre-compiled circuits for common use cases
- **Smart Contracts**: Deployed on Neo N3 and NeoX blockchains

## Conclusion

The Zero-Knowledge Service enables privacy-preserving applications on the Neo ecosystem while maintaining the transparency and verifiability that blockchain applications require. By leveraging Intel SGX with Occlum LibOS enclaves for secure proof generation and providing comprehensive zero-knowledge capabilities, it empowers developers to build sophisticated privacy-preserving applications on both Neo N3 and NeoX blockchains.
