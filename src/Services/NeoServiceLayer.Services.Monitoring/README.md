# Neo Service Layer - Monitoring Service

## Overview

The Monitoring Service provides comprehensive monitoring, metrics collection, and observability for the entire Neo Service Layer ecosystem. It integrates with Prometheus, Grafana, and other monitoring tools to provide real-time insights into system performance and behavior.

## Features

- **Metrics Collection**: Collects and aggregates metrics from all services
- **Custom Metrics**: Support for business-specific metrics
- **Real-time Monitoring**: Live dashboards and alerts
- **Performance Tracking**: Response times, throughput, and resource usage
- **Distributed Tracing**: End-to-end request tracing with OpenTelemetry
- **Log Aggregation**: Centralized logging with correlation
- **Alert Management**: Intelligent alerting with multiple notification channels

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                 Monitoring Service                   │
├─────────────────────────────────────────────────────┤
│  API Layer                                          │
│  ├── Metrics Endpoints                              │
│  ├── Query API                                      │
│  └── Alert Management API                           │
├─────────────────────────────────────────────────────┤
│  Collection Engine                                  │
│  ├── Prometheus Scraper                             │
│  ├── Custom Metrics Collector                       │
│  └── OpenTelemetry Receiver                         │
├─────────────────────────────────────────────────────┤
│  Processing Layer                                   │
│  ├── Metric Aggregation                             │
│  ├── Anomaly Detection                              │
│  └── Alert Engine                                   │
├─────────────────────────────────────────────────────┤
│  Storage Layer                                      │
│  ├── Time Series Database                           │
│  ├── Log Storage                                    │
│  └── Trace Storage                                  │
└─────────────────────────────────────────────────────┘
```

## Configuration

### Environment Variables

```bash
# Service Configuration
SERVICE_NAME=monitoring-service
SERVICE_PORT=8091
LOG_LEVEL=Information

# Prometheus Configuration
PROMETHEUS_SCRAPE_INTERVAL=15s
PROMETHEUS_RETENTION=30d
PROMETHEUS_STORAGE_PATH=/data/prometheus

# OpenTelemetry Configuration
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=monitoring-service
ENABLE_TRACING=true

# Alert Configuration
ALERT_EVALUATION_INTERVAL=30s
ALERT_NOTIFICATION_CHANNELS=email,slack,webhook

# Storage Configuration
TIMESERIES_DB_URL=http://influxdb:8086
LOG_STORAGE_PATH=/data/logs
TRACE_STORAGE_BACKEND=jaeger

# Service Discovery
CONSUL_ENABLED=true
CONSUL_ADDRESS=http://consul:8500
```

## API Endpoints

### Base URL
```
https://api.neo-service-layer.com/v1/monitoring
```

### Metrics Endpoints

#### Export Metrics (Prometheus Format)
```http
GET /metrics
```

Returns metrics in Prometheus exposition format.

#### Query Metrics
```http
POST /metrics/query
Content-Type: application/json

{
  "metric": "http_requests_total",
  "service": "storage-service",
  "timeRange": {
    "start": "2025-01-10T00:00:00Z",
    "end": "2025-01-10T12:00:00Z"
  },
  "aggregation": "sum",
  "interval": "5m"
}
```

**Response:**
```json
{
  "metric": "http_requests_total",
  "values": [
    {
      "timestamp": "2025-01-10T00:00:00Z",
      "value": 15234
    },
    {
      "timestamp": "2025-01-10T00:05:00Z",
      "value": 16789
    }
  ]
}
```

#### Record Custom Metric
```http
POST /metrics/custom
Content-Type: application/json

{
  "name": "business_transactions_total",
  "value": 1,
  "labels": {
    "type": "payment",
    "status": "success"
  }
}
```

### Alert Management

#### Create Alert Rule
```http
POST /alerts/rules
Content-Type: application/json

{
  "name": "High Error Rate Alert",
  "expression": "rate(http_errors_total[5m]) > 0.05",
  "duration": "5m",
  "severity": "critical",
  "annotations": {
    "summary": "High error rate detected",
    "description": "Error rate is above 5% for {{ $labels.service }}"
  },
  "notifications": ["slack", "email"]
}
```

#### Get Active Alerts
```http
GET /alerts/active
```

**Response:**
```json
{
  "alerts": [
    {
      "id": "alert-123",
      "name": "High Error Rate Alert",
      "state": "firing",
      "service": "payment-service",
      "severity": "critical",
      "startedAt": "2025-01-10T10:00:00Z",
      "value": 0.08
    }
  ]
}
```

### Dashboards

#### Get Dashboard List
```http
GET /dashboards
```

#### Get Dashboard Configuration
```http
GET /dashboards/{dashboardId}
```

## Metrics Collection

### Default Metrics

#### HTTP Metrics
- `http_requests_total`: Total HTTP requests
- `http_request_duration_seconds`: Request duration histogram
- `http_errors_total`: Total HTTP errors by status code

#### System Metrics
- `process_cpu_usage`: CPU usage percentage
- `process_memory_bytes`: Memory usage in bytes
- `process_open_fds`: Number of open file descriptors

#### Business Metrics
- `neo_transactions_total`: Total blockchain transactions
- `neo_blocks_processed`: Number of blocks processed
- `neo_gas_consumed`: Total GAS consumed

### Custom Metrics

```csharp
// Counter example
private readonly Counter transactionCounter = Metrics
    .CreateCounter("neo_transactions_total", "Total number of transactions",
        new CounterConfiguration
        {
            LabelNames = new[] { "type", "status" }
        });

// Usage
transactionCounter.WithLabels("transfer", "success").Inc();

// Histogram example
private readonly Histogram requestDuration = Metrics
    .CreateHistogram("request_duration_seconds", "Request duration",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0.1, 0.1, 10)
        });

// Usage
using (requestDuration.NewTimer())
{
    // Process request
}
```

## Distributed Tracing

### OpenTelemetry Integration

```csharp
services.AddOpenTelemetryTracing(builder =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("monitoring-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddJaegerExporter();
});
```

### Trace Context Propagation

The service automatically propagates trace context across service boundaries using W3C Trace Context standard.

## Alert Configuration

### Alert Rules

```yaml
groups:
  - name: service_alerts
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: rate(http_errors_total[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate on {{ $labels.service }}
          
      - alert: ServiceDown
        expr: up == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: Service {{ $labels.service }} is down
```

### Notification Channels

1. **Email Notifications**
   ```json
   {
     "type": "email",
     "config": {
       "to": ["ops@company.com"],
       "from": "monitoring@neo-service-layer.com"
     }
   }
   ```

2. **Slack Integration**
   ```json
   {
     "type": "slack",
     "config": {
       "webhook": "https://hooks.slack.com/services/xxx",
       "channel": "#alerts"
     }
   }
   ```

3. **Webhook**
   ```json
   {
     "type": "webhook",
     "config": {
       "url": "https://alerts.company.com/webhook",
       "method": "POST"
     }
   }
   ```

## Grafana Integration

### Pre-built Dashboards

1. **System Overview**: Overall system health and performance
2. **Service Dashboard**: Per-service metrics and performance
3. **Infrastructure Dashboard**: Database, Redis, and message queue metrics
4. **Business Metrics**: Application-specific business metrics
5. **Alert Dashboard**: Alert history and trends

### Dashboard Provisioning

Dashboards are automatically provisioned through:
```yaml
apiVersion: 1
providers:
  - name: 'neo-service-layer'
    orgId: 1
    folder: 'Neo Service Layer'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    options:
      path: /var/lib/grafana/dashboards
```

## Performance Optimization

- **Metric Cardinality Management**: Limits on label combinations
- **Downsampling**: Automatic downsampling of old data
- **Query Optimization**: Indexed time-series queries
- **Batch Processing**: Batched metric writes
- **Caching**: Query result caching

## Troubleshooting

### Common Issues

1. **Missing Metrics**
   - Verify service is registered in service discovery
   - Check firewall rules for metrics endpoint
   - Ensure Prometheus can reach the service

2. **High Memory Usage**
   - Review metric cardinality
   - Adjust retention policies
   - Enable metric downsampling

3. **Alert Fatigue**
   - Review alert thresholds
   - Implement alert grouping
   - Use alert suppression during maintenance

## Security

- **Authentication**: API key or JWT authentication for all endpoints
- **Authorization**: Role-based access to metrics and alerts
- **Encryption**: TLS for all metric collection
- **Data Privacy**: PII scrubbing from metrics and logs
- **Audit Logging**: All configuration changes are logged

## License

This service is part of the Neo Service Layer and is licensed under the MIT License.