# Neo Service Layer - Monitoring and Observability Guide

## Overview

This guide documents the comprehensive monitoring and observability system implemented for the Neo Service Layer platform. The system provides structured logging, distributed tracing, metrics collection, and performance monitoring capabilities.

## Table of Contents

1. [Architecture](#architecture)
2. [Components](#components)
3. [Configuration](#configuration)
4. [Usage](#usage)
5. [API Endpoints](#api-endpoints)
6. [Dashboards](#dashboards)
7. [Alerting](#alerting)
8. [Troubleshooting](#troubleshooting)

## Architecture

The observability stack consists of:

```
┌─────────────────────────────────────────────┐
│            Application Layer                 │
│  ┌─────────────────────────────────────┐   │
│  │   Structured Logging (Serilog)      │   │
│  │   Correlation IDs                   │   │
│  │   Performance Metrics               │   │
│  └─────────────────────────────────────┘   │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │   OpenTelemetry Instrumentation     │   │
│  │   - Traces                          │   │
│  │   - Metrics                         │   │
│  │   - Logs                            │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────┐
│            Collection Layer                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │  OTLP    │  │  Jaeger  │  │  Prom    │ │
│  │ Exporter │  │ Exporter │  │ Exporter │ │
│  └──────────┘  └──────────┘  └──────────┘ │
└─────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────┐
│            Storage & Visualization           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │  Jaeger  │  │Prometheus│  │  Grafana │ │
│  └──────────┘  └──────────┘  └──────────┘ │
└─────────────────────────────────────────────┘
```

## Components

### 1. Structured Logging

**Location**: `src/Infrastructure/NeoServiceLayer.Infrastructure.Observability/Logging/StructuredLogger.cs`

Features:
- Correlation ID tracking across requests
- Hierarchical logging with child loggers
- Automatic context enrichment
- Integration with OpenTelemetry spans

Usage:
```csharp
var logger = _loggerFactory.CreateLogger("ServiceName", correlationId);
logger.LogOperation("OperationName", new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Action"] = "CreateOrder"
});
```

### 2. Correlation ID Middleware

**Location**: `src/Api/NeoServiceLayer.Api/Middleware/CorrelationIdMiddleware.cs`

Features:
- Automatic correlation ID generation
- Header propagation (X-Correlation-Id)
- W3C Trace Context support
- Request/response logging

### 3. OpenTelemetry Integration

**Location**: `src/Infrastructure/NeoServiceLayer.Infrastructure.Observability/Telemetry/OpenTelemetryConfiguration.cs`

Instrumentation:
- ASP.NET Core (HTTP requests)
- HttpClient (outbound HTTP)
- Entity Framework Core (database)
- Custom spans for SGX operations

### 4. Performance Monitoring

**Location**: `src/Api/NeoServiceLayer.Api/Middleware/PerformanceMonitoringMiddleware.cs`

Metrics collected:
- Request duration (with percentiles)
- Memory allocation per request
- Error rates by endpoint
- Response time distribution

### 5. Observability API

**Location**: `src/Api/NeoServiceLayer.Api/Controllers/ObservabilityController.cs`

Endpoints:
- Performance statistics
- Error rates
- Health overview
- Trace queries
- Log queries

## Configuration

### appsettings.json

```json
{
  "Telemetry": {
    "TracingEnabled": true,
    "MetricsEnabled": true,
    "LoggingEnabled": true,
    "SamplingRatio": 1.0,
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc",
    "JaegerEndpoint": "localhost:6831",
    "PrometheusEnabled": true
  },
  "PerformanceThresholds": {
    "SlowRequestThresholdMs": 1000,
    "CriticalRequestThresholdMs": 5000,
    "MemoryDeltaThresholdKB": 10240,
    "ErrorRateThreshold": 0.05
  },
  "Logging": {
    "FilePath": "logs/neoservicelayer-.txt",
    "SeqUrl": "http://localhost:5341",
    "ApplicationInsights": {
      "InstrumentationKey": "your-key-here"
    }
  }
}
```

### Environment Variables

```bash
# OpenTelemetry
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=NeoServiceLayer
OTEL_SERVICE_VERSION=1.0.0

# Jaeger
JAEGER_AGENT_HOST=localhost
JAEGER_AGENT_PORT=6831

# Performance
PERF_SLOW_REQUEST_MS=1000
PERF_ERROR_RATE_THRESHOLD=0.05
```

## Usage

### Program.cs Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add observability services
builder.Services.AddObservability(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();

// Use observability middleware
app.UseObservability();
```

### Service Integration

```csharp
public class MyService
{
    private readonly IStructuredLogger _logger;
    
    public MyService(IStructuredLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("MyService");
    }
    
    public async Task<Result> ProcessAsync(Request request)
    {
        using var scope = _logger.BeginScope("ProcessRequest", new Dictionary<string, object>
        {
            ["RequestId"] = request.Id
        });
        
        try
        {
            // Process request
            var result = await DoWork();
            
            _logger.LogMetric("processing.duration", stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "ProcessRequest");
            throw;
        }
    }
}
```

## API Endpoints

### Performance Metrics

```http
GET /api/v1/observability/metrics/performance/{endpoint}?periodMinutes=60
```

Response:
```json
{
  "count": 1000,
  "successCount": 950,
  "errorCount": 50,
  "averageDuration": 150.5,
  "minDuration": 10,
  "maxDuration": 5000,
  "p50Duration": 100,
  "p95Duration": 500,
  "p99Duration": 2000,
  "errorRate": 0.05,
  "successRate": 0.95
}
```

### Error Rate

```http
GET /api/v1/observability/metrics/error-rate/{endpoint}?periodMinutes=5
```

### Health Overview

```http
GET /api/v1/observability/health/overview
```

Response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-15T10:00:00Z",
  "components": [
    {
      "name": "API",
      "status": "Healthy",
      "responseTimeMs": 50
    },
    {
      "name": "SGX Enclave",
      "status": "Healthy",
      "responseTimeMs": 100
    }
  ],
  "metrics": {
    "cpuUsagePercent": 35.5,
    "memoryUsageMB": 512,
    "activeConnections": 42,
    "requestsPerSecond": 150,
    "averageResponseTimeMs": 75
  }
}
```

### Traces

```http
GET /api/v1/observability/traces?limit=100&correlationId=abc123
```

### Logs

```http
GET /api/v1/observability/logs?limit=100&level=Error&correlationId=abc123
```

## Dashboards

### Grafana Dashboards

1. **System Overview**
   - Request rate
   - Error rate
   - Response time (P50, P95, P99)
   - Active connections

2. **Performance Dashboard**
   - Request duration histogram
   - Memory usage over time
   - CPU utilization
   - GC metrics

3. **SGX Enclave Dashboard**
   - Enclave operations/sec
   - Attestation success rate
   - Sealing/unsealing performance
   - Memory usage within enclave

4. **Business Metrics**
   - Transaction volume
   - Smart contract executions
   - API usage by endpoint
   - User activity patterns

### Prometheus Queries

```promql
# Request rate
rate(neoservicelayer_requests_total[5m])

# Error rate
rate(neoservicelayer_requests_total{status_code=~"5.."}[5m])
/ 
rate(neoservicelayer_requests_total[5m])

# P95 latency
histogram_quantile(0.95, 
  rate(neoservicelayer_request_duration_bucket[5m]))

# Memory usage
neoservicelayer_memory_usage

# Active connections
neoservicelayer_active_connections
```

## Alerting

### Alert Rules

```yaml
groups:
  - name: neoservicelayer
    rules:
      - alert: HighErrorRate
        expr: rate(neoservicelayer_requests_total{status_code=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value | humanizePercentage }}"
      
      - alert: SlowResponses
        expr: histogram_quantile(0.95, rate(neoservicelayer_request_duration_bucket[5m])) > 1000
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Slow response times"
          description: "P95 latency is {{ $value }}ms"
      
      - alert: HighMemoryUsage
        expr: neoservicelayer_memory_usage > 2048
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High memory usage"
          description: "Memory usage is {{ $value }}MB"
      
      - alert: SGXEnclaveDown
        expr: up{job="sgx-enclave"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "SGX Enclave is down"
          description: "SGX Enclave has been down for more than 1 minute"
```

### Alert Channels

Configure in your alerting system:
- Email notifications
- Slack/Teams webhooks
- PagerDuty integration
- SMS for critical alerts

## Troubleshooting

### Common Issues

1. **Missing Correlation IDs**
   - Ensure CorrelationIdMiddleware is registered early in pipeline
   - Check header propagation in HTTP clients

2. **No Traces Appearing**
   - Verify OTLP endpoint is accessible
   - Check sampling ratio (default 1.0 = 100%)
   - Ensure ActivitySource names match configuration

3. **High Memory Usage**
   - Review metrics history retention
   - Check for memory leaks in custom instrumentation
   - Adjust batch export settings

4. **Performance Degradation**
   - Reduce sampling ratio if needed
   - Use batch exporters instead of simple
   - Disable console exporters in production

### Debug Commands

```bash
# Check OpenTelemetry collector status
curl http://localhost:13133/

# View Prometheus metrics
curl http://localhost:9090/metrics

# Test Jaeger connectivity
curl http://localhost:16686/api/traces?service=NeoServiceLayer

# Check application metrics endpoint
curl http://localhost:5000/metrics
```

### Log Analysis

```bash
# Search for errors by correlation ID
grep "CorrelationId\":\"abc123" logs/*.json | jq '.Level == "Error"'

# Count errors by endpoint
cat logs/*.json | jq -r 'select(.Level == "Error") | .Properties.Endpoint' | sort | uniq -c

# Find slow requests
cat logs/*.json | jq -r 'select(.Properties.ElapsedMs > 1000) | "\(.Properties.Endpoint): \(.Properties.ElapsedMs)ms"'
```

## Best Practices

1. **Always use correlation IDs** for tracing requests across services
2. **Log at appropriate levels** (Debug in dev, Info in production)
3. **Include context** in log messages (user ID, request ID, etc.)
4. **Monitor key business metrics** not just technical metrics
5. **Set realistic thresholds** based on baseline performance
6. **Regular review** of alerts to reduce noise
7. **Document incidents** and link to relevant traces/logs
8. **Test monitoring** in staging before production

## Security Considerations

1. **Never log sensitive data** (passwords, keys, PII)
2. **Sanitize log outputs** to prevent injection
3. **Secure monitoring endpoints** with authentication
4. **Encrypt metrics in transit** (TLS for all exporters)
5. **Limit retention** of detailed logs (GDPR compliance)
6. **Access control** for dashboards and alerts
7. **Audit log access** for compliance

---

*Last Updated: January 2025*
*Version: 1.0.0*