# Health Check Endpoints

The Neo Service Layer provides comprehensive health check endpoints for monitoring service availability and performance.

## Endpoints

### Main Health Check
**GET** `/health`

Returns detailed health status for all registered health checks.

```json
{
  "status": "Healthy",
  "totalDuration": 125.5,
  "checks": {
    "self": {
      "status": "Healthy",
      "duration": 0.1,
      "description": "Service is running"
    },
    "blockchain": {
      "status": "Healthy",
      "duration": 15.2,
      "data": {
        "totalChains": 2,
        "healthyChains": 2,
        "chainStatus": {
          "NeoN3": { "isHealthy": true },
          "NeoX": { "isHealthy": true }
        }
      }
    },
    "storage": {
      "status": "Healthy",
      "duration": 5.8
    },
    "security-services": {
      "status": "Healthy",
      "duration": 20.1,
      "data": {
        "services": {
          "NetworkSecurity": true,
          "Compliance": true,
          "Attestation": true
        },
        "healthyCount": 3,
        "totalCount": 3
      }
    }
  },
  "timestamp": "2024-01-15T10:30:45Z"
}
```

### Ready Check
**GET** `/health/ready`

Returns whether the service is ready to handle requests.

- **200 OK**: Service is ready
- **503 Service Unavailable**: Service is not ready

### Live Check
**GET** `/health/live`

Returns whether the service is alive (basic liveness probe).

- **200 OK**: Service is alive
- **503 Service Unavailable**: Service is not responding

## Health Check Categories

### 1. Infrastructure Checks

#### Blockchain Health
- Checks connectivity to Neo N3 and Neo X nodes
- Verifies block height retrieval
- Reports individual chain status

#### Storage Health
- Tests read/write operations
- Verifies data integrity
- Checks storage service availability

#### Resource Health
- Monitors memory usage
- Tracks thread count
- Reports handle count
- Alerts on high resource usage (>70% warning, >90% critical)

### 2. Service Health Checks

#### Security Services
- **Network Security**: Firewall and threat detection status
- **Compliance**: Rule engine availability
- **Attestation**: SGX enclave verification

#### Blockchain Services
- **Voting**: Council node connectivity
- **Cross-Chain**: Bridge availability
- **Smart Contracts**: Contract deployment service
- **Proof of Reserve**: Asset verification service

#### Data Services
- **Event Subscription**: Event listener status
- **Notification**: Channel availability
- **Randomness**: Entropy generation

#### Advanced Services
- **Abstract Account**: Account abstraction service
- **Social Recovery**: Guardian management
- **Zero Knowledge**: Proof system availability

### 3. Configuration & Core Services
- Configuration service availability
- Overall Neo service health status
- SGX mode and enclave status

## Health Status Values

- **Healthy**: All checks passed, service is fully operational
- **Degraded**: Some non-critical checks failed, service is partially operational
- **Unhealthy**: Critical checks failed, service may not be operational

## Monitoring Integration

### Prometheus Metrics
Health check results are exposed as Prometheus metrics at `/metrics`:

```
health_check_status{name="blockchain"} 1
health_check_status{name="storage"} 1
health_check_status{name="security_services"} 0
health_check_duration_seconds{name="blockchain"} 0.015
```

### Kubernetes Probes
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Docker Compose Health Check
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost/health/live"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Alerting Rules

### Critical Alerts
- Any service in "Unhealthy" state for > 2 minutes
- Memory usage > 90%
- All blockchain connections down

### Warning Alerts
- Any service in "Degraded" state for > 5 minutes
- Memory usage > 70%
- Single blockchain connection down

## Custom Health Checks

To add a custom health check:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform health check logic
            var isHealthy = await CheckServiceHealthAsync();
            
            return isHealthy 
                ? HealthCheckResult.Healthy("Service is healthy")
                : HealthCheckResult.Unhealthy("Service is unhealthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom", tags: new[] { "ready" });
```

## Best Practices

1. **Set appropriate timeouts** for health checks to prevent hanging
2. **Use tags** to group related health checks
3. **Include relevant data** in health check results for debugging
4. **Monitor trends** not just current status
5. **Set up alerts** for prolonged degraded states
6. **Test health checks** regularly to ensure they work correctly

## Troubleshooting

### Health Check Timing Out
- Check network connectivity
- Verify service dependencies are running
- Review timeout settings

### False Positives
- Adjust thresholds for resource checks
- Ensure health check logic matches actual service requirements
- Check for transient network issues

### Missing Health Data
- Verify service registration in DI container
- Check for exceptions in health check implementation
- Enable debug logging for health checks