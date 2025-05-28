# Neo Service Layer - Randomness Service

## Overview

The Randomness Service provides verifiable random number generation for blockchain applications on the Neo N3 and NeoX blockchains. It leverages Intel SGX with Occlum LibOS enclaves to ensure the integrity and unpredictability of the random number generation process.

## Features

- **Secure Random Number Generation**: Generates random numbers within secure enclaves.
- **Verifiable Randomness**: Provides cryptographic proofs for verifying the fairness of random numbers.
- **Multi-Blockchain Support**: Supports both Neo N3 and NeoX blockchains.
- **Various Output Formats**: Generates random numbers, bytes, and strings.

## Architecture

The Randomness Service consists of the following components:

### Service Layer

- **IRandomnessService**: Interface defining the Randomness service operations.
- **RandomnessService**: Implementation of the Randomness service, inheriting from EnclaveBlockchainServiceBase.

### Enclave Layer

- **Enclave Implementation**: C++ code running within Intel SGX with Occlum LibOS enclaves to securely generate random numbers.
- **Secure Communication**: Encrypted communication between the service layer and the enclave.

### Blockchain Integration

- **Neo N3 Integration**: Integration with the Neo N3 blockchain for additional entropy and verification.
- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible) for additional entropy and verification.

## Random Number Generation

The Randomness Service uses a combination of techniques to generate secure and verifiable random numbers:

1. **Hardware-Based Randomness**: Uses hardware random number generators (RNGs) within the enclave.
2. **Cryptographic RNGs**: Uses cryptographically secure pseudorandom number generators (CSPRNGs).
3. **Blockchain Entropy**: Incorporates blockchain data (e.g., block hashes) as additional entropy.
4. **Verifiable Random Functions (VRFs)**: Uses VRFs to generate verifiable random numbers.

## API Reference

### IRandomnessService Interface

```csharp
public interface IRandomnessService : IEnclaveService, IBlockchainService
{
    Task<int> GenerateRandomNumberAsync(int min, int max, BlockchainType blockchainType);
    Task<byte[]> GenerateRandomBytesAsync(int length, BlockchainType blockchainType);
    Task<string> GenerateRandomStringAsync(int length, BlockchainType blockchainType);
    Task<VerifiableRandomResult> GenerateVerifiableRandomNumberAsync(int min, int max, string seed, BlockchainType blockchainType);
}
```

#### Methods

- **GenerateRandomNumberAsync**: Generates a random number between min and max (inclusive).
  - Parameters:
    - `min`: The minimum value.
    - `max`: The maximum value.
    - `blockchainType`: The blockchain type.
  - Returns: A random number between min and max (inclusive).

- **GenerateRandomBytesAsync**: Generates a random bytes array of the specified length.
  - Parameters:
    - `length`: The length of the random bytes array.
    - `blockchainType`: The blockchain type.
  - Returns: A random bytes array of the specified length.

- **GenerateRandomStringAsync**: Generates a random string of the specified length.
  - Parameters:
    - `length`: The length of the random string.
    - `blockchainType`: The blockchain type.
  - Returns: A random string of the specified length.

- **GenerateVerifiableRandomNumberAsync**: Generates a verifiable random number between min and max (inclusive).
  - Parameters:
    - `min`: The minimum value.
    - `max`: The maximum value.
    - `seed`: The seed for the random number generation.
    - `blockchainType`: The blockchain type.
  - Returns: A verifiable random result containing the random number and proof.

### VerifiableRandomResult Class

```csharp
public class VerifiableRandomResult
{
    public int Value { get; set; }
    public string Seed { get; set; }
    public string Proof { get; set; }
    public DateTime Timestamp { get; set; }
    public BlockchainType BlockchainType { get; set; }
}
```

#### Properties

- **Value**: The random value.
- **Seed**: The seed used for the random number generation.
- **Proof**: The proof of the random number generation.
- **Timestamp**: The timestamp of the random number generation.
- **BlockchainType**: The blockchain type.

## Smart Contract Integration

### Neo N3

```csharp
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace RandomnessConsumer
{
    [Contract("0x0123456789abcdef0123456789abcdef")]
    public class RandomnessConsumer : SmartContract
    {
        [InitialValue("0xabcdef0123456789abcdef0123456789", ContractParameterType.Hash160)]
        private static readonly UInt160 RandomnessContractAddress = default;

        public static int GetRandomNumber(int min, int max)
        {
            var result = (int)Contract.Call(RandomnessContractAddress, "getRandomNumber", CallFlags.All, new object[] { min, max });
            return result;
        }

        public static object GetVerifiableRandomNumber(int min, int max, string seed)
        {
            var result = (string)Contract.Call(RandomnessContractAddress, "getVerifiableRandomNumber", CallFlags.All, new object[] { min, max, seed });
            return result;
        }
    }
}
```

### NeoX (EVM)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface IRandomnessConsumer {
    function getRandomNumber(uint256 min, uint256 max) external view returns (uint256);
    function getVerifiableRandomNumber(uint256 min, uint256 max, string calldata seed) external view returns (uint256 value, string memory proof);
}

contract RandomnessConsumer {
    address private randomnessContract;

    constructor(address _randomnessContract) {
        randomnessContract = _randomnessContract;
    }

    function getRandomNumber(uint256 min, uint256 max) external view returns (uint256) {
        return IRandomnessConsumer(randomnessContract).getRandomNumber(min, max);
    }

    function getVerifiableRandomNumber(uint256 min, uint256 max, string calldata seed) external view returns (uint256 value, string memory proof) {
        return IRandomnessConsumer(randomnessContract).getVerifiableRandomNumber(min, max, seed);
    }
}
```

## Use Cases

The Randomness Service can be used for a variety of applications:

- **Gaming**: Verifiable random number generation for games of chance.
- **NFT Minting**: Random selection of NFT attributes.
- **Lottery Systems**: Verifiable lottery draws.
- **Random Sampling**: Selecting random participants for airdrops or rewards.
- **Cryptographic Key Generation**: Generating secure cryptographic keys.

## Security Considerations

- **Enclave Security**: All random number generation occurs within secure Intel SGX with Occlum LibOS enclaves.
- **Verifiability**: Cryptographic proofs ensure the verifiability of random numbers.
- **Entropy Sources**: Multiple entropy sources are used to ensure unpredictability.
- **Blockchain Integration**: Blockchain data is used as additional entropy.

## Deployment

The Randomness Service is deployed as part of the Neo Service Layer, with the following components:

- **Service Layer**: Deployed as a .NET service.
- **Enclave Layer**: Deployed within Intel SGX with Occlum LibOS enclaves.
- **Smart Contracts**: Deployed on the Neo N3 and NeoX blockchains.

## Conclusion

The Randomness Service provides a secure and verifiable way to generate random numbers for blockchain applications on the Neo N3 and NeoX blockchains. By leveraging Intel SGX with Occlum LibOS enclaves, it ensures the integrity and unpredictability of the random number generation process, enabling new classes of decentralized applications that require verifiable randomness.
