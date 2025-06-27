# Neo Service Layer - Docker Hub

The Neo Service Layer is now available on Docker Hub for easy deployment and distribution.

## 🐳 Docker Hub Repository

**Repository:** [`jinghuiliao/neo-service-layer`](https://hub.docker.com/r/jinghuiliao/neo-service-layer)

## 📦 Available Tags

- `latest` - Latest stable build
- `v1.0.0` - Version 1.0.0 release

## 🚀 Quick Start

### Pull and Run

```bash
# Pull the latest image
docker pull jinghuiliao/neo-service-layer:latest

# Run the container
docker run -d \
  --name neo-service-layer \
  -p 5000:5000 \
  -p 5001:5001 \
  -e NEO_ALLOW_SGX_SIMULATION=true \
  -e JWT_SECRET_KEY="your-secret-key-here" \
  jinghuiliao/neo-service-layer:latest
```

### Using Docker Compose

**Development Environment:**
```bash
# Quick development setup
docker-compose -f docker-compose.dev.yml up -d

# Access at http://localhost:8080
```

**Production Environment:**
```bash
# Set your JWT secret
export JWT_SECRET_KEY="your-secure-production-secret-key"

# Start production services
docker-compose up -d

# Access at http://localhost:5000 (HTTP) or https://localhost:5001 (HTTPS)
```

**Custom Configuration:**
```yaml
version: '3.8'
services:
  neo-service-layer:
    image: jinghuiliao/neo-service-layer:latest
    container_name: neo-service-layer
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      - NEO_ALLOW_SGX_SIMULATION=true
      - JWT_SECRET_KEY=your-secret-key-here
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - neo-data:/app/data
      - neo-logs:/var/log/neo-service-layer
    restart: unless-stopped

volumes:
  neo-data:
  neo-logs:
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `NEO_ALLOW_SGX_SIMULATION` | Enable SGX simulation mode | `false` | No |
| `JWT_SECRET_KEY` | JWT signing secret | - | Yes |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` | No |
| `ASPNETCORE_URLS` | Binding URLs | `http://+:5000;https://+:5001` | No |

### Ports

- `5000` - HTTP endpoint
- `5001` - HTTPS endpoint (requires SSL certificate)

### Volumes

- `/app/data` - Application data storage
- `/var/log/neo-service-layer` - Application logs
- `/app/config` - Configuration files

## 🏥 Health Check

The container includes a built-in health check endpoint:

```bash
# Check container health
curl http://localhost:5000/health

# Expected response: "Healthy"
```

## 🔍 Verification

### Test the Deployment

```bash
# 1. Pull and run the container
docker run -d \
  --name neo-test \
  -p 5000:5000 \
  -e NEO_ALLOW_SGX_SIMULATION=true \
  -e JWT_SECRET_KEY="test-secret-key" \
  jinghuiliao/neo-service-layer:latest

# 2. Wait for startup (30-60 seconds)
sleep 60

# 3. Test health endpoint
curl http://localhost:5000/health

# 4. Check logs
docker logs neo-test

# 5. Cleanup
docker stop neo-test && docker rm neo-test
```

## 📊 Image Information

- **Base Image:** `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Size:** ~1.31GB
- **Architecture:** linux/amd64
- **Includes:**
  - .NET 9.0 Runtime
  - Intel SGX SDK (simulation mode)
  - Rust toolchain for enclave components
  - Protocol Buffers compiler
  - Complete Neo Service Layer application

## 🔐 Security Features

- Non-root user execution (`neoservice` user)
- Intel SGX support for trusted execution
- JWT-based authentication
- HTTPS support with configurable certificates
- Minimal attack surface with production-optimized runtime

## 🛠️ Development

### Local Scripts

The repository includes convenient scripts for local development:

```bash
# Use Docker Hub image for development
./scripts/run-docker-dev.sh

# Use Docker Hub image for production
./scripts/run-docker-hub.sh

# Build and run locally (if you have the source)
./scripts/build-docker.sh
```

### Docker Compose Options

```bash
# Development environment (port 8080)
docker-compose -f docker-compose.dev.yml up -d

# Production environment (ports 5000/5001)
export JWT_SECRET_KEY="your-production-secret"
docker-compose up -d

# View logs
docker-compose logs -f neo-service-layer

# Stop services
docker-compose down
```

### Building Locally

If you want to build the image locally instead of using Docker Hub:

```bash
# Clone the repository
git clone <repository-url>
cd neo-service-layer

# Build the complete Docker image
docker build -f docker/Dockerfile -t neo-service-layer:local .

# Run locally built image
docker run -d \
  --name neo-local \
  -p 5000:5000 \
  -e NEO_ALLOW_SGX_SIMULATION=true \
  -e JWT_SECRET_KEY="local-secret-key" \
  neo-service-layer:local
```

## 📚 Additional Resources

- [Docker Documentation](docs/DOCKER.md) - Complete Docker setup guide

## 🐛 Troubleshooting

### Common Issues

1. **Container fails to start**
   - Ensure `JWT_SECRET_KEY` environment variable is set
   - Check if ports 5000/5001 are available
   - Verify sufficient system resources (1GB+ RAM)

2. **Health check fails**
   - Wait 60+ seconds for application startup
   - Check container logs: `docker logs <container-name>`
   - Verify environment variables are correctly set

3. **SGX-related errors**
   - Set `NEO_ALLOW_SGX_SIMULATION=true` for non-SGX systems
   - Ensure proper SGX drivers for hardware mode

### Getting Help

- Check container logs: `docker logs <container-name>`
- Verify configuration: `docker inspect <container-name>`
- Test connectivity: `curl http://localhost:5000/health`

## 📝 Changelog

### v1.0.0 (2025-06-27)
- Initial Docker Hub release
- Complete Neo Service Layer with all components
- Intel SGX simulation support
- Production-ready configuration
- Comprehensive health checks and monitoring