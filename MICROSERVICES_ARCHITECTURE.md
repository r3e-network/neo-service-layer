# Neo Service Layer - Scalable Microservices Architecture

This branch contains the scalable microservices implementation of Neo Service Layer where each service runs in its own Docker container with automatic service discovery and registration.

## Architecture Overview

### Key Features

1. **Service Discovery & Registration**
   - Consul-based service registry
   - Automatic service registration on startup
   - Health checking and heartbeat monitoring
   - Dynamic service discovery

2. **API Gateway**
   - YARP-based reverse proxy
   - Dynamic routing based on service discovery
   - Load balancing across service instances
   - Centralized entry point for all services

3. **Containerized Services**
   - Each service runs in its own Docker container
   - Lightweight Alpine Linux base images
   - Individual scaling capabilities
   - Resource isolation

4. **Infrastructure Components**
   - PostgreSQL for persistent storage
   - Redis for caching
   - RabbitMQ for message queuing
   - Consul for service discovery
   - Prometheus & Grafana for monitoring

## Quick Start

### Prerequisites

- Docker 20.10+
- Docker Compose 2.0+
- 8GB RAM minimum (16GB recommended)
- 20GB free disk space

### Deployment

1. **Clone and checkout the branch**
   ```bash
   git clone <repository>
   cd neo-service-layer
   git checkout feature/scalable-microservices-architecture
   ```

2. **Deploy all services**
   ```bash
   ./scripts/deploy-microservices.sh deploy
   ```

3. **Check service status**
   ```bash
   ./scripts/deploy-microservices.sh status
   ```

4. **View logs**
   ```bash
   # All services
   ./scripts/deploy-microservices.sh logs
   
   # Specific service
   ./scripts/deploy-microservices.sh logs notification-service
   ```

## Service URLs

- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## Available Services

### Core Services
- **Notification Service** - Multi-channel notifications (Email, SMS, Webhook)
- **Configuration Service** - Centralized configuration management
- **Backup Service** - Backup and restore functionality
- **Storage Service** - Distributed storage operations

### Blockchain Services
- **Smart Contracts Service** - Smart contract deployment and interaction
- **Cross-Chain Service** - Cross-chain operations and bridging
- **Oracle Service** - External data feed integration
- **Proof of Reserve Service** - Asset reserve attestations

### Security Services
- **Key Management Service** - Cryptographic key management
- **Zero Knowledge Service** - ZK proof generation/verification
- **Compliance Service** - Regulatory compliance checks
- **Abstract Account Service** - ERC-4337 account abstraction

### Infrastructure Services
- **Monitoring Service** - System monitoring and metrics
- **Health Service** - Health checks and status monitoring
- **Automation Service** - Task automation and scheduling
- **Event Subscription Service** - Event handling and subscriptions

## Scaling Services

Scale individual services based on load:

```bash
# Scale notification service to 3 instances
./scripts/deploy-microservices.sh scale notification-service 3

# Scale smart contracts service to 5 instances
./scripts/deploy-microservices.sh scale smart-contracts-service 5
```

## Adding a New Service

1. **Create service implementation**
   ```csharp
   // src/Services/NeoServiceLayer.Services.YourService/YourService.cs
   public class YourService : ServiceBase, IYourService
   {
       // Implementation
   }
   ```

2. **Create service host**
   ```csharp
   // src/Services/NeoServiceLayer.Services.YourService/Program.cs
   public class Program
   {
       public static async Task<int> Main(string[] args)
       {
           var host = new YourServiceHost(args);
           return await host.RunAsync();
       }
   }
   ```

3. **Generate Dockerfile**
   ```bash
   # Add service name to generate-service-dockerfiles.sh
   # Run the script
   ./scripts/generate-service-dockerfiles.sh
   ```

4. **Add to docker-compose**
   ```yaml
   your-service:
     build:
       context: .
       dockerfile: docker/microservices/services/your-service/Dockerfile
     <<: *service-defaults
     environment:
       SERVICE_NAME: YourService
       SERVICE_TYPE: YourType
   ```

5. **Deploy**
   ```bash
   ./scripts/deploy-microservices.sh deploy
   ```

## Service Communication

Services communicate through:

1. **HTTP/REST APIs** - Direct service-to-service calls
2. **Message Queue** - Asynchronous messaging via RabbitMQ
3. **Service Discovery** - Dynamic endpoint resolution via Consul

Example service call:
```csharp
// Discover and call another service
var notificationServices = await _serviceRegistry.DiscoverServicesAsync("Notification");
var service = notificationServices.FirstOrDefault(s => s.Status == ServiceStatus.Healthy);

if (service != null)
{
    var client = _httpClientFactory.CreateClient();
    var response = await client.PostAsJsonAsync(
        $"{service.Protocol}://{service.HostName}:{service.Port}/api/notification/send",
        notificationRequest);
}
```

## Monitoring

### Metrics
- Service health status
- Request rates and latencies
- Resource utilization
- Error rates

### Dashboards
Access Grafana at http://localhost:3000 for pre-configured dashboards:
- Service Overview
- API Gateway Metrics
- Infrastructure Health
- Business Metrics

### Alerts
Configure alerts in Grafana for:
- Service downtime
- High error rates
- Resource exhaustion
- Performance degradation

## Configuration

### Environment Variables
Services are configured via environment variables:

```bash
# Common variables
ASPNETCORE_ENVIRONMENT=Production
Consul__Address=http://consul:8500
ConnectionStrings__DefaultConnection=...
Redis__Configuration=redis:6379

# Service-specific
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
```

### Secrets Management
Sensitive data is managed through:
- Docker secrets
- Environment variables
- Consul KV store
- AWS Secrets Manager (optional)

## Production Deployment

### Kubernetes
Deploy to Kubernetes using Helm charts:
```bash
helm install neo-service-layer ./charts/neo-service-layer \
  --namespace neo \
  --values ./charts/neo-service-layer/values.prod.yaml
```

### Docker Swarm
Deploy to Docker Swarm:
```bash
docker stack deploy -c docker-compose.swarm.yml neo-service-layer
```

### Cloud Platforms
- **AWS ECS**: Use provided task definitions
- **Azure Container Instances**: Use ARM templates
- **Google Cloud Run**: Deploy individual services

## Troubleshooting

### Service Not Registering
1. Check Consul connectivity
2. Verify SERVICE_NAME and SERVICE_TYPE env vars
3. Check service logs for registration errors

### Service Discovery Issues
1. Verify Consul is running
2. Check service health endpoints
3. Review Consul UI for service status

### Performance Issues
1. Check resource allocation
2. Review service logs for bottlenecks
3. Scale affected services
4. Monitor metrics in Grafana

## Development

### Local Development
```bash
# Run infrastructure only
docker-compose -f docker-compose.microservices.yml up consul postgres redis rabbitmq

# Run specific service locally
cd src/Services/NeoServiceLayer.Services.Notification
dotnet run
```

### Testing
```bash
# Unit tests
dotnet test

# Integration tests with containers
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### Debugging
1. Enable debug logging in appsettings.Development.json
2. Use Visual Studio Docker debugging
3. Attach to running containers

## Best Practices

1. **Service Design**
   - Keep services small and focused
   - Use async/await throughout
   - Implement circuit breakers
   - Add comprehensive logging

2. **Container Optimization**
   - Use multi-stage builds
   - Minimize image layers
   - Run as non-root user
   - Include health checks

3. **Scalability**
   - Design for horizontal scaling
   - Use caching effectively
   - Implement rate limiting
   - Monitor resource usage

4. **Security**
   - Use HTTPS between services
   - Implement authentication/authorization
   - Rotate secrets regularly
   - Keep base images updated

## License

[Your License]

## Support

For issues or questions:
- GitHub Issues: [Repository URL]
- Documentation: [Docs URL]
- Community: [Discord/Slack]