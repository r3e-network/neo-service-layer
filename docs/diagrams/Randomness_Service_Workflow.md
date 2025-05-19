# Randomness Service Workflow

```mermaid
sequenceDiagram
    participant App as Application
    participant Host as TeeEnclaveHost
    participant Interface as OpenEnclaveInterface
    participant Enclave as OpenEnclaveEnclave
    participant Random as RandomnessService
    participant Utils as OpenEnclaveUtils
    
    Note over App,Utils: Random Number Generation
    
    App->>Host: GenerateRandomNumberAsync(min, max, userId, requestId)
    Host->>Interface: GenerateRandomNumberAsync
    Interface->>Enclave: generate_random_number
    Enclave->>Random: generate_random_number
    Random->>Utils: get_entropy()
    Utils-->>Random: entropy
    Random->>Random: generate_random_number(min, max)
    Random->>Random: store_random_number(randomNumber, userId, requestId)
    Random-->>Enclave: randomNumber
    Enclave-->>Interface: randomNumber
    Interface-->>Host: randomNumber
    Host-->>App: randomNumber
    
    Note over App,Utils: Proof Generation
    
    App->>Host: GetRandomNumberProofAsync(randomNumber, min, max, userId, requestId)
    Host->>Interface: GetRandomNumberProofAsync
    Interface->>Enclave: get_random_number_proof
    Enclave->>Random: get_random_number_proof
    Random->>Random: create_proof_data(randomNumber, min, max, userId, requestId)
    Random->>Utils: compute_sha256(proofData)
    Utils-->>Random: hash
    Random->>Utils: sign_data(hash)
    Utils-->>Random: signature
    Random->>Utils: base64_encode(signature)
    Utils-->>Random: proof
    Random-->>Enclave: proof
    Enclave-->>Interface: proof
    Interface-->>Host: proof
    Host-->>App: proof
    
    Note over App,Utils: Verification
    
    App->>Host: VerifyRandomNumberAsync(randomNumber, min, max, userId, requestId, proof)
    Host->>Interface: VerifyRandomNumberAsync
    Interface->>Enclave: verify_random_number
    Enclave->>Random: verify_random_number
    
    alt Stored in memory
        Random->>Random: check_stored_random_number(randomNumber, min, max, userId, requestId, proof)
        Random-->>Enclave: isValid
    else Verify proof
        Random->>Random: create_proof_data(randomNumber, min, max, userId, requestId)
        Random->>Utils: compute_sha256(proofData)
        Utils-->>Random: hash
        Random->>Utils: base64_decode(proof)
        Utils-->>Random: signature
        Random->>Utils: verify_signature(hash, signature)
        Utils-->>Random: isValid
        Random-->>Enclave: isValid
    end
    
    Enclave-->>Interface: isValid
    Interface-->>Host: isValid
    Host-->>App: isValid
```

## Workflow Description

### Random Number Generation

1. The application calls GenerateRandomNumberAsync with the minimum and maximum values, user ID, and request ID.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the generate_random_number method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the generate_random_number method of the RandomnessService.
5. The RandomnessService gets entropy from the OpenEnclaveUtils.
6. The RandomnessService generates a random number between the minimum and maximum values.
7. The RandomnessService stores the random number along with the user ID and request ID.
8. The random number is returned to the application.

### Proof Generation

1. The application calls GetRandomNumberProofAsync with the random number, minimum and maximum values, user ID, and request ID.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the get_random_number_proof method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the get_random_number_proof method of the RandomnessService.
5. The RandomnessService creates proof data containing the random number, minimum and maximum values, user ID, request ID, and a timestamp.
6. The RandomnessService computes the SHA-256 hash of the proof data.
7. The RandomnessService signs the hash using the enclave's private key.
8. The RandomnessService base64-encodes the signature to create the proof.
9. The proof is returned to the application.

### Verification

1. The application calls VerifyRandomNumberAsync with the random number, minimum and maximum values, user ID, request ID, and proof.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the verify_random_number method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the verify_random_number method of the RandomnessService.
5. The RandomnessService checks if the random number is stored in memory:
   - If it is, it compares the stored proof with the provided proof.
   - If it's not, it recreates the proof data, computes the hash, and verifies the signature.
6. The verification result is returned to the application.
