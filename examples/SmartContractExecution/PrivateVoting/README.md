# Private Voting Example

This example demonstrates how to create a private voting system on the Neo N3 blockchain using the Neo Service Layer (NSL) for confidential computing.

## Overview

The Private Voting contract allows for:

1. Creating voting sessions with multiple options
2. Casting votes privately (vote choices are encrypted)
3. Tallying votes while preserving voter privacy
4. Verifying voting results without revealing individual votes

## How It Works

### Voting Creation

The contract owner can create a new voting session by specifying:
- A unique voting ID
- The number of options voters can choose from
- The end time for the voting session

### Private Voting

Users can cast votes privately:
- The voter's address is public (to prevent double voting)
- The vote choice is encrypted
- The NSL verifies the vote is valid (within the option range)
- The NSL stores the encrypted vote securely

### Vote Tallying

When the voting period ends:
- The contract owner or any user (after the end time) can trigger vote tallying
- The NSL tallies the votes in a secure enclave
- Only the final counts for each option are revealed
- Individual votes remain confidential

## Architecture

1. **Smart Contract**: The on-chain component that manages voting sessions
2. **Neo Service Layer**: The off-chain component that handles confidential operations
3. **Client**: The user interface that interacts with both components

## Deployment

### Prerequisites

- Neo N3 private net or testnet
- Neo Service Layer running
- Neo-CLI or Neo-GUI

### Steps

1. Deploy the PrivateVoting contract:
   ```
   neo-cli deploy PrivateVoting.nef
   ```

2. Initialize the contract with the Neo Service Layer address:
   ```
   neo-cli invoke <contract-hash> initialize <service-layer-address>
   ```

## Usage

### Create a Voting Session

```
neo-cli invoke <contract-hash> createVoting <voting-id> <options-count> <end-time>
```

Example:
```
neo-cli invoke <contract-hash> createVoting election2023 5 1672531200
```

### Cast a Private Vote

```
neo-cli invoke <contract-hash> castVote <voting-id> <encrypted-vote>
```

The `<encrypted-vote>` should be generated using the Neo Service Layer's encryption API:

```
POST /api/v1/encryption/encrypt
{
  "data": "<vote-option>",
  "votingId": "<voting-id>"
}
```

### End Voting and Tally Results

```
neo-cli invoke <contract-hash> endVoting <voting-id>
```

### Get Voting Information

```
neo-cli invoke <contract-hash> getVotingInfo <voting-id>
```

### Check if a User Has Voted

```
neo-cli invoke <contract-hash> hasVoted <voting-id> <voter-address>
```

## Integration with Neo Service Layer

The contract calls the Neo Service Layer to:

1. Process private votes:
   - Decrypt and validate votes in a secure enclave
   - Store encrypted votes securely
   - Prevent unauthorized access to vote data

2. Tally private votes:
   - Decrypt and count votes in a secure enclave
   - Return only the final counts
   - Preserve voter privacy

## Security Considerations

- The Neo Service Layer must be trusted to process votes correctly
- The NSL uses Intel SGX to provide confidentiality and integrity guarantees
- Remote attestation should be used to verify the NSL's enclave
- Users should verify the NSL's attestation proof before casting votes
