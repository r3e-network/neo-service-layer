# Neo Service Layer API

## Overview

The Neo Service Layer provides a RESTful API for interacting with its services. The API is designed to be simple, consistent, and secure.

## Authentication

All API requests require authentication. The Neo Service Layer supports the following authentication methods:

- **API Key**: A simple API key that is included in the request header.
- **JWT**: JSON Web Tokens for more secure authentication.
- **OAuth 2.0**: OAuth 2.0 for delegated authentication.

### API Key Authentication

To use API key authentication, include the API key in the `X-API-Key` header:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
X-API-Key: your-api-key
```

### JWT Authentication

To use JWT authentication, include the JWT token in the `Authorization` header:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
Authorization: Bearer your-jwt-token
```

### OAuth 2.0 Authentication

To use OAuth 2.0 authentication, obtain an access token from the OAuth 2.0 server and include it in the `Authorization` header:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
Authorization: Bearer your-oauth-token
```

## API Endpoints

The Neo Service Layer API is organized around the following services:

- [Randomness Service](../services/randomness-service.md)
- [Oracle Service](../services/oracle-service.md)
- [Data Feeds Service](../services/data-feeds-service.md)
- [Key Management Service](../services/key-management-service.md)
- [Compute Service](../services/compute-service.md)
- [Storage Service](../services/storage-service.md)
- [Compliance Service](../services/compliance-service.md)
- [Event Subscription Service](../services/event-subscription-service.md)
- [Automation Service](../services/automation-service.md)
- [Cross-Chain Service](../services/cross-chain-service.md)
- [Proof of Reserve Service](../services/proof-of-reserve-service.md)

## Common Request Parameters

All API requests support the following common parameters:

- **blockchain**: The blockchain type to use (e.g., `neo-n3`, `neo-x`).
- **format**: The response format (e.g., `json`, `xml`).
- **version**: The API version to use (e.g., `v1`).

## Common Response Format

All API responses follow a common format:

```json
{
  "success": true,
  "data": {
    // Response data
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

In case of an error:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "error-code",
    "message": "Error message",
    "details": {
      // Error details
    }
  },
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Error Codes

The Neo Service Layer API uses the following error codes:

- **400**: Bad Request - The request was invalid or cannot be served.
- **401**: Unauthorized - Authentication is required or has failed.
- **403**: Forbidden - The request is not allowed.
- **404**: Not Found - The requested resource does not exist.
- **409**: Conflict - The request conflicts with the current state of the server.
- **429**: Too Many Requests - The user has sent too many requests in a given amount of time.
- **500**: Internal Server Error - An error occurred on the server.
- **503**: Service Unavailable - The server is currently unavailable.

## Rate Limiting

The Neo Service Layer API implements rate limiting to prevent abuse. Rate limits are applied on a per-API-key basis. The rate limits are as follows:

- **Requests per second**: 10
- **Requests per minute**: 100
- **Requests per hour**: 1,000
- **Requests per day**: 10,000

Rate limit information is included in the response headers:

- **X-RateLimit-Limit**: The maximum number of requests allowed in the current time window.
- **X-RateLimit-Remaining**: The number of requests remaining in the current time window.
- **X-RateLimit-Reset**: The time at which the current rate limit window resets, in UTC epoch seconds.

If you exceed the rate limit, you will receive a 429 Too Many Requests response.

## Pagination

For endpoints that return multiple items, the Neo Service Layer API supports pagination. Pagination parameters are included in the query string:

- **page**: The page number to retrieve (default: 1).
- **per_page**: The number of items per page (default: 10, max: 100).

Pagination information is included in the response metadata:

```json
{
  "success": true,
  "data": [
    // Response data
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z",
    "pagination": {
      "page": 1,
      "per_page": 10,
      "total_pages": 5,
      "total_items": 42
    }
  }
}
```

## Versioning

The Neo Service Layer API is versioned to ensure backward compatibility. The version is included in the URL path:

```
https://api.neoservicelayer.org/api/v1/randomness/generate
```

## API Explorer

The Neo Service Layer provides an API Explorer for interactive documentation and testing. The API Explorer is available at:

```
https://api.neoservicelayer.org/explorer
```

## SDKs

The Neo Service Layer provides SDKs for various programming languages:

- [.NET SDK](https://github.com/neo-project/neo-service-layer-dotnet-sdk)
- [JavaScript SDK](https://github.com/neo-project/neo-service-layer-js-sdk)
- [Python SDK](https://github.com/neo-project/neo-service-layer-python-sdk)
- [Java SDK](https://github.com/neo-project/neo-service-layer-java-sdk)
- [Go SDK](https://github.com/neo-project/neo-service-layer-go-sdk)

## References

- [Neo N3 Documentation](https://docs.neo.org/)
- [NeoX Documentation](https://docs.neo.org/neox/)
- [RESTful API Design Guidelines](https://restfulapi.net/)
