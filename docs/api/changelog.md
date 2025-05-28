# Neo Service Layer API Changelog

## Overview

This document provides a chronological list of changes to the Neo Service Layer API. It includes new features, bug fixes, and breaking changes.

## v2.0.0 (2023-06-01)

### Breaking Changes

- **Authentication**: Changed the authentication mechanism to use JWT tokens instead of API keys. API keys are still supported for backward compatibility.
- **Response Format**: Changed the response format to include a `success` field and an `error` field. The `data` field now contains the actual response data.
- **Error Handling**: Changed the error response format to include an `error` object with `code`, `message`, and `details` fields.
- **Pagination**: Changed the pagination mechanism to use cursor-based pagination instead of page-based pagination. Page-based pagination is still supported for backward compatibility.

### New Features

- **Randomness Service**:
  - Added support for additional random number generation algorithms.
  - Added support for batch random number generation.
  - Added support for custom seed generation.

- **Oracle Service**:
  - Added support for additional data sources.
  - Added support for data transformation options.
  - Added support for batch data fetching.

- **Key Management Service**:
  - Added support for additional key types and algorithms.
  - Added support for key rotation.
  - Added support for key revocation.

- **Compute Service**:
  - Added support for additional computation types and languages.
  - Added support for batch computation.
  - Added support for computation scheduling.

- **Storage Service**:
  - Added support for additional storage options.
  - Added support for access control mechanisms.
  - Added support for data versioning.

- **Compliance Service**:
  - Added support for additional compliance rules and regulations.
  - Added support for compliance reporting.
  - Added support for compliance auditing.

- **Event Subscription Service**:
  - Added support for additional event types.
  - Added support for event filtering options.
  - Added support for event batching.

### Bug Fixes

- Fixed an issue where the randomness service would return the same random number for consecutive requests with the same seed.
- Fixed an issue where the oracle service would fail to fetch data from certain HTTPS sources.
- Fixed an issue where the key management service would not properly validate key types.
- Fixed an issue where the compute service would not properly handle computation timeouts.
- Fixed an issue where the storage service would not properly handle large files.
- Fixed an issue where the compliance service would not properly validate certain address formats.
- Fixed an issue where the event subscription service would not properly deliver events in order.

## v1.5.0 (2023-03-15)

### New Features

- **Randomness Service**:
  - Added support for custom seed generation.
  - Added support for random byte generation.

- **Oracle Service**:
  - Added support for HTTPS data sources.
  - Added support for JSON path expressions.

- **Key Management Service**:
  - Added support for key export.
  - Added support for key import.

- **Compute Service**:
  - Added support for JavaScript computation.
  - Added support for Python computation.

- **Storage Service**:
  - Added support for file chunking.
  - Added support for file encryption.

- **Compliance Service**:
  - Added support for address validation.
  - Added support for transaction validation.

- **Event Subscription Service**:
  - Added support for webhook callbacks.
  - Added support for event filtering.

### Bug Fixes

- Fixed an issue where the randomness service would fail to generate random numbers with certain parameters.
- Fixed an issue where the oracle service would fail to parse certain JSON responses.
- Fixed an issue where the key management service would fail to generate certain key types.
- Fixed an issue where the compute service would fail to execute certain computations.
- Fixed an issue where the storage service would fail to store certain file types.
- Fixed an issue where the compliance service would fail to validate certain address types.
- Fixed an issue where the event subscription service would fail to deliver events to certain webhook endpoints.

## v1.0.0 (2023-01-01)

### Initial Release

- **Randomness Service**:
  - Random number generation.
  - Random number verification.

- **Oracle Service**:
  - Data fetching from external sources.
  - Data verification.

- **Key Management Service**:
  - Key generation.
  - Key signing.
  - Key verification.

- **Compute Service**:
  - Computation registration.
  - Computation execution.
  - Computation verification.

- **Storage Service**:
  - Data storage.
  - Data retrieval.

- **Compliance Service**:
  - Address verification.
  - Transaction verification.

- **Event Subscription Service**:
  - Subscription creation.
  - Event retrieval.

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Versioning](versioning.md)
