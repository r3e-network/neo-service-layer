# Neo Service Layer Workflows

## Overview

This document describes common workflows for using the Neo Service Layer. These workflows demonstrate how to use the various services together to accomplish common tasks.

## Workflow 1: Secure Random Number Generation for Smart Contracts

This workflow demonstrates how to generate a secure random number and use it in a smart contract.

### Steps

1. **Generate a Random Number**

   Use the Randomness Service to generate a verifiable random number:

   ```http
   POST /api/v1/randomness/generate
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "min": 1,
     "max": 100,
     "seed": "optional-seed"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "value": 42,
       "proof": "cryptographic-proof",
       "timestamp": "2023-01-01T00:00:00Z"
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

2. **Verify the Random Number**

   Verify the random number using the Randomness Service:

   ```http
   POST /api/v1/randomness/verify
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "value": 42,
     "proof": "cryptographic-proof",
     "timestamp": "2023-01-01T00:00:00Z"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "valid": true
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

3. **Use the Random Number in a Smart Contract**

   Use the random number in a Neo N3 smart contract:

   ```csharp
   using Neo.SmartContract.Framework;
   using Neo.SmartContract.Framework.Services;
   using System;
   using System.ComponentModel;

   namespace RandomnessExample
   {
       [DisplayName("RandomnessExample")]
       [ManifestExtra("Author", "Neo")]
       [ManifestExtra("Email", "dev@neo.org")]
       [ManifestExtra("Description", "Randomness Example")]
       public class RandomnessExample : SmartContract
       {
           [DisplayName("RandomNumberUsed")]
           public static event Action<int> OnRandomNumberUsed;

           public static bool UseRandomNumber(int value, byte[] proof, string timestamp)
           {
               // Verify the random number using the Randomness Service
               bool isValid = VerifyRandomNumber(value, proof, timestamp);
               if (!isValid)
               {
                   return false;
               }

               // Use the random number
               OnRandomNumberUsed(value);

               // Perform some action based on the random number
               // ...

               return true;
           }

           private static bool VerifyRandomNumber(int value, byte[] proof, string timestamp)
           {
               // Call the Randomness Service to verify the random number
               // This is a simplified example
               return true;
           }
       }
   }
   ```

## Workflow 2: Secure Data Feed for Smart Contracts

This workflow demonstrates how to fetch data from an external source and use it in a smart contract.

### Steps

1. **Fetch Data from an External Source**

   Use the Oracle Service to fetch data from an external source:

   ```http
   POST /api/v1/oracle/fetch
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "url": "https://api.example.com/data",
     "path": "$.data.value"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "value": "42.5",
       "proof": "cryptographic-proof",
       "timestamp": "2023-01-01T00:00:00Z",
       "source": "https://api.example.com/data"
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

2. **Verify the Data**

   Verify the data using the Oracle Service:

   ```http
   POST /api/v1/oracle/verify
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "value": "42.5",
     "proof": "cryptographic-proof",
     "timestamp": "2023-01-01T00:00:00Z",
     "source": "https://api.example.com/data"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "valid": true
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

3. **Use the Data in a Smart Contract**

   Use the data in a Neo N3 smart contract:

   ```csharp
   using Neo.SmartContract.Framework;
   using Neo.SmartContract.Framework.Services;
   using System;
   using System.ComponentModel;

   namespace OracleExample
   {
       [DisplayName("OracleExample")]
       [ManifestExtra("Author", "Neo")]
       [ManifestExtra("Email", "dev@neo.org")]
       [ManifestExtra("Description", "Oracle Example")]
       public class OracleExample : SmartContract
       {
           [DisplayName("DataUsed")]
           public static event Action<string> OnDataUsed;

           public static bool UseData(string value, byte[] proof, string timestamp, string source)
           {
               // Verify the data using the Oracle Service
               bool isValid = VerifyData(value, proof, timestamp, source);
               if (!isValid)
               {
                   return false;
               }

               // Use the data
               OnDataUsed(value);

               // Perform some action based on the data
               // ...

               return true;
           }

           private static bool VerifyData(string value, byte[] proof, string timestamp, string source)
           {
               // Call the Oracle Service to verify the data
               // This is a simplified example
               return true;
           }
       }
   }
   ```

## Workflow 3: Secure Key Management for Smart Contracts

This workflow demonstrates how to generate and use keys for signing transactions.

### Steps

1. **Generate a Key Pair**

   Use the Key Management Service to generate a key pair:

   ```http
   POST /api/v1/keys/generate
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "keyType": "secp256r1",
     "keyUsage": "signing",
     "exportable": false
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "keyId": "key-id",
       "publicKey": "public-key",
       "keyType": "secp256r1",
       "keyUsage": "signing",
       "exportable": false,
       "createdAt": "2023-01-01T00:00:00Z"
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

2. **Sign Data**

   Use the Key Management Service to sign data:

   ```http
   POST /api/v1/keys/sign
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "keyId": "key-id",
     "data": "data-to-sign",
     "algorithm": "ECDSA"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "signature": "signature",
       "keyId": "key-id",
       "algorithm": "ECDSA",
       "timestamp": "2023-01-01T00:00:00Z"
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

3. **Verify a Signature**

   Use the Key Management Service to verify a signature:

   ```http
   POST /api/v1/keys/verify
   Host: api.neoservicelayer.org
   Content-Type: application/json
   X-API-Key: your-api-key

   {
     "blockchain": "neo-n3",
     "keyId": "key-id",
     "data": "data-to-verify",
     "signature": "signature",
     "algorithm": "ECDSA"
   }
   ```

   Response:

   ```json
   {
     "success": true,
     "data": {
       "valid": true
     },
     "error": null,
     "meta": {
       "requestId": "request-id",
       "timestamp": "2023-01-01T00:00:00Z"
     }
   }
   ```

## References

- [Neo N3 Documentation](https://docs.neo.org/)
- [NeoX Documentation](https://docs.neo.org/neox/)
- [Neo Service Layer API Documentation](../api/README.md)
