# Neo Service Layer API Documentation

[![API Version](https://img.shields.io/badge/API-v1.0-blue)](https://github.com/r3e-network/neo-service-layer)
[![OpenAPI](https://img.shields.io/badge/OpenAPI-3.0-green)](https://swagger.io/specification/)
[![Microservices](https://img.shields.io/badge/architecture-microservices-orange)](https://microservices.io/)

> **🚀 Production-Ready Microservices API** - Comprehensive documentation for the Neo Service Layer platform

## Overview

The Neo Service Layer provides a comprehensive RESTful API ecosystem built on a **microservices architecture**. The platform offers secure, scalable services for blockchain applications with Intel SGX integration and comprehensive observability.

## 🏗️ API Architecture

### API Gateway (Primary Entry Point)
**Base URL**: `http://localhost:7000`

The API Gateway serves as the unified entry point for all microservices:
- **Authentication & Authorization** - JWT-based security
- **Rate Limiting** - DDoS protection and fair usage
- **Request Routing** - Intelligent service discovery
- **Load Balancing** - High availability and performance
- **Circuit Breakers** - Fault tolerance and resilience

### Individual Services (Development)
For development and testing, individual services can be accessed directly:

| Service | Port | Purpose | Documentation |
|---------|------|---------|---------------|
| Storage Service | 8081 | Data persistence and retrieval | [Storage API](../services/storage-service.md) |
| Key Management | 8082 | Cryptographic key operations | [Key Management API](../services/key-management-service.md) |
| Notification | 8083 | Multi-channel notifications | [Notification API](../services/notification-service.md) |
| AI Pattern Recognition | 8084 | Machine learning analytics | [AI Services API](../services/pattern-recognition-service.md) |
| Configuration | 8085 | Dynamic configuration management | [Configuration API](../services/configuration-service.md) |
| Oracle | 8086 | External data feeds | [Oracle API](../services/oracle-service.md) |
| Cross-Chain | 8087 | Multi-blockchain operations | [Cross-Chain API](../services/cross-chain-service.md) |

## 🔐 Authentication

### JWT Authentication
All production API calls require JWT authentication:

```http
GET /api/v1/storage/documents HTTP/1.1
Host: localhost:7000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

### Obtaining Access Tokens

```bash
# Authenticate and get JWT token
curl -X POST http://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your-username",
    "password": "your-password"
  }'

# Response
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "def502004a8b7e..."
}
```

### API Key Authentication (Development)
For development and testing, API key authentication is available:

```http
GET /api/v1/health HTTP/1.1
Host: localhost:7000
X-API-Key: your-development-api-key
```

## 📊 Core API Endpoints

### System Health & Monitoring

#### Health Check
```bash
GET /health
```
Returns overall system health status.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0",
  "services": {
    "gateway": "healthy",
    "consul": "healthy",
    "storage": "healthy",
    "keymanagement": "healthy",
    "notification": "healthy"
  }
}
```

#### Service Discovery
```bash
GET /api/v1/services
```
Returns available services and their status.

#### Metrics
```bash
GET /metrics
```
Prometheus-formatted metrics for monitoring.

### Authentication Endpoints

#### Login
```bash
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "string",
  "password": "string"
}
```

#### Refresh Token
```bash
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refresh_token": "string"
}
```

#### Logout
```bash
POST /api/v1/auth/logout
Authorization: Bearer {token}
```

## 🗄️ Storage Service API

### Document Operations

#### Store Document
```bash
POST /api/v1/storage/documents
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "contract-template.json",
  "content": "base64-encoded-content",
  "metadata": {
    "type": "smart-contract",
    "version": "1.0"
  }
}
```

#### Retrieve Document
```bash
GET /api/v1/storage/documents/{id}
Authorization: Bearer {token}
```

#### List Documents
```bash
GET /api/v1/storage/documents?page=1&limit=20&type=smart-contract
Authorization: Bearer {token}
```

#### Delete Document
```bash
DELETE /api/v1/storage/documents/{id}
Authorization: Bearer {token}
```

### Encrypted Storage
```bash
POST /api/v1/storage/encrypted
Authorization: Bearer {token}
Content-Type: application/json

{
  "data": "sensitive-data",
  "encryption_key_id": "key-123",
  "metadata": {
    "classification": "confidential"
  }
}
```

## 🔑 Key Management API

### Key Generation
```bash
POST /api/v1/keys/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "algorithm": "RSA",
  "key_size": 2048,
  "usage": ["encrypt", "decrypt"],
  "metadata": {
    "purpose": "smart-contract-signing"
  }
}
```

### Key Retrieval
```bash
GET /api/v1/keys/{key_id}
Authorization: Bearer {token}
```

### Digital Signatures
```bash
POST /api/v1/keys/{key_id}/sign
Authorization: Bearer {token}
Content-Type: application/json

{
  "data": "data-to-sign",
  "algorithm": "SHA256withRSA"
}
```

### Key Rotation
```bash
POST /api/v1/keys/{key_id}/rotate
Authorization: Bearer {token}
```

## 📧 Notification Service API

### Send Notification
```bash
POST /api/v1/notifications/send
Authorization: Bearer {token}
Content-Type: application/json

{
  "channel": "email",
  "recipient": "user@example.com",
  "subject": "Transaction Confirmation",
  "message": "Your transaction has been confirmed",
  "priority": "normal",
  "metadata": {
    "transaction_id": "tx-123"
  }
}
```

### Notification Templates
```bash
POST /api/v1/notifications/templates
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "transaction-alert",
  "channel": "email",
  "subject": "Transaction Alert: {{transaction_type}}",
  "body": "Transaction {{transaction_id}} of type {{transaction_type}} has been {{status}}."
}
```

### Notification History
```bash
GET /api/v1/notifications/history?page=1&limit=50
Authorization: Bearer {token}
```

## 🤖 AI Services API

### Pattern Recognition
```bash
POST /api/v1/ai/pattern-recognition/analyze
Authorization: Bearer {token}
Content-Type: application/json

{
  "data": {
    "transactions": [...],
    "addresses": [...],
    "time_range": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-01-15T23:59:59Z"
    }
  },
  "analysis_type": "fraud_detection"
}
```

### Prediction Services
```bash
POST /api/v1/ai/prediction/forecast
Authorization: Bearer {token}
Content-Type: application/json

{
  "model": "market-prediction",
  "input_data": {
    "historical_prices": [...],
    "market_indicators": {...}
  },
  "prediction_horizon": "7d"
}
```

## ⛓️ Cross-Chain Bridge API

### Supported Networks
```bash
GET /api/v1/crosschain/networks
Authorization: Bearer {token}
```

### Bridge Transaction
```bash
POST /api/v1/crosschain/bridge
Authorization: Bearer {token}
Content-Type: application/json

{
  "source_network": "neo-n3",
  "destination_network": "neo-x",
  "asset": "GAS",
  "amount": "10.0",
  "destination_address": "0x742d35cc6ab4b16c56b27a8a3cb5db1d3ec0e4a1"
}
```

### Transaction Status
```bash
GET /api/v1/crosschain/transactions/{transaction_id}
Authorization: Bearer {token}
```

## 🔮 Oracle Service API

### Data Feeds
```bash
GET /api/v1/oracle/feeds
Authorization: Bearer {token}
```

### Request Data
```bash
POST /api/v1/oracle/request
Authorization: Bearer {token}
Content-Type: application/json

{
  "source": "coinmarketcap",
  "query": {
    "symbol": "NEO",
    "convert": "USD"
  },
  "callback_url": "https://your-app.com/oracle-callback"
}
```

### Subscribe to Feed
```bash
POST /api/v1/oracle/subscriptions
Authorization: Bearer {token}
Content-Type: application/json

{
  "feed_id": "crypto-prices",
  "callback_url": "https://your-app.com/price-updates",
  "filters": {
    "symbols": ["NEO", "GAS"]
  }
}
```

## 📋 Response Format

### Standard Success Response
```json
{
  "success": true,
  "data": {
    // Response payload
  },
  "metadata": {
    "timestamp": "2024-01-15T10:30:00Z",
    "request_id": "req-123456",
    "version": "1.0"
  }
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input parameters",
    "details": {
      "field": "amount",
      "reason": "must be greater than 0"
    }
  },
  "metadata": {
    "timestamp": "2024-01-15T10:30:00Z",
    "request_id": "req-123456"
  }
}
```

### Pagination Response
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 150,
    "total_pages": 8,
    "has_next": true,
    "has_previous": false
  }
}
```

## ⚠️ Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `AUTHENTICATION_REQUIRED` | 401 | Valid authentication token required |
| `INSUFFICIENT_PERMISSIONS` | 403 | User lacks required permissions |
| `RESOURCE_NOT_FOUND` | 404 | Requested resource does not exist |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `RATE_LIMIT_EXCEEDED` | 429 | Rate limit exceeded |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |
| `INTERNAL_ERROR` | 500 | Internal server error |

## 🚦 Rate Limiting

### Default Limits
- **Requests per minute**: 100
- **Requests per hour**: 1,000  
- **Burst requests**: 20

### Rate Limit Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248600
```

### Rate Limit Exceeded Response
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Please try again later.",
    "retry_after": 60
  }
}
```

## 📊 Observability

### Distributed Tracing
All API requests include tracing headers:
```http
X-Trace-Id: 123e4567-e89b-12d3-a456-426614174000
X-Span-Id: 456e7890-f12c-34e5-b678-539725281001
```

### Request Logging
All requests are logged with:
- Request ID for correlation
- User ID (if authenticated)
- Response time
- Status code
- User agent

### Metrics
Key metrics tracked:
- Request latency (P50, P95, P99)
- Request volume
- Error rates
- Service availability

## 🔧 Development Tools

### OpenAPI Specification
```bash
GET /api/v1/openapi.json
```
Returns the complete OpenAPI 3.0 specification.

### Interactive Documentation
```bash
GET /swagger
```
Swagger UI for interactive API exploration.

### Health Dashboard
```bash
GET /health/dashboard
```
HTML dashboard showing service health and metrics.

## 📱 SDK Support

### Official SDKs
- **[.NET SDK](../../src/SDK/NeoServiceLayer.SDK/README.md)** - Full-featured .NET client
- **[JavaScript SDK](../../website/README_SDK.md)** - Browser and Node.js support
- **[Python SDK](https://pypi.org/project/neo-service-layer/)** - Python client library

### SDK Example (.NET)
```csharp
var client = new NeoServiceLayerClient("http://localhost:7000");
await client.AuthenticateAsync("username", "password");

var result = await client.Storage.StoreDocumentAsync(new Document
{
    Name = "contract.json",
    Content = documentBytes,
    Metadata = new { Type = "smart-contract" }
});
```

## 🧪 Testing

### Development Environment
```bash
# Start development stack
docker-compose up -d

# Run API tests
dotnet test tests/Integration/NeoServiceLayer.Integration.Tests/ \
  --filter "FullyQualifiedName~MockedServiceTests"
```

### Postman Collection
Download the [Postman Collection](./postman-collection.json) for comprehensive API testing.

### Environment Variables
```bash
# Development
NEO_SERVICE_API_URL=http://localhost:7000
NEO_SERVICE_API_KEY=dev-api-key

# Production
NEO_SERVICE_API_URL=https://api.neoservice.io
NEO_SERVICE_JWT_TOKEN=production-jwt-token
```

## 📚 Additional Resources

### API Guides
- **[Authentication Guide](./authentication.md)** - Detailed authentication setup
- **[Rate Limiting Guide](./rate-limiting.md)** - Rate limiting configuration
- **[Error Handling Guide](./error-handling.md)** - Error handling best practices

### Service Documentation
- **[Storage Service](../services/storage-service.md)** - Data storage and retrieval
- **[Key Management](../services/key-management-service.md)** - Cryptographic operations
- **[Notification Service](../services/notification-service.md)** - Multi-channel messaging
- **[AI Services](../services/pattern-recognition-service.md)** - Machine learning APIs

### Integration Examples
- **[Smart Contract Integration](../../examples/smart-contract-integration.md)**
- **[Cross-Chain Bridge Usage](../../examples/cross-chain-bridge.md)**
- **[Real-Time Notifications](../../examples/notification-integration.md)**

## 🤝 Support

- **📖 Documentation**: Complete API documentation
- **🐛 Issues**: [GitHub Issues](https://github.com/r3e-network/neo-service-layer/issues)
- **💬 API Support**: [API Discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **📧 Contact**: api-support@r3e.network

---

**🚀 Ready to integrate? Start with our [Quick Start Guide](../deployment/QUICK_START.md)!**