# Neo Service Layer - Docker Hub Deployment Guide

The Neo Service Layer Docker images have been successfully published to Docker Hub!

## Available Images

- **Latest**: `r3enetwork/neo-service-layer:latest`
- **Version 1.0.0**: `r3enetwork/neo-service-layer:v1.0.0`

## Quick Start

### 1. Pull the Image

```bash
docker pull r3enetwork/neo-service-layer:latest
```

### 2. Run with Docker Compose

Use the provided `docker-compose.dockerhub.yml` file:

```bash
# Download the docker-compose file
wget https://raw.githubusercontent.com/your-repo/neo-service-layer/main/docker-compose.dockerhub.yml

# Set JWT secret key (important for production!)
export JWT_SECRET_KEY=$(openssl rand -base64 32)

# Start the services
docker-compose -f docker-compose.dockerhub.yml up -d

# Check status
docker-compose -f docker-compose.dockerhub.yml ps

# View logs
docker-compose -f docker-compose.dockerhub.yml logs -f neo-service-layer
```

### 3. Run Standalone (Basic)

```bash
docker run -d \
  --name neo-service-layer \
  -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  r3enetwork/neo-service-layer:latest
```

## Accessing the Service

Once running, you can access:

- **Web Interface**: http://localhost:5000
- **API Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Services Status**: http://localhost:5000/services

## Environment Variables

Key environment variables you can configure:

```bash
# Database Connection
ConnectionStrings__DefaultConnection=Host=postgres;Database=neoservice;Username=neouser;Password=yourpassword

# Redis Configuration
Redis__Configuration=redis:6379

# JWT Authentication
Jwt__SecretKey=YourSecureRandomKey123!
Jwt__Issuer=neo-service-layer
Jwt__Audience=neo-service-layer-clients
Jwt__ExpiryMinutes=60

# SGX Mode (SIM for simulation, HW for hardware)
SGX_MODE=SIM
```

## Production Deployment

For production deployment:

1. **Use secure passwords** for database and Redis
2. **Generate a strong JWT secret key**
3. **Enable HTTPS** with proper certificates
4. **Configure proper resource limits**
5. **Enable monitoring and logging**

Example production docker-compose configuration:

```yaml
version: '3.8'

services:
  neo-service-layer:
    image: r3enetwork/neo-service-layer:v1.0.0
    deploy:
      resources:
        limits:
          cpus: '4'
          memory: 4G
        reservations:
          cpus: '2'
          memory: 2G
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8443
      # Add your production settings here
    volumes:
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro
      - ./certificates:/app/certificates:ro
    restart: always
```

## Troubleshooting

### Check Container Logs
```bash
docker logs neo-service-layer
```

### Verify Health Status
```bash
curl http://localhost:5000/health
```

### Access Container Shell
```bash
docker exec -it neo-service-layer /bin/sh
```

### Common Issues

1. **Port Already in Use**: Change the port mapping in docker-compose.yml
2. **Database Connection Failed**: Ensure PostgreSQL is running and accessible
3. **JWT Validation Error**: Generate a secure JWT key and set it properly

## Image Details

- **Base**: .NET 9.0 Alpine Linux (minimal footprint)
- **Size**: ~580MB
- **Architecture**: linux/amd64
- **Features**: 
  - Multi-blockchain support (Neo N3, Neo X)
  - Intel SGX simulation mode enabled
  - Comprehensive service layer with 30+ microservices
  - Built-in health checks and monitoring

## Security Notes

This image includes:
- Non-root user execution
- Health check endpoints
- JWT authentication support
- SGX simulation mode (for testing without SGX hardware)

For production with real SGX hardware, additional configuration is required.

## Support

For issues or questions:
- GitHub Issues: [Your Repository URL]
- Docker Hub: https://hub.docker.com/r/r3enetwork/neo-service-layer

## Version History

- **v1.0.0** (2025-01-07): Initial release with core services