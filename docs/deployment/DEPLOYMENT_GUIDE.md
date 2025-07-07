# Neo Service Layer Deployment Guide

This guide provides comprehensive instructions for deploying the Neo Service Layer in various environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Configuration](#environment-configuration)
- [Deployment Options](#deployment-options)
- [Production Deployment](#production-deployment)
- [Security Considerations](#security-considerations)
- [Monitoring and Maintenance](#monitoring-and-maintenance)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

- **Operating System**: Linux (Ubuntu 20.04+ recommended), Windows Server 2019+, or macOS
- **CPU**: Minimum 4 cores, 8 cores recommended for production
- **RAM**: Minimum 8GB, 16GB recommended for production
- **Storage**: 50GB minimum, SSD recommended
- **Network**: Stable internet connection with open ports as configured

### Software Requirements

- **.NET 8.0 SDK** or later
- **Docker** (optional, for containerized deployment)
- **PostgreSQL** 14+ or **SQLite** (for development)
- **Redis** 6+ (optional, for distributed caching)
- **Intel SGX SDK** (if using hardware enclaves)

### Blockchain Requirements

- **Neo N3 Node**: Access to Neo N3 RPC endpoint
- **Neo X Node**: Access to Neo X RPC endpoint
- **Contract Deployments**: Social Recovery and other contracts must be deployed

## Environment Configuration

### 1. Create Environment File

Copy the example environment file and configure it:

```bash
cp .env.example .env
```

### 2. Required Environment Variables

Edit `.env` with your production values:

```bash
# REQUIRED: JWT Configuration
JWT_SECRET_KEY=<generate-with-openssl-rand-base64-32>

# REQUIRED: Intel Attestation Service
IAS_API_KEY=<your-ias-api-key>

# REQUIRED for Production: Configuration Encryption
CONFIG_ENCRYPTION_KEY=<generate-with-openssl-rand-base64-32>

# SGX Mode
SGX_MODE=HW  # Use HW for production with real SGX hardware

# Blockchain Configuration
NEO_N3_RPC_URL=https://mainnet1.neo.coz.io:443
NEO_X_RPC_URL=https://mainnet.rpc.banelabs.org

# Social Recovery Contracts (update with your deployed addresses)
SOCIAL_RECOVERY_CONTRACT_NEO_N3=0xYourContractAddress
SOCIAL_RECOVERY_CONTRACT_NEO_X=0xYourContractAddress

# Database (for production)
CONNECTION_STRING=Host=localhost;Database=neoservicelayer;Username=neo;Password=<secure-password>

# Optional: Telemetry
OTEL_EXPORTER_JAEGER_ENDPOINT=http://jaeger:14268/api/traces
```

### 3. Generate Secure Keys

Generate required cryptographic keys:

```bash
# Generate JWT Secret Key
openssl rand -base64 32

# Generate Configuration Encryption Key
openssl rand -base64 32
```

### 4. Application Settings

Update `appsettings.Production.json` for production-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "NeoServiceLayer": "Information"
    }
  },
  "Security": {
    "RequireHttps": true
  },
  "RateLimit": {
    "ApiRateLimit": {
      "PermitLimit": 1000,
      "WindowMinutes": 1
    }
  }
}
```

## Deployment Options

### Option 1: Direct Deployment

1. **Build the application**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Run database migrations** (if using PostgreSQL):
   ```bash
   dotnet ef database update -p src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence
   ```

3. **Start the services**:
   ```bash
   cd publish
   dotnet NeoServiceLayer.Api.dll
   dotnet NeoServiceLayer.Web.dll
   ```

### Option 2: Docker Deployment

1. **Build Docker images**:
   ```bash
   docker build -t neoservicelayer-api:latest -f src/Api/NeoServiceLayer.Api/Dockerfile .
   docker build -t neoservicelayer-web:latest -f src/Web/NeoServiceLayer.Web/Dockerfile .
   ```

2. **Run with Docker Compose**:
   ```bash
   docker-compose -f docker-compose.production.yml up -d
   ```

### Option 3: Kubernetes Deployment

1. **Apply Kubernetes manifests**:
   ```bash
   kubectl apply -f k8s/namespace.yaml
   kubectl apply -f k8s/configmap.yaml
   kubectl apply -f k8s/secrets.yaml
   kubectl apply -f k8s/deployment.yaml
   kubectl apply -f k8s/service.yaml
   kubectl apply -f k8s/ingress.yaml
   ```

2. **Verify deployment**:
   ```bash
   kubectl get pods -n neo-service-layer
   kubectl get services -n neo-service-layer
   ```

## Production Deployment

### 1. Pre-Deployment Checklist

- [ ] All environment variables are set with production values
- [ ] SSL certificates are configured
- [ ] Database is provisioned and accessible
- [ ] Blockchain nodes are accessible
- [ ] Smart contracts are deployed and verified
- [ ] Backup strategy is in place
- [ ] Monitoring and alerting are configured
- [ ] Security audit has been performed

### 2. Deployment Steps

1. **Prepare the server**:
   ```bash
   # Update system
   sudo apt update && sudo apt upgrade -y
   
   # Install .NET runtime
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt update
   sudo apt install -y aspnetcore-runtime-8.0
   ```

2. **Configure firewall**:
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw enable
   ```

3. **Setup reverse proxy (Nginx)**:
   ```nginx
   server {
       listen 80;
       server_name api.yourservice.com;
       return 301 https://$server_name$request_uri;
   }

   server {
       listen 443 ssl;
       server_name api.yourservice.com;

       ssl_certificate /etc/ssl/certs/your-cert.pem;
       ssl_certificate_key /etc/ssl/private/your-key.pem;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

4. **Create systemd service**:
   ```ini
   [Unit]
   Description=Neo Service Layer API
   After=network.target

   [Service]
   Type=notify
   WorkingDirectory=/var/www/neoservicelayer
   ExecStart=/usr/bin/dotnet /var/www/neoservicelayer/NeoServiceLayer.Api.dll
   Restart=always
   RestartSec=10
   KillSignal=SIGINT
   SyslogIdentifier=neoservicelayer-api
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
   EnvironmentFile=/var/www/neoservicelayer/.env

   [Install]
   WantedBy=multi-user.target
   ```

5. **Start and enable services**:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable neoservicelayer-api
   sudo systemctl start neoservicelayer-api
   sudo systemctl status neoservicelayer-api
   ```

### 3. Health Verification

Verify the deployment is healthy:

```bash
# Check API health
curl https://api.yourservice.com/health

# Check specific service health
curl https://api.yourservice.com/health/ready

# Check metrics (if enabled)
curl https://api.yourservice.com/metrics
```

## Security Considerations

### 1. Network Security

- Use HTTPS with valid SSL certificates
- Configure firewall rules to restrict access
- Use VPN for administrative access
- Implement rate limiting and DDoS protection

### 2. Application Security

- Never commit secrets to version control
- Rotate JWT keys regularly
- Use strong passwords for all services
- Enable audit logging
- Implement proper CORS policies

### 3. SGX Security

- Use hardware mode (`SGX_MODE=HW`) in production
- Verify attestation reports
- Protect sealed storage keys
- Monitor enclave health

### 4. Database Security

- Use encrypted connections
- Implement proper access controls
- Regular backups with encryption
- Monitor for suspicious activity

## Monitoring and Maintenance

### 1. Logging

Configure centralized logging:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://elasticsearch:9200",
          "indexFormat": "neoservicelayer-{0:yyyy.MM.dd}"
        }
      }
    ]
  }
}
```

### 2. Metrics

Enable Prometheus metrics:

```csharp
// In Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();
        builder.AddMeter("NeoServiceLayer");
    });
```

### 3. Alerts

Configure alerting rules:

```yaml
# prometheus-rules.yml
groups:
  - name: neoservicelayer
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate detected
```

### 4. Backup Strategy

Implement regular backups:

```bash
# Backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/neoservicelayer"

# Database backup
pg_dump -h localhost -U neo -d neoservicelayer > "$BACKUP_DIR/db_$DATE.sql"

# Configuration backup
tar -czf "$BACKUP_DIR/config_$DATE.tar.gz" /var/www/neoservicelayer/appsettings.*.json

# Encrypt backups
gpg --encrypt --recipient backup@yourservice.com "$BACKUP_DIR/db_$DATE.sql"

# Upload to S3
aws s3 cp "$BACKUP_DIR/db_$DATE.sql.gpg" s3://your-backup-bucket/
```

## Troubleshooting

### Common Issues

1. **Service won't start**
   - Check logs: `journalctl -u neoservicelayer-api -f`
   - Verify environment variables are set
   - Check database connectivity

2. **Authentication failures**
   - Verify JWT_SECRET_KEY is set correctly
   - Check token expiration settings
   - Ensure clock synchronization

3. **Blockchain connectivity issues**
   - Test RPC endpoints manually
   - Check firewall rules
   - Verify node synchronization

4. **Performance issues**
   - Monitor CPU and memory usage
   - Check database query performance
   - Review rate limiting settings

### Debug Mode

Enable detailed logging for troubleshooting:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
```

### Support

For additional support:
- Check the [troubleshooting guide](../troubleshooting/README.md)
- Review [GitHub issues](https://github.com/your-org/neo-service-layer/issues)
- Contact the development team

## Next Steps

After successful deployment:

1. Configure monitoring dashboards
2. Set up automated backups
3. Schedule security audits
4. Plan for scaling strategy
5. Document operational procedures