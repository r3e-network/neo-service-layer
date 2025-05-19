# Verifiable Randomness Example

This example demonstrates how to generate provably fair random numbers on the Neo N3 blockchain using the Neo Service Layer (NSL) for secure and verifiable randomness.

## Overview

The Verifiable Randomness contract provides:

1. Secure random number generation
2. Verifiable proofs of randomness
3. Support for single or multiple random numbers
4. Customizable range for random numbers
5. Optional seed input for deterministic randomness

## How It Works

### Random Number Request

Users can request random numbers by:
- Specifying a range (min and max values)
- Optionally providing a seed for deterministic randomness
- Paying a small fee in GAS (configurable by the contract owner)

### Random Number Generation

The Neo Service Layer generates random numbers:
- Using a secure source of entropy within the TEE
- Creating cryptographic proofs of the randomness
- Ensuring the numbers are within the specified range
- Returning both the numbers and their proofs

### Verification

Anyone can verify the randomness:
- The contract stores both the random numbers and their proofs
- The NSL provides verification functions
- The verification process is transparent and deterministic

## Architecture

1. **Smart Contract**: The on-chain component that handles requests and stores results
2. **Neo Service Layer**: The off-chain component that generates secure random numbers
3. **Client**: The user interface that interacts with both components

## Deployment

### Prerequisites

- Neo N3 private net or testnet
- Neo Service Layer running
- Neo-CLI or Neo-GUI

### Steps

1. Deploy the VerifiableRandomness contract:
   ```
   neo-cli deploy VerifiableRandomness.nef
   ```

2. Initialize the contract with the Neo Service Layer address:
   ```
   neo-cli invoke <contract-hash> initialize <service-layer-address>
   ```

3. (Optional) Set the fee for random number generation:
   ```
   neo-cli invoke <contract-hash> setFee <fee-amount>
   ```

## Usage

### Request a Random Number

```
neo-cli invoke <contract-hash> requestRandomNumber <min> <max> [seed]
```

Example:
```
neo-cli invoke <contract-hash> requestRandomNumber 1 100 "my-seed"
```

This returns a request ID that you'll use to generate and retrieve the random number.

### Generate a Random Number

```
neo-cli invoke <contract-hash> generateRandomNumber <request-id>
```

### Generate Multiple Random Numbers

```
neo-cli invoke <contract-hash> generateRandomNumbers <request-id> <count>
```

Example:
```
neo-cli invoke <contract-hash> generateRandomNumbers "tx-hash-timestamp" 5
```

### Verify Random Number Proof

```
neo-cli invoke <contract-hash> verifyRandomNumberProof <request-id>
```

### Withdraw Fees (Owner Only)

```
neo-cli invoke <contract-hash> withdrawFees <to-address>
```

## Integration with Neo Service Layer

The contract calls the Neo Service Layer to:

1. Generate random numbers:
   - Using a secure source of entropy in the TEE
   - Creating cryptographic proofs
   - Ensuring the numbers are within the specified range

2. Verify randomness proofs:
   - Validating the cryptographic proofs
   - Confirming the numbers were generated correctly

## Use Cases

- Gaming: Fair lottery systems, random item drops, card shuffling
- DeFi: Random selection of validators, fair distribution mechanisms
- Governance: Random selection of committee members
- Security: Generating secure keys and nonces

## Security Considerations

- The Neo Service Layer must be trusted to generate truly random numbers
- The NSL uses Intel SGX to provide confidentiality and integrity guarantees
- Remote attestation should be used to verify the NSL's enclave
- Users should verify the NSL's attestation proof before relying on the random numbers
- The contract includes a fee mechanism to prevent spam attacks
