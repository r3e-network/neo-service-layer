# Observability API Specifications

## Overview

This document provides comprehensive OpenAPI specifications for observability and monitoring APIs in the Neo Service Layer platform. These APIs provide metrics, tracing, health checks, and performance monitoring capabilities for comprehensive system observability.

## Base Configuration

```yaml
openapi: 3.0.3
info:
  title: Neo Service Layer Observability API
  description: |
    Comprehensive observability API providing metrics, tracing, health monitoring,
    and performance analysis for the Neo Service Layer platform. Includes
    OpenTelemetry integration and real-time monitoring capabilities.
  version: "2.0.0"
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT
  contact:
    name: Neo Observability Team
    email: monitoring@neo.org

servers:
  - url: https://api.neo-service-layer.com/v2
    description: Production server
  - url: https://staging.neo-service-layer.com/v2
    description: Staging server
  - url: http://localhost:5000
    description: Development server

security:
  - BearerAuth: []
  - ApiKeyAuth: []

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
    ApiKeyAuth:
      type: apiKey
      in: header
      name: X-API-Key
```

## Health Monitoring API

### System Health Check

#### GET /api/health

Primary health check endpoint for load balancers and monitoring systems.

```yaml
paths:
  /api/health:
    get:
      tags:
        - Health Monitoring
      summary: System health check
      description: |
        Quick health check endpoint that returns the overall system status.
        Designed for load balancers and uptime monitoring systems.
      operationId: getSystemHealth
      security: []
      responses:
        '200':
          description: System is healthy
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HealthStatus'
              example:
                status: "Healthy"
                timestamp: "2024-01-15T10:30:00Z"
                version: "2.0.0"
        '503':
          description: System is unhealthy
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HealthStatus'

components:
  schemas:
    HealthStatus:
      type: object
      properties:
        status:
          type: string
          enum: [Healthy, Degraded, Unhealthy]
          description: Overall system health status
        timestamp:
          type: string
          format: date-time
          description: Health check timestamp
        version:
          type: string
          description: Application version
        uptime:
          type: integer
          description: System uptime in seconds
          nullable: true
      required:
        - status
        - timestamp
        - version
```

### Detailed Health Check

#### GET /api/health/detailed

Comprehensive health check with component-level status information.

```yaml
  /api/health/detailed:
    get:
      tags:
        - Health Monitoring
      summary: Detailed health information
      description: |
        Comprehensive health check providing detailed status information
        for all system components including services, dependencies, and
        infrastructure elements.
      operationId: getDetailedHealth
      responses:
        '200':
          description: Detailed health information retrieved
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DetailedHealthResponse'

components:
  schemas:
    DetailedHealthResponse:
      type: object
      properties:
        overall:
          $ref: '#/components/schemas/HealthStatus'
        components:
          type: object
          properties:
            database:
              $ref: '#/components/schemas/ComponentHealth'
            sgxEnclave:
              $ref: '#/components/schemas/ComponentHealth'
            securityService:
              $ref: '#/components/schemas/ComponentHealth'
            observabilityService:
              $ref: '#/components/schemas/ComponentHealth'
            resilienceService:
              $ref: '#/components/schemas/ComponentHealth'
        dependencies:
          type: object
          properties:
            redis:
              $ref: '#/components/schemas/DependencyHealth'
            externalApi:
              $ref: '#/components/schemas/DependencyHealth'
        performance:
          $ref: '#/components/schemas/PerformanceHealth'
        security:
          $ref: '#/components/schemas/SecurityHealth'

    ComponentHealth:
      type: object
      properties:
        status:
          type: string
          enum: [Healthy, Degraded, Unhealthy]
        message:
          type: string
          description: Status description
        lastCheck:
          type: string
          format: date-time
        responseTime:
          type: integer
          description: Health check response time in milliseconds
        details:
          type: object
          description: Component-specific health details
      required:
        - status
        - lastCheck

    DependencyHealth:
      type: object
      properties:
        status:
          type: string
          enum: [Available, Unavailable, Degraded]
        connectionTime:
          type: integer
          description: Connection time in milliseconds
        lastSuccessfulConnection:
          type: string
          format: date-time
        errorCount:
          type: integer
          description: Recent error count
        details:
          type: object
      required:
        - status

    PerformanceHealth:
      type: object
      properties:
        averageResponseTime:
          type: number
          format: float
          description: Average response time in milliseconds
        throughput:
          type: number
          format: float
          description: Requests per second
        errorRate:
          type: number
          format: float
          description: Error rate percentage
        memoryUsage:
          type: object
          properties:
            used:
              type: integer
              description: Used memory in bytes
            total:
              type: integer
              description: Total memory in bytes
            percentage:
              type: number
              format: float
              description: Memory usage percentage
        cpuUsage:
          type: number
          format: float
          description: CPU usage percentage

    SecurityHealth:
      type: object
      properties:
        threatsDetected:
          type: integer
          description: Security threats detected in last hour
        lastSecurityScan:
          type: string
          format: date-time
        encryptionStatus:
          type: string
          enum: [Enabled, Disabled, Error]
        attestationStatus:
          type: string
          enum: [Valid, Invalid, Expired, NotPerformed]
        riskLevel:
          type: string
          enum: [Low, Medium, High, Critical]
```

### Service-Specific Health Checks

#### GET /api/health/services/{serviceName}

Health check for specific service components.

```yaml
  /api/health/services/{serviceName}:
    get:
      tags:
        - Health Monitoring
      summary: Service-specific health check
      description: Retrieves health information for a specific service
      operationId: getServiceHealth
      parameters:
        - name: serviceName
          in: path
          required: true
          schema:
            type: string
            enum: [security, sgx, observability, resilience, storage]
          description: Name of the service to check
      responses:
        '200':
          description: Service health information
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ServiceHealthResponse'
        '404':
          description: Service not found

components:
  schemas:
    ServiceHealthResponse:
      type: object
      properties:
        serviceName:
          type: string
          description: Name of the service
        status:
          type: string
          enum: [Healthy, Degraded, Unhealthy]
        version:
          type: string
          description: Service version
        uptime:
          type: integer
          description: Service uptime in seconds
        metrics:
          type: object
          description: Service-specific metrics
        dependencies:
          type: array
          items:
            type: object
            properties:
              name:
                type: string
              status:
                type: string
                enum: [Healthy, Unhealthy]
        lastRestart:
          type: string
          format: date-time
          nullable: true
```

## Metrics API

### System Metrics

#### GET /api/metrics

Prometheus-compatible metrics endpoint.

```yaml
  /api/metrics:
    get:
      tags:
        - Metrics
      summary: Get system metrics
      description: |
        Returns system metrics in Prometheus format for monitoring
        and alerting systems. Includes application, business, and
        infrastructure metrics.
      operationId: getMetrics
      parameters:
        - name: format
          in: query
          schema:
            type: string
            enum: [prometheus, json, openmetrics]
            default: prometheus
          description: Metrics output format
      responses:
        '200':
          description: Metrics retrieved successfully
          content:
            text/plain:
              schema:
                type: string
                description: Prometheus format metrics
            application/json:
              schema:
                $ref: '#/components/schemas/JsonMetricsResponse'

components:
  schemas:
    JsonMetricsResponse:
      type: object
      properties:
        timestamp:
          type: string
          format: date-time
        application:
          $ref: '#/components/schemas/ApplicationMetrics'
        business:
          $ref: '#/components/schemas/BusinessMetrics'
        infrastructure:
          $ref: '#/components/schemas/InfrastructureMetrics'
        security:
          $ref: '#/components/schemas/SecurityMetrics'

    ApplicationMetrics:
      type: object
      properties:
        requests:
          type: object
          properties:
            total:
              type: integer
              description: Total requests processed
            rate:
              type: number
              format: float
              description: Requests per second
            errors:
              type: integer
              description: Total error count
            errorRate:
              type: number
              format: float
              description: Error rate percentage
        responseTime:
          type: object
          properties:
            average:
              type: number
              format: float
              description: Average response time in ms
            p50:
              type: number
              format: float
            p90:
              type: number
              format: float
            p99:
              type: number
              format: float
        activeConnections:
          type: integer
          description: Current active connections

    BusinessMetrics:
      type: object
      properties:
        sgxExecutions:
          type: object
          properties:
            total:
              type: integer
            successful:
              type: integer
            failed:
              type: integer
            averageExecutionTime:
              type: number
              format: float
        securityValidations:
          type: object
          properties:
            total:
              type: integer
            threatsDetected:
              type: integer
            threatsBlocked:
              type: integer
        dataOperations:
          type: object
          properties:
            encryptions:
              type: integer
            decryptions:
              type: integer
            sealingOperations:
              type: integer
            unsealingOperations:
              type: integer

    InfrastructureMetrics:
      type: object
      properties:
        system:
          type: object
          properties:
            cpuUsage:
              type: number
              format: float
              description: CPU usage percentage
            memoryUsage:
              type: number
              format: float
              description: Memory usage percentage
            diskUsage:
              type: number
              format: float
              description: Disk usage percentage
        process:
          type: object
          properties:
            heapMemory:
              type: integer
              description: Heap memory usage in bytes
            gcCollections:
              type: integer
              description: Garbage collection count
            threadCount:
              type: integer
              description: Active thread count

    SecurityMetrics:
      type: object
      properties:
        threats:
          type: object
          properties:
            sqlInjectionAttempts:
              type: integer
            xssAttempts:
              type: integer
            codeInjectionAttempts:
              type: integer
            rateLimitViolations:
              type: integer
        authentication:
          type: object
          properties:
            successfulLogins:
              type: integer
            failedLogins:
              type: integer
            tokenGenerations:
              type: integer
            tokenValidations:
              type: integer
```

### Custom Metrics Query

#### POST /api/metrics/query

Query custom metrics with filters and aggregations.

```yaml
  /api/metrics/query:
    post:
      tags:
        - Metrics
      summary: Query custom metrics
      description: |
        Query metrics with custom filters, time ranges, and aggregations.
        Supports PromQL-like syntax for advanced metric queries.
      operationId: queryMetrics
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/MetricsQuery'
      responses:
        '200':
          description: Query executed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/MetricsQueryResponse'

components:
  schemas:
    MetricsQuery:
      type: object
      required:
        - query
      properties:
        query:
          type: string
          description: PromQL-like query string
          example: "neo_requests_total{service=\"security\"}"
        start:
          type: string
          format: date-time
          description: Query start time
        end:
          type: string
          format: date-time
          description: Query end time
        step:
          type: string
          description: Query step interval
          example: "1m"
        aggregation:
          type: string
          enum: [sum, avg, min, max, count]
          description: Aggregation function
        groupBy:
          type: array
          items:
            type: string
          description: Group by labels

    MetricsQueryResponse:
      type: object
      properties:
        query:
          type: string
          description: Original query
        resultType:
          type: string
          enum: [vector, matrix, scalar, string]
        result:
          type: array
          items:
            $ref: '#/components/schemas/MetricSample'
        executionTime:
          type: number
          format: float
          description: Query execution time in seconds

    MetricSample:
      type: object
      properties:
        metric:
          type: object
          description: Metric labels
        values:
          type: array
          items:
            type: array
            items:
              oneOf:
                - type: number
                - type: string
          description: Timestamp-value pairs
```

## Tracing API

### Distributed Tracing

#### GET /api/tracing/traces

Retrieve distributed traces for request flow analysis.

```yaml
  /api/tracing/traces:
    get:
      tags:
        - Distributed Tracing
      summary: Get distributed traces
      description: |
        Retrieves distributed traces for analyzing request flows across
        services. Supports filtering by time range, service, operation,
        and trace attributes.
      operationId: getTraces
      parameters:
        - name: service
          in: query
          schema:
            type: string
          description: Filter by service name
        - name: operation
          in: query
          schema:
            type: string
          description: Filter by operation name
        - name: startTime
          in: query
          schema:
            type: string
            format: date-time
          description: Start time for trace query
        - name: endTime
          in: query
          schema:
            type: string
            format: date-time
          description: End time for trace query
        - name: minDuration
          in: query
          schema:
            type: integer
          description: Minimum trace duration in milliseconds
        - name: maxDuration
          in: query
          schema:
            type: integer
          description: Maximum trace duration in milliseconds
        - name: limit
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 1000
            default: 100
          description: Maximum number of traces to return
        - name: tags
          in: query
          schema:
            type: string
          description: Filter by tags (format: key:value,key2:value2)
      responses:
        '200':
          description: Traces retrieved successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TracesResponse'

components:
  schemas:
    TracesResponse:
      type: object
      properties:
        traces:
          type: array
          items:
            $ref: '#/components/schemas/Trace'
        totalCount:
          type: integer
          description: Total number of traces matching criteria
        hasMore:
          type: boolean
          description: Whether more traces are available

    Trace:
      type: object
      properties:
        traceId:
          type: string
          description: Unique trace identifier
        rootSpan:
          $ref: '#/components/schemas/Span'
        spans:
          type: array
          items:
            $ref: '#/components/schemas/Span'
        duration:
          type: integer
          description: Total trace duration in microseconds
        startTime:
          type: string
          format: date-time
        services:
          type: array
          items:
            type: string
          description: Services involved in the trace
        errorCount:
          type: integer
          description: Number of spans with errors

    Span:
      type: object
      properties:
        spanId:
          type: string
          description: Unique span identifier
        parentSpanId:
          type: string
          description: Parent span identifier
          nullable: true
        operationName:
          type: string
          description: Operation name
        serviceName:
          type: string
          description: Service name
        startTime:
          type: integer
          description: Span start time (microseconds since epoch)
        duration:
          type: integer
          description: Span duration in microseconds
        tags:
          type: object
          description: Span tags/attributes
        status:
          type: object
          properties:
            code:
              type: string
              enum: [OK, ERROR, TIMEOUT]
            message:
              type: string
              nullable: true
        events:
          type: array
          items:
            $ref: '#/components/schemas/SpanEvent'

    SpanEvent:
      type: object
      properties:
        name:
          type: string
          description: Event name
        timestamp:
          type: integer
          description: Event timestamp (microseconds since epoch)
        attributes:
          type: object
          description: Event attributes
```

### Trace Details

#### GET /api/tracing/traces/{traceId}

Get detailed information for a specific trace.

```yaml
  /api/tracing/traces/{traceId}:
    get:
      tags:
        - Distributed Tracing
      summary: Get trace details
      description: Retrieves detailed information for a specific trace
      operationId: getTraceDetails
      parameters:
        - name: traceId
          in: path
          required: true
          schema:
            type: string
          description: Trace identifier
      responses:
        '200':
          description: Trace details retrieved
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TraceDetailsResponse'
        '404':
          description: Trace not found

components:
  schemas:
    TraceDetailsResponse:
      type: object
      properties:
        trace:
          $ref: '#/components/schemas/Trace'
        timeline:
          type: array
          items:
            $ref: '#/components/schemas/TimelineEvent'
        criticalPath:
          type: array
          items:
            type: string
          description: Span IDs on the critical path
        bottlenecks:
          type: array
          items:
            $ref: '#/components/schemas/Bottleneck'
        dependencies:
          type: object
          description: Service dependency graph

    TimelineEvent:
      type: object
      properties:
        timestamp:
          type: integer
          description: Event timestamp (microseconds since epoch)
        type:
          type: string
          enum: [SpanStart, SpanFinish, Error, Annotation]
        spanId:
          type: string
        serviceName:
          type: string
        description:
          type: string

    Bottleneck:
      type: object
      properties:
        spanId:
          type: string
        serviceName:
          type: string
        operationName:
          type: string
        duration:
          type: integer
          description: Duration in microseconds
        percentageOfTrace:
          type: number
          format: float
          description: Percentage of total trace time
        reason:
          type: string
          description: Suspected reason for bottleneck
```

## Performance Monitoring API

### Performance Reports

#### GET /api/performance/reports

Generate performance analysis reports.

```yaml
  /api/performance/reports:
    get:
      tags:
        - Performance Monitoring
      summary: Get performance reports
      description: |
        Generates comprehensive performance analysis reports including
        response times, throughput, error rates, and resource utilization.
      operationId: getPerformanceReports
      parameters:
        - name: reportType
          in: query
          schema:
            type: string
            enum: [summary, detailed, trending, comparative]
            default: summary
        - name: timeRange
          in: query
          schema:
            type: string
            enum: [1h, 6h, 24h, 7d, 30d]
            default: 24h
        - name: services
          in: query
          schema:
            type: array
            items:
              type: string
          description: Filter by specific services
          style: form
          explode: true
      responses:
        '200':
          description: Performance report generated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PerformanceReport'

components:
  schemas:
    PerformanceReport:
      type: object
      properties:
        reportId:
          type: string
          description: Unique report identifier
        reportType:
          type: string
        generatedAt:
          type: string
          format: date-time
        timeRange:
          $ref: '#/components/schemas/TimeRange'
        summary:
          $ref: '#/components/schemas/PerformanceSummary'
        serviceMetrics:
          type: array
          items:
            $ref: '#/components/schemas/ServicePerformanceMetrics'
        trends:
          type: array
          items:
            $ref: '#/components/schemas/PerformanceTrend'
        alerts:
          type: array
          items:
            $ref: '#/components/schemas/PerformanceAlert'
        recommendations:
          type: array
          items:
            $ref: '#/components/schemas/PerformanceRecommendation'

    TimeRange:
      type: object
      properties:
        start:
          type: string
          format: date-time
        end:
          type: string
          format: date-time
        duration:
          type: string
          description: Duration string (e.g., "24h")

    PerformanceSummary:
      type: object
      properties:
        totalRequests:
          type: integer
        averageResponseTime:
          type: number
          format: float
          description: Average response time in milliseconds
        throughput:
          type: number
          format: float
          description: Requests per second
        errorRate:
          type: number
          format: float
          description: Error rate percentage
        availability:
          type: number
          format: float
          description: Availability percentage
        apdex:
          type: number
          format: float
          description: Application Performance Index
        sla:
          type: object
          properties:
            target:
              type: number
              format: float
            achieved:
              type: number
              format: float
            breaches:
              type: integer

    ServicePerformanceMetrics:
      type: object
      properties:
        serviceName:
          type: string
        requestCount:
          type: integer
        averageResponseTime:
          type: number
          format: float
        errorRate:
          type: number
          format: float
        resourceUtilization:
          type: object
          properties:
            cpu:
              type: number
              format: float
            memory:
              type: number
              format: float
            disk:
              type: number
              format: float
        endpoints:
          type: array
          items:
            $ref: '#/components/schemas/EndpointPerformance'

    EndpointPerformance:
      type: object
      properties:
        path:
          type: string
        method:
          type: string
        requestCount:
          type: integer
        averageResponseTime:
          type: number
          format: float
        errorRate:
          type: number
          format: float
        latencyPercentiles:
          type: object
          properties:
            p50:
              type: number
              format: float
            p90:
              type: number
              format: float
            p95:
              type: number
              format: float
            p99:
              type: number
              format: float

    PerformanceTrend:
      type: object
      properties:
        metric:
          type: string
          enum: [response_time, throughput, error_rate, resource_usage]
        direction:
          type: string
          enum: [improving, degrading, stable]
        change:
          type: number
          format: float
          description: Percentage change
        significance:
          type: string
          enum: [low, medium, high]
        timeframe:
          type: string

    PerformanceAlert:
      type: object
      properties:
        id:
          type: string
        severity:
          type: string
          enum: [info, warning, critical]
        metric:
          type: string
        threshold:
          type: number
          format: float
        currentValue:
          type: number
          format: float
        message:
          type: string
        triggeredAt:
          type: string
          format: date-time
        service:
          type: string
          nullable: true

    PerformanceRecommendation:
      type: object
      properties:
        id:
          type: string
        title:
          type: string
        description:
          type: string
        priority:
          type: string
          enum: [low, medium, high, critical]
        category:
          type: string
          enum: [scaling, optimization, configuration, infrastructure]
        expectedImpact:
          type: string
        effort:
          type: string
          enum: [low, medium, high]
        actions:
          type: array
          items:
            type: string
```

## Logging API

### Log Query and Search

#### POST /api/logs/search

Search and filter system logs.

```yaml
  /api/logs/search:
    post:
      tags:
        - Logging
      summary: Search system logs
      description: |
        Search and filter system logs with advanced query capabilities.
        Supports full-text search, structured filters, and time-based queries.
      operationId: searchLogs
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/LogSearchRequest'
      responses:
        '200':
          description: Log search completed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/LogSearchResponse'

components:
  schemas:
    LogSearchRequest:
      type: object
      properties:
        query:
          type: string
          description: Full-text search query
        filters:
          type: object
          properties:
            level:
              type: array
              items:
                type: string
                enum: [trace, debug, info, warning, error, critical]
            service:
              type: array
              items:
                type: string
            correlationId:
              type: string
            userId:
              type: string
            traceId:
              type: string
        timeRange:
          $ref: '#/components/schemas/TimeRange'
        sort:
          type: object
          properties:
            field:
              type: string
              enum: [timestamp, level, service]
              default: timestamp
            direction:
              type: string
              enum: [asc, desc]
              default: desc
        limit:
          type: integer
          minimum: 1
          maximum: 10000
          default: 100
        offset:
          type: integer
          minimum: 0
          default: 0

    LogSearchResponse:
      type: object
      properties:
        logs:
          type: array
          items:
            $ref: '#/components/schemas/LogEntry'
        totalCount:
          type: integer
        hasMore:
          type: boolean
        aggregations:
          type: object
          description: Log aggregations by level, service, etc.
        executionTime:
          type: number
          format: float
          description: Search execution time in seconds

    LogEntry:
      type: object
      properties:
        id:
          type: string
          description: Unique log entry identifier
        timestamp:
          type: string
          format: date-time
        level:
          type: string
          enum: [trace, debug, info, warning, error, critical]
        message:
          type: string
        service:
          type: string
        correlationId:
          type: string
          nullable: true
        traceId:
          type: string
          nullable: true
        userId:
          type: string
          nullable: true
        properties:
          type: object
          description: Additional structured log properties
        exception:
          $ref: '#/components/schemas/LogException'
          nullable: true

    LogException:
      type: object
      properties:
        type:
          type: string
        message:
          type: string
        stackTrace:
          type: string
        innerException:
          $ref: '#/components/schemas/LogException'
          nullable: true
```

## Error Handling

### Error Response Schemas

```yaml
components:
  schemas:
    ObservabilityErrorResponse:
      type: object
      properties:
        error:
          type: string
          description: Error message
        code:
          type: string
          description: Error code
        details:
          type: object
          description: Additional error details
        timestamp:
          type: string
          format: date-time
        requestId:
          type: string
          description: Request correlation ID
      required:
        - error
        - code
        - timestamp
        - requestId
```

## Performance Specifications

### Response Time Targets

| Endpoint | Target Latency | Max Latency |
|----------|----------------|-------------|
| `/health` | < 10ms | 50ms |
| `/health/detailed` | < 100ms | 500ms |
| `/metrics` | < 50ms | 200ms |
| `/metrics/query` | < 500ms | 2000ms |
| `/tracing/traces` | < 200ms | 1000ms |
| `/performance/reports` | < 1000ms | 5000ms |
| `/logs/search` | < 300ms | 1500ms |

### Data Retention Policies

| Data Type | Retention Period | Storage |
|-----------|-----------------|---------|
| Metrics | 90 days | Time-series DB |
| Traces | 30 days | Distributed storage |
| Logs | 60 days | Log aggregation |
| Health Checks | 7 days | Memory cache |
| Performance Reports | 180 days | Object storage |

This comprehensive observability API specification provides complete monitoring, metrics, tracing, and logging capabilities for the Neo Service Layer platform, enabling comprehensive system observability and performance analysis.