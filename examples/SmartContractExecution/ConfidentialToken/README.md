# Confidential Token Example

This example demonstrates how to create a confidential token on the Neo N3 blockchain using the Neo Service Layer (NSL) for confidential computing.

## Overview

The Confidential Token (CTOK) is a NEP-17 compatible token that adds confidential transfer capabilities. It allows users to:

1. Transfer tokens publicly (standard NEP-17 transfers)
2. Transfer tokens confidentially (amounts are encrypted)
3. Mint and burn tokens (admin functions)

## How It Works

### Standard Transfers

Standard transfers work like any NEP-17 token:
- Sender and recipient addresses are public
- Transfer amount is public
- Balances are stored on-chain in plaintext

### Confidential Transfers

Confidential transfers use the Neo Service Layer:
- Sender and recipient addresses are still public
- Transfer amount is encrypted
- The NSL verifies the transfer in a secure enclave
- The NSL updates encrypted balances

## Architecture

1. **Smart Contract**: The on-chain component that handles token logic
2. **Neo Service Layer**: The off-chain component that handles confidential operations
3. **Client**: The user interface that interacts with both components

## Deployment

### Prerequisites

- Neo N3 private net or testnet
- Neo Service Layer running
- Neo-CLI or Neo-GUI

### Steps

1. Deploy the ConfidentialToken contract:
   ```
   neo-cli deploy ConfidentialToken.nef
   ```

2. Initialize the contract with the Neo Service Layer address:
   ```
   neo-cli invoke <contract-hash> initialize <service-layer-address>
   ```

3. Mint initial tokens:
   ```
   neo-cli invoke <contract-hash> mint <address> <amount>
   ```

## Usage

### Standard Transfer

```
neo-cli invoke <contract-hash> transfer <from-address> <to-address> <amount> []
```

### Confidential Transfer

```
neo-cli invoke <contract-hash> confidentialTransfer <from-address> <to-address> <encrypted-amount>
```

The `<encrypted-amount>` should be generated using the Neo Service Layer's encryption API:

```
POST /api/v1/encryption/encrypt
{
  "data": "<amount>",
  "publicKey": "<recipient-public-key>"
}
```

## Integration with Neo Service Layer

The contract calls the Neo Service Layer to process confidential transfers. The NSL:

1. Decrypts the amount in a secure enclave
2. Verifies the sender has sufficient balance
3. Updates the encrypted balances
4. Returns success/failure to the contract

## Security Considerations

- The Neo Service Layer must be trusted to process confidential transfers correctly
- The NSL uses Intel SGX to provide confidentiality and integrity guarantees
- Remote attestation should be used to verify the NSL's enclave
- Users should verify the NSL's attestation proof before using confidential transfers
