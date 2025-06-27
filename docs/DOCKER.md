# Docker Guide for Neo Service Layer

This guide provides comprehensive instructions for building and running the Neo Service Layer API using Docker.

## Quick Start

### 1. Build the Application
```bash
# Build and publish the .NET application
dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release -o src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/
```

### 2. Build the Docker Image
```bash
# Build the minimal Docker image (recommended for development/testing)
sudo docker build -f docker/Dockerfile.minimal -t neo-service-layer:minimal .
```

### 3. Run the Container
```bash
# Run in Development mode (HTTP only, with Swagger UI)
sudo docker run -d -p 8080:5000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS="http://+:5000" \
  -e JWT_SECRET_KEY="your-secure-jwt-secret-key-at-least-32-characters-long" \
  --name neo-service-layer \
  neo-service-layer:minimal
```

### 4. Test the Application
```bash
# Health check
curl http://localhost:8080/health

# API info
curl http://localhost:8080/api/info

# Swagger UI (Development mode only)
# Open http://localhost:8080 in your browser
```

## Available Docker Files

### 1. `docker/Dockerfile.minimal` (Recommended)
- **Purpose**: Lightweight container using pre-built application
- **Build Time**: ~4 seconds
- **Size**: Minimal (runtime only)
- **Use Case**: Development, testing, quick deployments

**Advantages:**
- Fast build times
- Smaller image size
- Uses pre-built published output
- No build dependencies in container

**Requirements:**
- Application must be published locally first
- Suitable for CI/CD pipelines where build happens separately

### 2. `docker/Dockerfile.simple`
- **Purpose**: Simplified build without Rust/SGX components
- **Build Time**: ~2-3 minutes
- **Use Case**: Standard .NET deployment without advanced features

### 3. `docker/Dockerfile.fixed`
- **Purpose**: Enhanced version with better NuGet handling
- **Build Time**: ~3-4 minutes
- **Use Case**: When experiencing NuGet package issues

### 4. `docker/Dockerfile` (Original)
- **Purpose**: Full-featured build with SGX and Rust support
- **Build Time**: 10+ minutes (may fail without protobuf-compiler)
- **Use Case**: Production with all advanced features

## Environment Configurations

### Development Mode
```bash
sudo docker run -d -p 8080:5000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS="http://+:5000" \
  -e JWT_SECRET_KEY="your-secure-jwt-secret-key-at-least-32-characters-long" \
  --name neo-service-layer-dev \
  neo-service-layer:minimal
```

**Features:**
- HTTP only (no SSL certificate required)
- Swagger UI enabled at root path (`/`)
- Detailed error messages
- Development logging

### Production Mode
```bash
sudo docker run -d -p 8080:5000 -p 8443:5001 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JWT_SECRET_KEY="your-production-jwt-secret-key" \
  -e SSL_CERT_PATH="/etc/ssl/certs/neo-service-layer.pfx" \
  -e SSL_CERT_PASSWORD="your-certificate-password" \
  -v /path/to/your/cert.pfx:/etc/ssl/certs/neo-service-layer.pfx:ro \
  --name neo-service-layer-prod \
  neo-service-layer:minimal
```

**Features:**
- Both HTTP (port 5000) and HTTPS (port 5001)
- SSL certificate required
- Production logging
- Enhanced security headers

## Required Environment Variables

### Essential Variables
- `JWT_SECRET_KEY`: JWT signing key (minimum 32 characters)
- `ASPNETCORE_ENVIRONMENT`: Environment (Development/Production)
- `ASPNETCORE_URLS`: Override default URLs if needed

### Production Variables
- `SSL_CERT_PATH`: Path to SSL certificate file
- `SSL_CERT_PASSWORD`: SSL certificate password
- `DATABASE_CONNECTION_STRING`: Database connection
- `REDIS_CONNECTION_STRING`: Redis connection

### Optional Variables
- `CORS_ALLOWED_ORIGINS`: Allowed CORS origins
- `NEO_N3_RPC_URL`: Neo N3 blockchain RPC URL
- `NEO_X_RPC_URL`: Neo X blockchain RPC URL
- `SGX_MODE`: SGX mode (HW/SW/SIM)

## Build Scripts

### Quick Build Script
Create `scripts/build-docker.sh`:
```bash
#!/bin/bash
set -e

echo "Building .NET application..."
dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release -o src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/

echo "Building Docker image..."
sudo docker build -f docker/Dockerfile.minimal -t neo-service-layer:latest .

echo "Docker image built successfully!"
echo "Run with: sudo docker run -d -p 8080:5000 -e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS=\"http://+:5000\" -e JWT_SECRET_KEY=\"your-secure-jwt-secret-key-at-least-32-characters-long\" --name neo-service-layer neo-service-layer:latest"
```

### Development Run Script
Create `scripts/run-docker-dev.sh`:
```bash
#!/bin/bash
set -e

# Stop and remove existing container
sudo docker stop neo-service-layer 2>/dev/null || true
sudo docker rm neo-service-layer 2>/dev/null || true

# Run new container
sudo docker run -d -p 8080:5000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS="http://+:5000" \
  -e JWT_SECRET_KEY="development-jwt-secret-key-for-testing-only-32chars" \
  --name neo-service-layer \
  neo-service-layer:latest

echo "Container started successfully!"
echo "Health check: curl http://localhost:8080/health"
echo "API info: curl http://localhost:8080/api/info"
echo "Swagger UI: http://localhost:8080"
```

## Troubleshooting

### Common Issues

#### 1. SSL Certificate Error in Production
**Error**: `Could not find a part of the path '${SSL_CERT_PATH:-/etc/ssl/certs/neo-service-layer.pfx}'`

**Solution**: Run in Development mode or provide SSL certificate:
```bash
# Option 1: Development mode (no SSL)
-e ASPNETCORE_ENVIRONMENT=Development -e ASPNETCORE_URLS="http://+:5000"

# Option 2: Provide SSL certificate
-e SSL_CERT_PATH="/path/to/cert.pfx" -v /host/path/cert.pfx:/path/to/cert.pfx:ro
```

#### 2. JWT Secret Key Error
**Error**: `JWT secret key must be configured via JWT_SECRET_KEY environment variable`

**Solution**: Provide a secure JWT secret key:
```bash
-e JWT_SECRET_KEY="your-secure-jwt-secret-key-at-least-32-characters-long"
```

#### 3. Port Already in Use
**Error**: `bind: address already in use`

**Solution**: Use different ports or stop conflicting services:
```bash
# Use different port
-p 8081:5000

# Or stop existing container
sudo docker stop neo-service-layer
```

#### 4. Build Failures with Original Dockerfile
**Error**: Rust build hangs or fails

**Solution**: Use the minimal Dockerfile approach:
```bash
# Build application first
dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release -o src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/

# Use minimal Dockerfile
sudo docker build -f docker/Dockerfile.minimal -t neo-service-layer:minimal .
```

### Health Checks

#### Container Health
```bash
# Check if container is running
sudo docker ps | grep neo-service-layer

# Check container logs
sudo docker logs neo-service-layer

# Check container resource usage
sudo docker stats neo-service-layer
```

#### Application Health
```bash
# Health endpoint
curl http://localhost:8080/health

# API info endpoint
curl http://localhost:8080/api/info

# Check response time
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:8080/health
```

## Performance Optimization

### Image Size Optimization
- Use `docker/Dockerfile.minimal` for smallest images
- Multi-stage builds separate build and runtime dependencies
- `.dockerignore` excludes unnecessary files

### Runtime Optimization
```bash
# Limit memory usage
--memory=1g --memory-swap=1g

# Limit CPU usage
--cpus=1.0

# Set restart policy
--restart=unless-stopped
```

### Production Deployment
```bash
sudo docker run -d \
  --name neo-service-layer-prod \
  --restart=unless-stopped \
  --memory=2g \
  --cpus=2.0 \
  -p 80:5000 \
  -p 443:5001 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JWT_SECRET_KEY="${JWT_SECRET_KEY}" \
  -e SSL_CERT_PATH="/etc/ssl/certs/neo-service-layer.pfx" \
  -e SSL_CERT_PASSWORD="${SSL_CERT_PASSWORD}" \
  -v /etc/ssl/certs/neo-service-layer.pfx:/etc/ssl/certs/neo-service-layer.pfx:ro \
  neo-service-layer:latest
```

## Security Considerations

### Environment Variables
- Never hardcode secrets in Dockerfiles
- Use Docker secrets or external secret management
- Rotate JWT keys regularly

### Network Security
- Use HTTPS in production
- Implement proper firewall rules
- Consider using Docker networks for service isolation

### Container Security
- Run containers as non-root user (already configured)
- Keep base images updated
- Scan images for vulnerabilities
- Use minimal base images

## Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Build and Test Docker Image
  run: |
    # Build application
    dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release -o src/Api/NeoServiceLayer.Api/bin/Release/net9.0/publish/
    
    # Build Docker image
    docker build -f docker/Dockerfile.minimal -t neo-service-layer:${{ github.sha }} .
    
    # Test the image
    docker run -d -p 8080:5000 \
      -e ASPNETCORE_ENVIRONMENT=Development \
      -e ASPNETCORE_URLS="http://+:5000" \
      -e JWT_SECRET_KEY="test-jwt-secret-key-for-ci-pipeline-32chars" \
      --name test-container \
      neo-service-layer:${{ github.sha }}
    
    # Wait for startup
    sleep 10
    
    # Health check
    curl -f http://localhost:8080/health
    
    # Cleanup
    docker stop test-container
    docker rm test-container
```

This Docker setup provides a robust, scalable foundation for deploying the Neo Service Layer API in various environments.