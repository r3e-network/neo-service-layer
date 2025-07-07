# Neo Service Layer - Quick Start Guide

## 🚀 5-Minute Deployment

### 1. Prerequisites
```bash
# Check .NET version (8.0+ required)
dotnet --version

# Check Docker (optional)
docker --version
```

### 2. Configure Environment
```bash
# Copy and edit environment file
cp .env.example .env

# Generate secure keys
echo "JWT_SECRET_KEY=$(openssl rand -base64 32)" >> .env
echo "CONFIG_ENCRYPTION_KEY=$(openssl rand -base64 32)" >> .env
```

### 3. Quick Local Deployment
```bash
# Restore dependencies
dotnet restore

# Run API service
dotnet run --project src/Api/NeoServiceLayer.Api

# Run Web service (in another terminal)
dotnet run --project src/Web/NeoServiceLayer.Web
```

### 4. Docker Deployment
```bash
# Build and run with Docker Compose
docker-compose up -d

# Check status
docker-compose ps
```

## 📋 Essential Configuration

### Required Environment Variables
| Variable | Description | Example |
|----------|-------------|---------|
| `JWT_SECRET_KEY` | JWT signing key (32+ chars) | Generated with `openssl rand -base64 32` |
| `IAS_API_KEY` | Intel Attestation Service key | Get from Intel portal |
| `SGX_MODE` | SGX operation mode | `SW` for dev, `HW` for production |

### Blockchain Configuration
| Variable | Description | Default |
|----------|-------------|---------|
| `NEO_N3_RPC_URL` | Neo N3 RPC endpoint | http://localhost:20332 |
| `NEO_X_RPC_URL` | Neo X RPC endpoint | http://localhost:8545 |

## 🔍 Health Checks

```bash
# Check API health
curl http://localhost:5000/health

# Check specific services
curl http://localhost:5000/health/ready

# View API documentation
open http://localhost:5000/swagger
```

## 🛠️ Common Commands

### Development
```bash
# Run tests
dotnet test

# Build for production
dotnet publish -c Release

# Run with hot reload
dotnet watch run --project src/Api/NeoServiceLayer.Api
```

### Database
```bash
# Apply migrations
dotnet ef database update -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence

# Create new migration
dotnet ef migrations add MigrationName -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence
```

### Docker
```bash
# View logs
docker-compose logs -f

# Restart services
docker-compose restart

# Clean up
docker-compose down -v
```

## 📝 Configuration Files

### Development
- `appsettings.Development.json` - Development settings
- `.env` - Environment variables (not committed)

### Production
- `appsettings.Production.json` - Production settings
- `docker-compose.production.yml` - Production Docker config

## 🚨 Troubleshooting

### Service Won't Start
1. Check environment variables: `dotnet run -- --show-config`
2. Verify database connection
3. Check port availability: `lsof -i :5000`

### Authentication Issues
1. Verify JWT_SECRET_KEY is set
2. Check token expiration
3. Ensure Authorization header format: `Bearer <token>`

### Blockchain Connection Failed
1. Test RPC endpoint: `curl <RPC_URL>`
2. Check network connectivity
3. Verify blockchain is synced

## 📚 Next Steps

1. Read the [full deployment guide](DEPLOYMENT_GUIDE.md)
2. Configure [monitoring](../monitoring/README.md)
3. Review [security checklist](../security/SECURITY_CHECKLIST.md)
4. Set up [CI/CD pipeline](../.github/workflows/README.md)

## 🆘 Getting Help

- **Documentation**: `/docs`
- **API Reference**: `http://localhost:5000/swagger`
- **Issues**: [GitHub Issues](https://github.com/your-org/neo-service-layer/issues)
- **Support**: support@neoservicelayer.io