# Health Service

## Overview
The Health Service provides comprehensive system health diagnostics and reporting for the Neo Service Layer. It monitors node health, consensus mechanisms, network metrics, and provides alerting capabilities to ensure optimal system performance and reliability.

## Features

### Node Monitoring
- **Real-time Health Checks**: Continuous monitoring of blockchain nodes
- **Performance Metrics**: CPU, memory, disk, and network utilization
- **Consensus Participation**: Track node participation in consensus
- **Connectivity Status**: Monitor peer connections and network health
- **Version Tracking**: Monitor node software versions and updates

### Network Health
- **Consensus Health**: Monitor consensus algorithm performance
- **Block Propagation**: Track block propagation times across network
- **Transaction Pool**: Monitor mempool health and congestion
- **Network Partitions**: Detect and alert on network partitions
- **Fork Detection**: Identify and track blockchain forks

### Alerting System
- **Threshold-based Alerts**: Configurable thresholds for various metrics
- **Smart Alerting**: Intelligent alert filtering to reduce noise
- **Alert Escalation**: Multi-level alert escalation policies
- **Integration**: Integration with external monitoring systems
- **Recovery Tracking**: Monitor system recovery after incidents

### Historical Analysis
- **Health Trends**: Long-term health trend analysis
- **Performance History**: Historical performance data
- **Incident Tracking**: Track and analyze past incidents
- **Capacity Planning**: Data for capacity planning decisions

## API Endpoints

### Node Health
- `GET /api/health/nodes` - List all monitored nodes
- `GET /api/health/nodes/{address}` - Get specific node health
- `POST /api/health/nodes/register` - Register node for monitoring
- `DELETE /api/health/nodes/{address}` - Unregister node

### Network Health
- `GET /api/health/consensus` - Get consensus health status
- `GET /api/health/network` - Get overall network metrics
- `GET /api/health/blocks` - Get block propagation metrics
- `GET /api/health/forks` - Get active fork information

### Alerts
- `GET /api/health/alerts` - List active alerts
- `GET /api/health/alerts/{id}` - Get alert details
- `POST /api/health/alerts/acknowledge` - Acknowledge alert
- `POST /api/health/thresholds` - Set alert thresholds

### Historical Data
- `GET /api/health/history/{metric}` - Get historical health data
- `GET /api/health/reports/summary` - Get health summary report
- `GET /api/health/incidents` - List past incidents

## Configuration

```json
{
  "Health": {
    "Monitoring": {
      "Interval": "00:00:30",
      "Timeout": "00:00:10",
      "RetryAttempts": 3
    },
    "Thresholds": {
      "CpuUsage": 80,
      "MemoryUsage": 85,
      "DiskUsage": 90,
      "ResponseTime": "00:00:05"
    },
    "Alerts": {
      "EnableEmailAlerts": true,
      "EnableSlackIntegration": true,
      "AlertCooldown": "00:05:00"
    },
    "History": {
      "RetentionDays": 90,
      "SampleInterval": "00:01:00"
    }
  }
}
```

## Usage Examples

### Registering a Node for Monitoring
```csharp
var registration = new NodeRegistrationRequest
{
    Address = "http://node1.example.com:10332",
    Name = "Main Neo Node",
    NodeType = NodeType.Consensus,
    AlertContacts = new[] { "admin@example.com" },
    CustomThresholds = new HealthThreshold
    {
        CpuThreshold = 75,
        MemoryThreshold = 80,
        ResponseTimeThreshold = TimeSpan.FromSeconds(3)
    }
};

await healthService.RegisterNodeForMonitoringAsync(registration, BlockchainType.Neo3);
```

### Getting Node Health Status
```csharp
var nodeHealth = await healthService.GetNodeHealthAsync("http://node1.example.com:10332", BlockchainType.Neo3);

Console.WriteLine($"Node Status: {nodeHealth.Status}");
Console.WriteLine($"CPU Usage: {nodeHealth.CpuUsage}%");
Console.WriteLine($"Memory Usage: {nodeHealth.MemoryUsage}%");
Console.WriteLine($"Last Block: {nodeHealth.LastBlockHeight}");
```

### Monitoring Consensus Health
```csharp
var consensusHealth = await healthService.GetConsensusHealthAsync(BlockchainType.Neo3);

Console.WriteLine($"Active Validators: {consensusHealth.ActiveValidators}");
Console.WriteLine($"Block Time Average: {consensusHealth.AverageBlockTime}");
Console.WriteLine($"Consensus Participation: {consensusHealth.ParticipationRate}%");
```

### Setting Custom Thresholds
```csharp
var thresholds = new HealthThreshold
{
    CpuThreshold = 85,
    MemoryThreshold = 90,
    DiskThreshold = 95,
    ResponseTimeThreshold = TimeSpan.FromSeconds(5),
    BlockHeightLag = 2
};

await healthService.SetHealthThresholdAsync("http://node1.example.com:10332", thresholds, BlockchainType.Neo3);
```

## Health Metrics

### Node-Level Metrics
- **System Resources**: CPU, memory, disk usage
- **Network**: Peer connections, bandwidth utilization
- **Blockchain**: Block height, sync status, transaction throughput
- **Application**: Service status, response times, error rates

### Network-Level Metrics
- **Consensus**: Validator participation, view changes, timeouts
- **Performance**: Block times, transaction confirmation times
- **Connectivity**: Network topology, partition detection
- **Security**: Fork detection, double-spend attempts

### Service-Level Metrics
- **Availability**: Service uptime and responsiveness
- **Performance**: Request latency, throughput
- **Errors**: Error rates and types
- **Resources**: Service resource utilization

## Alert Types

### Critical Alerts
- Node offline or unreachable
- Consensus failure or long view changes
- Network partition detected
- Security incidents (forks, attacks)

### Warning Alerts
- High resource utilization
- Slow block propagation
- Increased error rates
- Performance degradation

### Information Alerts
- Node version updates available
- Maintenance windows
- Performance trends
- Capacity recommendations

## Integration

The Health Service integrates with:
- **All Neo Services**: Monitor health of all platform services
- **Monitoring Service**: Provide health data for system monitoring
- **Notification Service**: Send health alerts and reports
- **Configuration Service**: Dynamic health monitoring configuration
- **Logging Service**: Correlate health data with system logs

## Advanced Features

### Predictive Health Monitoring
- Machine learning-based anomaly detection
- Predictive failure analysis
- Capacity planning recommendations
- Performance trend analysis

### Custom Health Checks
- Service-specific health check implementations
- Custom metric collection
- Business logic health validation
- External dependency health checks

### Integration Capabilities
- Prometheus metrics export
- Grafana dashboard integration
- PagerDuty alert integration
- Slack/Teams notifications

## Best Practices

1. **Comprehensive Monitoring**: Monitor all critical system components
2. **Appropriate Thresholds**: Set realistic and actionable alert thresholds
3. **Regular Review**: Regularly review and update monitoring configurations
4. **Incident Response**: Establish clear incident response procedures
5. **Trend Analysis**: Use historical data for trend analysis and planning
6. **Documentation**: Document health monitoring procedures and thresholds

## Error Handling

Common error scenarios:
- `NodeUnreachable`: Target node is not accessible
- `TimeoutError`: Health check request timed out
- `InvalidThreshold`: Threshold values are invalid
- `MonitoringDisabled`: Health monitoring is disabled for the node
- `InsufficientData`: Not enough data for health assessment

## Performance Considerations

- Health checks are performed asynchronously to minimize impact
- Historical data is aggregated and sampled to reduce storage requirements
- Alert processing is throttled to prevent spam
- Monitoring intervals can be adjusted based on system requirements
- Resource usage monitoring may add overhead to monitored systems

## Monitoring and Metrics

The service provides metrics for:
- Health check success/failure rates
- Alert generation and resolution times
- System performance trends
- Monitoring system performance
- Historical health data quality