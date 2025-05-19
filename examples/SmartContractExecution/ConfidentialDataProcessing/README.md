# Confidential Data Processing Example

This example demonstrates how to process sensitive data confidentially on the Neo N3 blockchain using the Neo Service Layer (NSL) for secure computation.

## Overview

The Confidential Data Processing contract allows for:

1. Submitting encrypted data for confidential processing
2. Processing data in a secure enclave without revealing the content
3. Retrieving the processed results securely
4. Supporting various types of confidential data processing

## How It Works

### Data Submission

Users can submit encrypted data for processing:
- The data is encrypted before submission
- The contract stores the encrypted data and metadata
- Only the data submitter can later retrieve the results

### Confidential Processing

The Neo Service Layer processes the data confidentially:
- The data is processed in a secure Intel SGX enclave
- The processing logic runs in a trusted execution environment
- The data remains encrypted throughout the processing
- Even the NSL operator cannot view the data content

### Result Retrieval

The data submitter can retrieve the processed results:
- Results are encrypted specifically for the submitter
- Only the original submitter can decrypt the results
- The contract verifies the identity of the requester

## Supported Processing Types

The contract supports various types of confidential data processing:

1. **Machine Learning**: Train or run ML models on sensitive data
2. **Data Analytics**: Perform statistical analysis on private data
3. **Private Set Intersection**: Find common elements between private datasets
4. **Secure Aggregation**: Combine data from multiple sources without revealing individual inputs
5. **Confidential Computation**: Execute custom algorithms on encrypted data

## Architecture

1. **Smart Contract**: The on-chain component that manages data submissions
2. **Neo Service Layer**: The off-chain component that processes data confidentially
3. **Client**: The user interface that encrypts data and decrypts results

## Deployment

### Prerequisites

- Neo N3 private net or testnet
- Neo Service Layer running
- Neo-CLI or Neo-GUI

### Steps

1. Deploy the ConfidentialDataProcessing contract:
   ```
   neo-cli deploy ConfidentialDataProcessing.nef
   ```

2. Initialize the contract with the Neo Service Layer address:
   ```
   neo-cli invoke <contract-hash> initialize <service-layer-address>
   ```

3. (Optional) Set the fee for data processing:
   ```
   neo-cli invoke <contract-hash> setFee <fee-amount>
   ```

## Usage

### Submit Data

```
neo-cli invoke <contract-hash> submitData <encrypted-data> <processing-type>
```

The `<encrypted-data>` should be generated using the Neo Service Layer's encryption API:

```
POST /api/v1/encryption/encrypt
{
  "data": "<raw-data>",
  "publicKey": "<nsl-public-key>"
}
```

The `<processing-type>` should be one of:
- 0: Machine Learning
- 1: Data Analytics
- 2: Private Set Intersection
- 3: Secure Aggregation
- 4: Confidential Computation

### Process Data

```
neo-cli invoke <contract-hash> processData <data-id>
```

### Retrieve Result

```
neo-cli invoke <contract-hash> retrieveResult <data-id>
```

### Get Data Information

```
neo-cli invoke <contract-hash> getDataInfo <data-id>
```

## Integration with Neo Service Layer

The contract calls the Neo Service Layer to:

1. Process confidential data:
   - Decrypt and process data in a secure enclave
   - Apply the specified processing algorithm
   - Store the encrypted results securely

2. Retrieve confidential results:
   - Verify the identity of the requester
   - Encrypt the results specifically for the requester
   - Return the encrypted results

## Use Cases

- **Healthcare**: Process patient data while preserving privacy
- **Finance**: Analyze financial records without exposing sensitive information
- **Research**: Collaborate on datasets without revealing proprietary information
- **IoT**: Process sensor data with privacy guarantees
- **Supply Chain**: Analyze confidential business data across organizations

## Security Considerations

- The Neo Service Layer must be trusted to process data correctly
- The NSL uses Intel SGX to provide confidentiality and integrity guarantees
- Remote attestation should be used to verify the NSL's enclave
- Users should verify the NSL's attestation proof before submitting sensitive data
- The contract includes a fee mechanism to prevent spam attacks
