# Neo Service Layer - API Service

## Overview

The API Service provides a unified RESTful API interface for all Neo Service Layer services. It handles authentication, authorization, request routing, rate limiting, and response formatting, providing a consistent and secure way for clients to interact with the Neo Service Layer.

## Features

- **Unified API Interface**: A single entry point for all Neo Service Layer services.
- **Authentication**: Support for API key, JWT, and OAuth 2.0 authentication methods.
- **Authorization**: Role-based access control for API endpoints.
- **Request Routing**: Route requests to the appropriate service.
- **Rate Limiting**: Limit the rate of API requests to prevent abuse.
- **Response Formatting**: Format responses in a consistent way.
- **API Documentation**: Swagger/OpenAPI documentation for the API.
- **API Versioning**: Support for multiple API versions.
- **Error Handling**: Consistent error handling and reporting.
- **Logging**: Comprehensive logging of API requests and responses.
- **Metrics**: Collection and exposure of API metrics.

## Architecture

The API Service consists of the following components:

### API Gateway

- **Request Handler**: Handles incoming HTTP requests.
- **Authentication Handler**: Authenticates API requests.
- **Authorization Handler**: Authorizes API requests.
- **Rate Limiting Handler**: Limits the rate of API requests.
- **Request Router**: Routes requests to the appropriate service.
- **Response Formatter**: Formats responses in a consistent way.
- **Error Handler**: Handles and reports errors.
- **Logging Handler**: Logs API requests and responses.
- **Metrics Handler**: Collects and exposes API metrics.

### Service Integration

- **Service Registry**: Registry of available services.
- **Service Client**: Client for communicating with services.
- **Service Discovery**: Discovery of service endpoints.

### API Documentation

- **Swagger/OpenAPI**: Swagger/OpenAPI documentation for the API.
- **API Explorer**: Interactive API explorer.
- **API Reference**: Comprehensive API reference documentation.

## Request Flow

The request flow through the API Service follows these steps:

1. **Request Reception**: The API Service receives an HTTP request.
2. **Authentication**: The request is authenticated using the provided credentials.
3. **Authorization**: The request is authorized based on the authenticated user's roles and permissions.
4. **Rate Limiting**: The request is checked against rate limits.
5. **Request Routing**: The request is routed to the appropriate service.
6. **Service Processing**: The service processes the request.
7. **Response Formatting**: The response is formatted in a consistent way.
8. **Response Return**: The formatted response is returned to the client.

## API Endpoints

The API Service provides endpoints for all Neo Service Layer services:

- **Randomness Service**: `/api/v1/randomness/*`
- **Oracle Service**: `/api/v1/oracle/*`
- **Key Management Service**: `/api/v1/keys/*`
- **Compute Service**: `/api/v1/compute/*`
- **Storage Service**: `/api/v1/storage/*`
- **Compliance Service**: `/api/v1/compliance/*`
- **Event Subscription Service**: `/api/v1/events/*`

For detailed information about the endpoints provided by each service, see the respective service documentation.

## Authentication

The API Service supports the following authentication methods:

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

## Authorization

The API Service uses role-based access control to authorize API requests. Each API endpoint is associated with one or more roles, and users are assigned roles that determine which endpoints they can access.

The following roles are defined:

- **Admin**: Full access to all API endpoints.
- **User**: Access to basic API endpoints.
- **Service**: Access to service-specific API endpoints.

## Rate Limiting

The API Service implements rate limiting to prevent abuse. Rate limits are applied on a per-API-key basis, with the following default limits:

- **Requests per second**: 10
- **Requests per minute**: 100
- **Requests per hour**: 1,000
- **Requests per day**: 10,000

Rate limit information is included in the response headers:

- **X-RateLimit-Limit**: The maximum number of requests allowed in the current time window.
- **X-RateLimit-Remaining**: The number of requests remaining in the current time window.
- **X-RateLimit-Reset**: The time at which the current rate limit window resets, in UTC epoch seconds.

If you exceed the rate limit, you will receive a 429 Too Many Requests response with a Retry-After header indicating how many seconds to wait before making another request.

## Response Format

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

## Error Handling

The API Service provides consistent error handling and reporting. Errors are categorized by HTTP status code and include an error code, message, and details.

The following HTTP status codes are used:

- **400 Bad Request**: The request was invalid or cannot be served.
- **401 Unauthorized**: Authentication is required or has failed.
- **403 Forbidden**: The request is not allowed.
- **404 Not Found**: The requested resource does not exist.
- **409 Conflict**: The request conflicts with the current state of the server.
- **429 Too Many Requests**: The user has sent too many requests in a given amount of time.
- **500 Internal Server Error**: An error occurred on the server.
- **503 Service Unavailable**: The server is currently unavailable.

## API Versioning

The API Service supports multiple API versions to ensure backward compatibility. The API version is specified in the URL path:

```
https://api.neoservicelayer.org/api/v1/randomness/generate
```

In this example, `v1` indicates that you are using version 1 of the API.

## API Documentation

The API Service provides comprehensive API documentation:

- **Swagger/OpenAPI**: Swagger/OpenAPI documentation for the API.
- **API Explorer**: Interactive API explorer for testing API endpoints.
- **API Reference**: Comprehensive API reference documentation.

The API documentation is available at:

```
https://api.neoservicelayer.org/docs
```

## Logging

The API Service logs all API requests and responses for audit and debugging purposes. Logs include the following information:

- **Request ID**: A unique identifier for the request.
- **Timestamp**: The time at which the request was received.
- **HTTP Method**: The HTTP method used for the request.
- **URL**: The URL of the request.
- **Status Code**: The HTTP status code of the response.
- **Response Time**: The time taken to process the request.
- **User ID**: The ID of the authenticated user.
- **IP Address**: The IP address of the client.
- **User Agent**: The user agent of the client.

## Metrics

The API Service collects and exposes the following metrics:

- **Request Count**: The number of requests received.
- **Success Count**: The number of successful requests.
- **Failure Count**: The number of failed requests.
- **Response Time**: The time taken to process requests.
- **Request Rate**: The rate of requests per second.
- **Error Rate**: The rate of errors per second.

Metrics are exposed via a Prometheus-compatible endpoint:

```
https://api.neoservicelayer.org/metrics
```

## Deployment

The API Service is deployed as part of the Neo Service Layer, with the following components:

- **API Gateway**: Deployed as a .NET service.
- **Service Integration**: Deployed as part of the API Gateway.
- **API Documentation**: Deployed as static files served by the API Gateway.

## Conclusion

The API Service provides a unified, secure, and consistent way for clients to interact with the Neo Service Layer. By handling authentication, authorization, request routing, rate limiting, and response formatting, it simplifies the integration of blockchain applications with the Neo Service Layer services.
