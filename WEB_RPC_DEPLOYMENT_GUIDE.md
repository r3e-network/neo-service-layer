# Neo Service Layer - Web & RPC Deployment Guide

## 🚀 Overview

The Neo Service Layer now includes a comprehensive web interface and JSON-RPC 2.0 server for enterprise blockchain infrastructure. This guide covers the deployment of both components for production-ready operation.

## 📋 Components Added

### 1. Enhanced Web Interface
- **Location**: `src/Web/NeoServiceLayer.Web/`
- **Features**: Modern responsive UI, service demos, API documentation
- **Port**: 5000 (HTTP), 5001 (HTTPS)
- **Endpoints**: 
  - `/` - Main landing page
  - `/ServicePages/ServiceDemo` - Interactive service demonstrations
  - `/swagger` - API documentation
  - `/health` - Health checks

### 2. JSON-RPC 2.0 Server
- **Location**: `src/RPC/NeoServiceLayer.RPC.Server/`
- **Features**: Standards-compliant JSON-RPC, real-time WebSocket, batch requests
- **Port**: 8027 (HTTP), 8080 (Container)
- **Endpoints**:
  - `/rpc` - JSON-RPC endpoint
  - `/rpc/methods` - Available methods
  - `/rpc/health` - RPC health check
  - `/ws` - WebSocket notifications

### 3. Enhanced Website Preview
- **Location**: `docs/web/website-preview.html`
- **Features**: Static preview with modern design, feature showcase

## 🔧 Deployment Options

### Option 1: Docker Compose (Recommended)

Deploy all services including web interface and RPC server:

```bash
# Generate secure credentials
./scripts/generate-secure-credentials.sh

# Deploy all services
docker-compose -f docker-compose.all-services.yml up -d

# Verify deployment
docker-compose -f docker-compose.all-services.yml ps
```

**Access Points:**
- Web Interface: http://localhost:5000
- RPC Server: http://localhost:8027
- API Gateway: http://localhost:7000
- Swagger UI: http://localhost:5000/swagger

### Option 2: Individual Service Deployment

#### Web Interface Only
```bash
cd src/Web/NeoServiceLayer.Web
dotnet run --environment Production
```

#### RPC Server Only
```bash
cd src/RPC/NeoServiceLayer.RPC.Server
dotnet run --environment Production
```

### Option 3: Production Build

```bash
# Build web interface
dotnet publish src/Web/NeoServiceLayer.Web -c Release -o ./publish/web

# Build RPC server
dotnet publish src/RPC/NeoServiceLayer.RPC.Server -c Release -o ./publish/rpc

# Deploy to your infrastructure
```

## ⚙️ Configuration

### Environment Variables

```bash
# JWT Configuration
export JWT_SECRET_KEY="your-secure-256-bit-key"
export JWT_ISSUER="neo-service-layer"
export JWT_AUDIENCE="neo-service-layer-clients"

# Database Configuration
export DB_HOST="localhost"
export DB_PORT="5432"
export DB_NAME="neo_service_layer"
export DB_USER="neo_service_user"
export DB_PASSWORD="your-secure-password"

# Redis Configuration
export REDIS_CONNECTION="localhost:6379"
export REDIS_PASSWORD="your-redis-password"

# Service Discovery
export CONSUL_ADDRESS="http://localhost:8500"
```

### Production Settings

#### Web Interface (`appsettings.Production.json`)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "JwtSettings": {
    "Issuer": "NeoServiceLayer",
    "Audience": "NeoServiceLayerUsers"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://api.yourdomain.com"
    ]
  },
  "Swagger": {
    "Enabled": false
  }
}
```

#### RPC Server Configuration
- Rate limiting: 100 requests per minute per client
- Authentication: JWT tokens required
- WebSocket: Real-time notifications supported
- Batch requests: Up to 100 operations per batch

## 🔐 Security Features

### Web Interface Security
- ✅ HTTPS enforcement in production
- ✅ JWT authentication with secure key management
- ✅ CORS protection with configurable origins
- ✅ Input validation and sanitization
- ✅ Rate limiting protection
- ✅ Security headers (HSTS, CSP, etc.)

### RPC Server Security
- ✅ JSON-RPC 2.0 compliant
- ✅ JWT authentication required
- ✅ Rate limiting (token bucket algorithm)
- ✅ Request size limits
- ✅ Method-level permissions
- ✅ WebSocket authentication

## 📊 Monitoring & Health Checks

### Health Check Endpoints
```bash
# Web Interface Health
curl http://localhost:5000/health

# RPC Server Health  
curl http://localhost:8027/rpc/health

# Individual Service Health
curl http://localhost:8001/health  # Service ports 8001-8026
```

### Monitoring Integration
- **Prometheus**: Metrics collection on `/metrics`
- **Grafana**: Dashboards for visualization
- **Jaeger**: Distributed tracing
- **Serilog**: Structured logging

## 🧪 Testing the Deployment

### 1. Web Interface Tests
```bash
# Test main page
curl -I http://localhost:5000/

# Test API documentation
curl -I http://localhost:5000/swagger

# Test health endpoint
curl http://localhost:5000/health
```

### 2. RPC Server Tests
```bash
# Get available methods
curl http://localhost:8027/rpc/methods

# Test RPC call (requires auth token)
curl -X POST http://localhost:8027/rpc \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "jsonrpc": "2.0",
    "method": "keymanagement.createkey",
    "params": {
      "keyId": "test-key",
      "keyType": "ECDSA",
      "keyUsage": "Signing",
      "exportable": false
    },
    "id": 1
  }'
```

### 3. WebSocket Connection Test
```javascript
const ws = new WebSocket('ws://localhost:8027/ws?access_token=YOUR_TOKEN');

ws.onopen = () => {
    console.log('Connected to Neo Service Layer WebSocket');
    
    // Subscribe to blockchain events
    ws.send(JSON.stringify({
        method: 'SubscribeToBlockchain',
        params: ['NeoN3']
    }));
};

ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    console.log('Received notification:', data);
};
```

## 🚀 API Usage Examples

### REST API Examples
```javascript
// Get demo token
const tokenResponse = await fetch('/api/auth/demo-token', { method: 'POST' });
const { token } = await tokenResponse.json();

// Use services
const response = await fetch('/api/keymanagement/generate', {
    method: 'POST',
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        keyId: 'my-key',
        keyType: 'ECDSA',
        keyUsage: 'Signing'
    })
});
```

### JSON-RPC Examples
```javascript
// Initialize RPC client
const rpc = new NeoRpcClient('/rpc');
rpc.setAuthToken(token);

// Single RPC call
const result = await rpc.call('keymanagement.createkey', {
    keyId: 'my-key',
    keyType: 'ECDSA',
    keyUsage: 'Signing',
    exportable: false
});

// Batch RPC calls
const results = await rpc.batch([
    { method: 'keymanagement.listkeys' },
    { method: 'storage.getdata', params: { key: 'my-data' } },
    { method: 'randomness.generatebytes', params: { length: 32 } }
]);
```

## 📈 Performance Considerations

### Web Interface
- Static asset caching enabled
- Gzip compression for responses
- CDN integration for Bootstrap/FontAwesome
- Lazy loading for service demonstrations

### RPC Server
- Connection pooling for database access
- Response caching for frequently accessed data
- Rate limiting to prevent abuse
- WebSocket connection management

## 🐛 Troubleshooting

### Common Issues

1. **Web Interface not accessible**
   ```bash
   # Check if service is running
   docker logs neo-web-interface
   
   # Verify port binding
   netstat -tlnp | grep :5000
   ```

2. **RPC calls failing**
   ```bash
   # Check RPC server logs
   docker logs neo-rpc-server
   
   # Verify JWT token
   echo "YOUR_TOKEN" | base64 -d
   ```

3. **WebSocket connection issues**
   - Verify authentication token in URL
   - Check firewall rules for WebSocket traffic
   - Ensure proper CORS configuration

### Log Analysis
```bash
# Web interface logs
tail -f src/Web/NeoServiceLayer.Web/logs/*.txt

# RPC server logs  
tail -f src/RPC/NeoServiceLayer.RPC.Server/logs/*.txt

# Container logs
docker-compose -f docker-compose.all-services.yml logs -f web-interface rpc-server
```

## 📚 Additional Resources

- **API Documentation**: Available at `/swagger` when web interface is running
- **RPC Method List**: Available at `/rpc/methods`
- **Service Health**: Check `/health` endpoints for all services
- **WebSocket Events**: Real-time notifications for blockchain and service events

## 🎯 Production Checklist

- [ ] Environment variables configured securely
- [ ] HTTPS certificates installed and configured
- [ ] Database connections secured and encrypted
- [ ] JWT secret keys generated and rotated
- [ ] CORS origins configured for production domains
- [ ] Rate limiting tuned for expected traffic
- [ ] Monitoring and alerting configured
- [ ] Log rotation and retention policies set
- [ ] Backup and disaster recovery procedures tested
- [ ] Security scanning completed
- [ ] Load testing performed
- [ ] Documentation updated for operations team

---

🎉 **Congratulations!** Your Neo Service Layer web interface and RPC server are now production-ready with enterprise-grade security, monitoring, and scalability features.