# Distributed Tracing with OpenTelemetry

## Overview

The Neo Service Layer microservices architecture now includes comprehensive distributed tracing using OpenTelemetry and Jaeger. This enables end-to-end visibility across all service calls.

## Architecture

### Components

1. **Jaeger** - Distributed tracing backend
   - UI: http://localhost:16686
   - OTLP receiver: port 4317 (gRPC), 4318 (HTTP)
   - Query API: port 16686

2. **OpenTelemetry Instrumentation**
   - Added to API Gateway
   - Added to Storage Service
   - Can be added to all other services

3. **Trace Propagation**
   - W3C Trace Context headers
   - Automatic context propagation through HTTP calls

### Services with Tracing

Currently instrumented:
- API Gateway (api-gateway)
- Storage Service (storage-service)

## Configuration

### Service Configuration

Each service requires OpenTelemetry packages and configuration:

```csharp
// OpenTelemetry packages
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.10.0" />

// Configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(serviceName)
            .AddOtlpExporter();
    });
```

### Environment Variables

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
OTEL_SERVICE_NAME=<service-name>
OTEL_TRACES_EXPORTER=otlp
```

## Testing Distributed Tracing

### 1. Verify Services are Running

```bash
docker ps | grep -E "(jaeger|api-gateway|storage)"
```

### 2. Make API Calls

```bash
# Get auth token
TOKEN=$(curl -s http://localhost:5200/api/auth/login \
  -X POST -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r .token)

# Make traced calls
curl -s http://localhost:5200/api/storage/stats \
  -H "Authorization: Bearer $TOKEN"
```

### 3. View Traces in Jaeger

1. Open http://localhost:16686
2. Select a service from the dropdown
3. Click "Find Traces"
4. Click on a trace to see the distributed call flow

### 4. Query Traces via API

```bash
# List services
curl -s http://localhost:16686/api/services | jq

# Get recent traces
curl -s "http://localhost:16686/api/traces?service=api-gateway&limit=5" | jq

# Get specific trace
curl -s "http://localhost:16686/api/traces/<trace-id>" | jq
```

## Custom Instrumentation

### Adding Custom Spans

```csharp
var activitySource = new ActivitySource(serviceName);

using var activity = activitySource.StartActivity("CustomOperation", ActivityKind.Server);
activity?.SetTag("custom.tag", "value");
activity?.SetTag("operation.type", "database");

// Do work...

activity?.SetStatus(ActivityStatusCode.Ok);
```

### Recording Exceptions

```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.AddException(ex); // Note: RecordException is deprecated
    throw;
}
```

## Trace Context Propagation

The trace context is automatically propagated through HTTP headers:
- `traceparent`: W3C trace context
- `tracestate`: W3C trace state
- `X-Correlation-ID`: Custom correlation ID

## Performance Considerations

1. **Sampling**: Configure sampling rates for high-volume services
2. **Batching**: Traces are batched before sending to reduce overhead
3. **Filtering**: Filter out health checks and metrics endpoints

## Next Steps

1. Add OpenTelemetry to remaining services:
   - Authentication Service
   - Notification Service
   - Configuration Service
   - Health Monitoring Service

2. Configure sampling strategies
3. Set up trace-based alerting
4. Integrate with APM dashboards

## Troubleshooting

### No Traces Appearing

1. Check service logs for OTLP export errors
2. Verify Jaeger is running: `docker logs neo-jaeger`
3. Check network connectivity between services

### Missing Spans

1. Ensure all services have OpenTelemetry configured
2. Verify trace context propagation in HTTP headers
3. Check for exceptions in span creation

### Performance Impact

1. Monitor CPU/memory usage with tracing enabled
2. Adjust batch size and export interval
3. Configure appropriate sampling rates