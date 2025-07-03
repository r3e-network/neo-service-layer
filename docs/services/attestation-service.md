# Attestation Service

## Overview

The Attestation Service provides secure remote attestation capabilities for Intel SGX enclaves. It enables verification of enclave integrity, ensuring that code execution occurs within a genuine SGX enclave with untampered code.

## Features

- **Remote Attestation**: Verify enclave integrity from external systems
- **Quote Generation**: Generate SGX quotes with custom user data
- **Report Verification**: Validate attestation reports and certificates
- **Enclave Identity**: Manage enclave measurement and identity
- **Chain of Trust**: Verify complete attestation certificate chain
- **Real-time Status**: Monitor attestation service availability
- **Audit Trail**: Complete logging of attestation events

## API Reference

### Generate Attestation Report

Generates a new attestation report for the current enclave.

**Endpoint**: `POST /api/v1/attestation/generate/{blockchainType}`

**Request Body**:
```json
{
  "userData": "base64_encoded_user_data",
  "reportType": "QUOTE",
  "targetInfo": {
    "mrenclave": "hex_measurement",
    "attributes": "0x03"
  }
}
```

**Response**:
```json
{
  "success": true,
  "reportId": "att_123abc...",
  "quote": "base64_encoded_quote",
  "timestamp": "2025-01-01T00:00:00Z",
  "enclaveInfo": {
    "mrenclave": "hex_measurement",
    "mrsigner": "hex_signer",
    "isvprodid": 1,
    "isvsvn": 1
  }
}
```

### Verify Attestation

Verifies an attestation report or quote.

**Endpoint**: `POST /api/v1/attestation/verify/{blockchainType}`

**Request Body**:
```json
{
  "attestationReport": "base64_encoded_report",
  "expectedMeasurement": "hex_measurement",
  "nonce": "verification_nonce"
}
```

**Response**:
```json
{
  "success": true,
  "verified": true,
  "details": {
    "measurementMatch": true,
    "signatureValid": true,
    "certificateChainValid": true,
    "timestampValid": true
  },
  "enclaveStatus": "OK"
}
```

### Get Enclave Status

Retrieves current enclave status and measurements.

**Endpoint**: `GET /api/v1/attestation/status/{blockchainType}`

**Response**:
```json
{
  "enclaveRunning": true,
  "measurements": {
    "mrenclave": "c29b7e7ba3ac...",
    "mrsigner": "83d719e77dea...",
    "isvprodid": 1,
    "isvsvn": 1
  },
  "attestationAvailable": true,
  "lastAttestationTime": "2025-01-01T00:00:00Z"
}
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "AttestationService": {
    "Enabled": true,
    "AttestationProvider": "Intel",
    "EpidUrl": "https://api.trustedservices.intel.com/sgx/dev/attestation/v4/",
    "IasApiKey": "your-ias-api-key",
    "QuoteType": "ECDSA",
    "CacheTimeout": 3600,
    "VerificationPolicy": {
      "RequireLatestTcb": true,
      "AcceptConfigurationNeeded": false,
      "AcceptGroupOutOfDate": false
    }
  }
}
```

## Attestation Flow

1. **Enclave Initialization**
   - Enclave starts and generates initial measurements
   - MRENCLAVE and MRSIGNER values are computed

2. **Quote Generation**
   - Application requests attestation quote
   - Enclave generates report with user data
   - Quote is signed by platform

3. **Remote Verification**
   - Verifier receives quote
   - Checks signature and certificate chain
   - Validates measurements against expected values

4. **Trust Establishment**
   - Successful verification establishes trust
   - Secure channel can be created

## Security Considerations

- **Measurement Validation**: Always verify MRENCLAVE matches expected value
- **Replay Protection**: Include nonce or timestamp in user data
- **Certificate Validation**: Verify complete certificate chain
- **TCB Updates**: Keep platform TCB (Trusted Computing Base) updated
- **Quote Freshness**: Check quote generation timestamp

## Usage Examples

### Generate Attestation for Service

```csharp
var client = new AttestationServiceClient(apiKey);

var request = new GenerateAttestationRequest
{
    UserData = Encoding.UTF8.GetBytes("service-binding-data"),
    ReportType = AttestationReportType.Quote
};

var result = await client.GenerateAttestationAsync(request, BlockchainType.NeoN3);
Console.WriteLine($"Quote generated: {result.ReportId}");
```

### Verify Remote Service

```csharp
var verifyRequest = new VerifyAttestationRequest
{
    AttestationReport = receivedQuote,
    ExpectedMeasurement = knownGoodMeasurement,
    Nonce = generatedNonce
};

var verification = await client.VerifyAttestationAsync(verifyRequest, BlockchainType.NeoN3);
if (verification.Verified)
{
    Console.WriteLine("Remote service verified successfully");
}
```

## Best Practices

1. **Regular Attestation**: Re-attest periodically during long sessions
2. **Measurement Storage**: Securely store expected measurements
3. **Nonce Usage**: Always include fresh nonce for replay protection
4. **Error Handling**: Gracefully handle attestation failures
5. **Audit Logging**: Log all attestation events for compliance

## Performance Considerations

- Quote generation: ~500ms
- Remote verification: ~200ms (excluding network)
- Status check: <10ms
- Certificate caching reduces verification time

## Limitations

- Maximum user data size: 64 bytes
- Quote validity period: 24 hours
- Concurrent attestations: Limited by platform
- Network dependency for IAS verification

## Related Services

- [Key Management Service](key-management-service.md) - For secure key attestation
- [Compute Service](compute-service.md) - For attested computations
- [Network Security Service](network-security-service.md) - For secure communications