# Neo Service Layer - Health Service

## Overview

The Health Service monitors and reports the health status of all Neo Service Layer components, providing real-time health checks, dependency mapping, and automated alerting capabilities.

## Features

- **Real-time Health Monitoring**: Continuous monitoring of all service components
- **Dependency Health Tracking**: Monitors health of dependent services and infrastructure
- **Custom Health Checks**: Extensible framework for service-specific health validations
- **Health History**: Tracks health status over time for trend analysis
- **Automated Alerts**: Configurable alerts for health degradation
- **Health Aggregation**: Provides overall system health score

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Health Service                     │
├─────────────────────────────────────────────────────┤
│  API Layer                                          │
│  ├── Health Check Endpoints                         │
│  ├── Metrics Endpoints                              │
│  └── Alert Configuration                            │
├─────────────────────────────────────────────────────┤
│  Health Check Engine                                │
│  ├── Service Health Checks                          │
│  ├── Infrastructure Health Checks                   │
│  └── Custom Health Checks                           │
├─────────────────────────────────────────────────────┤
│  Data Layer                                         │
│  ├── Health History Storage                         │
│  ├── Alert Configuration Storage                    │
│  └── Metrics Storage                                │
└─────────────────────────────────────────────────────┘
```

## Configuration

### Environment Variables

```bash
# Service Configuration
SERVICE_NAME=health-service
SERVICE_PORT=8090
LOG_LEVEL=Information

# Health Check Configuration
HEALTH_CHECK_INTERVAL=30
HEALTH_CHECK_TIMEOUT=10
HEALTH_CHECK_FAILURE_THRESHOLD=3

# Database Configuration
DB_CONNECTION_STRING=Host=postgres;Database=neo_health;Username=neo_user

# Redis Configuration
REDIS_CONNECTION=redis:6379

# Alert Configuration
ENABLE_ALERTS=true
ALERT_EMAIL=ops@neo-service-layer.com
ALERT_WEBHOOK=https://alerts.company.com/webhook

# Service Discovery
CONSUL_ENABLED=true
CONSUL_ADDRESS=http://consul:8500
```

## API Endpoints

### Base URL
```
https://api.neo-service-layer.com/v1/health
```

### Health Status Endpoints

#### Overall Health
```http
GET /health
```

Returns the aggregated health status of the entire system.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-10T10:00:00Z",
  "services": {
    "storage-service": "Healthy",
    "oracle-service": "Healthy",
    "key-management": "Degraded"
  },
  "infrastructure": {
    "database": "Healthy",
    "redis": "Healthy",
    "rabbitmq": "Healthy"
  },
  "overallScore": 95
}
```

#### Liveness Check
```http
GET /health/live
```

Basic liveness check for Kubernetes.

#### Readiness Check
```http
GET /health/ready
```

Readiness check including dependency verification.

#### Service-Specific Health
```http
GET /health/services/{serviceName}
```

Get detailed health information for a specific service.

**Response:**
```json
{
  "serviceName": "storage-service",
  "status": "Healthy",
  "lastChecked": "2025-01-10T10:00:00Z",
  "uptime": "48h30m",
  "checks": {
    "database": "Healthy",
    "diskSpace": "Healthy",
    "memoryUsage": "Healthy"
  },
  "metrics": {
    "responseTime": 45,
    "errorRate": 0.01,
    "throughput": 1500
  }
}
```

### Health History

#### Get Health History
```http
GET /health/history?service={serviceName}&from={timestamp}&to={timestamp}
```

Retrieve health history for analysis.

### Alert Configuration

#### Configure Alerts
```http
POST /health/alerts
Content-Type: application/json

{
  "name": "High Error Rate",
  "condition": "errorRate > 0.05",
  "severity": "critical",
  "notifications": ["email", "webhook"]
}
```

## Health Check Types

### 1. Service Health Checks
- HTTP endpoint availability
- Response time validation
- Error rate monitoring
- Resource usage checks

### 2. Infrastructure Health Checks
- Database connectivity
- Redis availability
- Message queue status
- Disk space monitoring
- Memory usage monitoring

### 3. Custom Health Checks
- Business logic validation
- Data consistency checks
- Integration health verification

## Monitoring Integration

### Prometheus Metrics
```
# Service health status
health_service_status{service="storage-service"} 1

# Health check duration
health_check_duration_seconds{service="storage-service",check="database"} 0.045

# Overall system health score
system_health_score 95
```

### Grafana Dashboard
The Health Service provides a comprehensive Grafana dashboard showing:
- System health overview
- Service health matrix
- Health trends over time
- Alert history

## Development

### Adding Custom Health Checks

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform custom health check logic
            var isHealthy = await CheckCustomLogicAsync();
            
            return isHealthy 
                ? HealthCheckResult.Healthy("Custom check passed")
                : HealthCheckResult.Unhealthy("Custom check failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception during health check", ex);
        }
    }
}
```

### Registering Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom_check")
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddRedis("redis")
    .AddRabbitMQ("rabbitmq");
```

## Troubleshooting

### Common Issues

1. **Health Check Timeouts**
   - Increase `HEALTH_CHECK_TIMEOUT` value
   - Check network connectivity to dependent services
   - Review service response times

2. **False Positives**
   - Adjust `HEALTH_CHECK_FAILURE_THRESHOLD`
   - Review health check criteria
   - Check for transient network issues

3. **Missing Health Data**
   - Verify service registration in Consul
   - Check health endpoint accessibility
   - Review firewall rules

## Security

- Health endpoints are protected by API authentication
- Sensitive health data is encrypted in transit
- Access control for alert configuration
- Audit logging for all health check activities

## Performance Considerations

- Health checks are performed asynchronously
- Results are cached for 30 seconds by default
- Parallel health checks for improved performance
- Configurable check intervals to reduce load

## License

This service is part of the Neo Service Layer and is licensed under the MIT License.