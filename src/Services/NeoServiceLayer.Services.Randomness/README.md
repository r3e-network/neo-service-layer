# Randomness Service

## Overview

The Randomness Service is a secure, enclave-based service for generating verifiable random numbers. It provides a framework for generating random numbers that can be verified by third parties, ensuring that the randomness is fair and tamper-proof.

## Features

- **Verifiable Random Numbers**: Generate random numbers with cryptographic proofs that can be verified by third parties.
- **Seed-Based Generation**: Generate random numbers based on a seed, allowing for deterministic but unpredictable sequences.
- **Block Hash Integration**: Incorporate blockchain block hashes into the random number generation process for additional entropy.
- **Cryptographic Proofs**: Generate cryptographic proofs for random numbers, allowing third parties to verify the authenticity of the randomness.
- **Blockchain Support**: Support for both Neo N3 and NeoX blockchains.

## Architecture

The Randomness Service is built on the Neo Service Layer framework and uses the Trusted Execution Environment (TEE) to provide secure random number generation. The service consists of the following components:

- **IRandomnessService**: The interface that defines the operations supported by the service.
- **RandomnessService**: The implementation of the service that uses the enclave to generate random numbers.
- **EnclaveManager**: The component that manages the communication with the enclave.

## Usage

### Service Registration

```csharp
// Register the service
services.AddNeoService<IRandomnessService, RandomnessService>();

// Register the service with the service registry
serviceProvider.RegisterAllNeoServices();
```

### Generating a Random Number

```csharp
// Generate a random number
var result = await randomnessService.GenerateRandomNumberAsync(
    0,
    100,
    BlockchainType.NeoN3);

Console.WriteLine($"Random number: {result.Value}");
```

### Generating a Verifiable Random Number

```csharp
// Generate a verifiable random number
var result = await randomnessService.GenerateVerifiableRandomNumberAsync(
    "my-seed",
    0,
    100,
    BlockchainType.NeoN3);

Console.WriteLine($"Random number: {result.Value}");
Console.WriteLine($"Proof: {result.Proof}");
```

### Verifying a Random Number

```csharp
// Verify a random number
bool isValid = await randomnessService.VerifyRandomNumberAsync(
    result,
    BlockchainType.NeoN3);

if (isValid)
{
    Console.WriteLine("Random number is valid.");
}
else
{
    Console.WriteLine("Random number is invalid.");
}
```

## Security Considerations

- All random number generation is performed within the secure enclave.
- Random numbers are cryptographically signed to ensure authenticity.
- All operations are logged for audit purposes.

## API Reference

### GenerateRandomNumberAsync

Generates a random number.

```csharp
Task<RandomnessResult> GenerateRandomNumberAsync(
    int minValue,
    int maxValue,
    BlockchainType blockchainType);
```

### GenerateVerifiableRandomNumberAsync

Generates a verifiable random number.

```csharp
Task<RandomnessResult> GenerateVerifiableRandomNumberAsync(
    string seed,
    int minValue,
    int maxValue,
    BlockchainType blockchainType);
```

### VerifyRandomNumberAsync

Verifies a random number.

```csharp
Task<bool> VerifyRandomNumberAsync(
    RandomnessResult result,
    BlockchainType blockchainType);
```

### GetRandomnessSourcesAsync

Gets the list of randomness sources.

```csharp
Task<IEnumerable<string>> GetRandomnessSourcesAsync(
    BlockchainType blockchainType);
```
