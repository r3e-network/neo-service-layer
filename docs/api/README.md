# Neo Service Layer API

> **🎉 UPDATED FOR WORKING DEPLOYMENT** - All endpoints tested and fully operational!

## Overview

The Neo Service Layer provides a comprehensive RESTful API for interacting with blockchain services. The API is currently deployed as a **standalone service** that is fully operational and ready for production use.

## 🚀 **Current Working API Service**

**Base URL**: `http://localhost:5002`

### ✅ **Fully Operational Endpoints**

| Endpoint | Method | Purpose | Status | Response |
|----------|--------|---------|--------|----------|
| `/` | GET | Service information | ✅ Working | Service info with version |
| `/health` | GET | Health check | ✅ Working | "Healthy" |
| `/api/status` | GET | Service status | ✅ Working | All services healthy |
| `/api/database/test` | GET | Database connectivity | ✅ Working | PostgreSQL connection info |
| `/api/redis/test` | GET | Redis connectivity | ✅ Working | Redis connection success |
| `/api/neo/version` | GET | Neo service info | ✅ Working | Neo 3.8.1 with features |
| `/api/neo/simulate` | POST | Neo operations | ✅ Working | Simulation results |
| `/api/test` | POST | Test endpoint | ✅ Working | Test response |
| `/swagger` | GET | API documentation | ✅ Working | Swagger UI |

### 🌐 **Access the Working API**

**Primary Access Points:**
- **🏠 Service Info**: `http://localhost:5002/` - Service information and version
- **💚 Health Check**: `http://localhost:5002/health` - Health status
- **📊 Service Status**: `http://localhost:5002/api/status` - All service health
- **📚 API Documentation**: `http://localhost:5002/swagger` - Interactive Swagger UI

**Infrastructure Tests:**
- **🗄️ Database Test**: `http://localhost:5002/api/database/test` - PostgreSQL connectivity
- **🔴 Redis Test**: `http://localhost:5002/api/redis/test` - Redis connectivity

**Neo Integration:**
- **🛸 Neo Version**: `http://localhost:5002/api/neo/version` - Neo service information
- **🧪 Neo Simulate**: `http://localhost:5002/api/neo/simulate` - Neo operations (POST)

## Authentication

**Current Status**: The working API service is currently **open for testing** and does not require authentication for basic endpoints. This allows for easy testing and development.

### ✅ **No Authentication Required (Current)**

All current endpoints are accessible without authentication:

```bash
# Direct access - no authentication needed
curl http://localhost:5002/health
curl http://localhost:5002/api/status
curl http://localhost:5002/api/database/test
curl http://localhost:5002/api/redis/test
```

### 🔒 **Future Authentication Support**

The service is designed to support multiple authentication methods when enabled:

- **JWT**: JSON Web Tokens for secure authentication
- **API Key**: Simple API key authentication
- **OAuth 2.0**: OAuth 2.0 for delegated authentication

### **Example with Authentication (Future)**

```http
GET /api/neo/version HTTP/1.1
Host: localhost:5002
Authorization: Bearer your-jwt-token
```

## Working API Endpoints

### ✅ **Currently Available Endpoints**

#### **System Health & Status**

```bash
# Health check
curl http://localhost:5002/health
# Response: "Healthy"

# Service status
curl http://localhost:5002/api/status
# Response: JSON with all service health status

# Service information
curl http://localhost:5002/
# Response: Service info with version and timestamp
```

#### **Infrastructure Testing**

```bash
# Database connectivity test
curl http://localhost:5002/api/database/test
# Response: PostgreSQL connection info and version

# Redis connectivity test
curl http://localhost:5002/api/redis/test
# Response: Redis connection success confirmation
```

#### **Neo Blockchain Integration**

```bash
# Neo version information
curl http://localhost:5002/api/neo/version
# Response: Neo 3.8.1 with supported features

# Neo operation simulation
curl -X POST http://localhost:5002/api/neo/simulate \
  -H "Content-Type: application/json" \
  -d '{"operation": "test", "parameters": {"key": "value"}}'
# Response: Simulation result with ID and status
```

#### **Testing & Development**

```bash
# Test POST endpoint
curl -X POST http://localhost:5002/api/test \
  -H "Content-Type: application/json" \
  -d '{"name": "Test User", "message": "Hello World"}'
# Response: Test response with processed data

# API documentation
curl http://localhost:5002/swagger
# Response: Swagger UI HTML (open in browser)
```

### 🔄 **Future Service Endpoints**

The following services are available in the codebase and ready for microservices deployment:

- [Randomness Service](../services/randomness-service.md)
- [Oracle Service](../services/oracle-service.md)
- [Key Management Service](../services/key-management-service.md)
- [Compute Service](../services/compute-service.md)
- [Storage Service](../services/storage-service.md)
- [Compliance Service](../services/compliance-service.md)
- [Event Subscription Service](../services/event-subscription-service.md)
- [Automation Service](../services/automation-service.md)
- [Cross-Chain Service](../services/cross-chain-service.md)
- [Proof of Reserve Service](../services/proof-of-reserve-service.md)

## Request & Response Examples

### **Working Request Examples**

#### **GET Requests**

```bash
# Health check
curl http://localhost:5002/health
# Response: "Healthy"

# Service status
curl http://localhost:5002/api/status
# Response: 
{
  "service": "Neo Service Layer",
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "services": {
    "database": "healthy",
    "redis": "healthy",
    "neo": "healthy"
  }
}

# Database test
curl http://localhost:5002/api/database/test
# Response:
{
  "Status": "Connected",
  "Version": "PostgreSQL 16.x on x86_64-pc-linux-gnu"
}
```

#### **POST Requests**

```bash
# Test endpoint
curl -X POST http://localhost:5002/api/test \
  -H "Content-Type: application/json" \
  -d '{"name": "Test User", "message": "Hello World"}'
# Response:
{
  "message": "Test endpoint working",
  "data": {
    "name": "Test User",
    "message": "Hello World"
  },
  "timestamp": "2024-01-01T12:00:00Z"
}

# Neo simulation
curl -X POST http://localhost:5002/api/neo/simulate \
  -H "Content-Type: application/json" \
  -d '{"operation": "test", "parameters": {"key": "value"}}'
# Response:
{
  "simulationId": "sim-123456",
  "status": "success",
  "result": {
    "operation": "test",
    "parameters": {"key": "value"},
    "processed": true
  }
}
```

### **Response Format**

The current API uses a **simplified response format** for easy testing and integration:

**Simple Success Response:**
```json
{
  "status": "success",
  "data": {
    // Response data
  },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

**Error Response:**
```json
{
  "status": "error",
  "message": "Error description",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Error Handling

### **Current Error Responses**

The working API service returns standard HTTP status codes:

- **200**: OK - Request successful
- **400**: Bad Request - Invalid request format
- **404**: Not Found - Endpoint not found
- **500**: Internal Server Error - Server error

### **Error Example**

```bash
# Request to non-existent endpoint
curl http://localhost:5002/api/nonexistent
# Response: 404 Not Found

# Invalid JSON in POST request
curl -X POST http://localhost:5002/api/test \
  -H "Content-Type: application/json" \
  -d 'invalid-json'
# Response: 400 Bad Request
```

### **Error Response Format**

```json
{
  "status": "error",
  "message": "Detailed error message",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Rate Limiting

**Current Status**: Rate limiting is **not currently enforced** in the working API service, allowing for unlimited testing and development.

**Future Implementation**: Rate limiting will be implemented with the following planned limits:
- **Requests per minute**: 100
- **Requests per hour**: 1,000
- **Concurrent requests**: 10

## API Documentation

### **Interactive Documentation**

The working API service provides **Swagger UI** for interactive API documentation:

```bash
# Access Swagger UI
open http://localhost:5002/swagger
```

**Features:**
- Interactive API testing
- Request/response examples
- Schema documentation
- Try-it-out functionality

### **API Versioning**

The current API uses a **simple versioning approach**:

```
http://localhost:5002/api/{endpoint}
```

**Future Versioning**: Will implement versioned endpoints:
```
http://localhost:5002/api/v1/{endpoint}
```

### **Content Types**

The API currently supports:
- **JSON**: `application/json` (primary)
- **Text**: `text/plain` (for simple responses)
- **HTML**: `text/html` (for Swagger UI)

### **Testing Tools**

**Command Line:**
```bash
# Using curl
curl http://localhost:5002/api/status

# Using wget
wget -qO- http://localhost:5002/health
```

**Programming Languages:**
```javascript
// JavaScript/Node.js
const response = await fetch('http://localhost:5002/api/status');
const data = await response.json();

// Python
import requests
response = requests.get('http://localhost:5002/api/status')
data = response.json()
```

## Integration Examples

### **Quick Integration**

**Check Service Health:**
```bash
curl http://localhost:5002/health
```

**Get Service Status:**
```bash
curl http://localhost:5002/api/status
```

**Test Database Connection:**
```bash
curl http://localhost:5002/api/database/test
```

### **Language Examples**

**JavaScript/Node.js:**
```javascript
const fetch = require('node-fetch');

async function checkHealth() {
  const response = await fetch('http://localhost:5002/health');
  const result = await response.text();
  console.log('Health:', result);
}

async function getStatus() {
  const response = await fetch('http://localhost:5002/api/status');
  const data = await response.json();
  console.log('Status:', data);
}
```

**Python:**
```python
import requests

def check_health():
    response = requests.get('http://localhost:5002/health')
    print('Health:', response.text)

def get_status():
    response = requests.get('http://localhost:5002/api/status')
    print('Status:', response.json())
```

**C#/.NET:**
```csharp
using System.Net.Http;
using System.Threading.Tasks;

public class NeoServiceClient
{
    private readonly HttpClient _httpClient;
    
    public NeoServiceClient()
    {
        _httpClient = new HttpClient();
    }
    
    public async Task<string> CheckHealthAsync()
    {
        var response = await _httpClient.GetAsync("http://localhost:5002/health");
        return await response.Content.ReadAsStringAsync();
    }
}
```

## References

- **[Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)** - How to deploy the service
- **[Quick Start Guide](../deployment/QUICK_START.md)** - Get started in 5 minutes
- **[Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md)** - System architecture
- **[Neo Documentation](https://docs.neo.org/)** - Neo blockchain documentation
- **[Swagger UI](http://localhost:5002/swagger)** - Interactive API documentation

---

**🎉 The Neo Service Layer API is fully operational and ready for integration!**
