# Secure RPC and Metrics Collection Guide

This document provides a comprehensive guide to the secure remote procedure call (RPC) and metrics collection features in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Secure RPC System](#secure-rpc-system)
3. [Metrics Collection](#metrics-collection)
4. [Best Practices](#best-practices)
5. [Troubleshooting](#troubleshooting)
6. [References](#references)

## Overview

The Neo Confidential Serverless Layer (NCSL) provides a secure execution environment for JavaScript functions using Intel SGX and the OpenEnclave SDK. The secure RPC and metrics collection features enhance the security, reliability, and usability of the NCSL.

## Secure RPC System

The secure RPC system provides a way for enclaves to communicate with each other and with the host application using remote procedure calls. The `SecureRpcSystem` class is the main entry point for making RPC calls and registering RPC methods.

### Making RPC Calls

```csharp
// Create an RPC system
var rpcSystem = new SecureRpcSystem(
    loggerFactory.CreateLogger<SecureRpcSystem>(),
    enclaveInterface,
    eventSystem,
    secureLogger,
    new SecureRpcOptions
    {
        DefaultTimeoutMs = 30000,
        MaxConcurrentRequests = 10,
        EnableAuthentication = true,
        EnableEncryption = true
    });

// Call a remote procedure
string method = "GetUserProfile";
var parameters = new
{
    UserId = "user123",
    IncludeDetails = true
};
var response = await rpcSystem.CallAsync(method, parameters);

if (response.IsSuccess)
{
    Console.WriteLine($"RPC call successful: {response.Result}");
}
else
{
    Console.WriteLine($"RPC call failed: {response.Error}");
}
```

### Registering RPC Methods

```csharp
// Register an RPC method
string method = "GetUserProfile";
rpcSystem.RegisterMethod(method, async (request) =>
{
    // Get the parameters
    var parameters = request.Parameters as JsonElement;
    string userId = parameters.GetProperty("UserId").GetString();
    bool includeDetails = parameters.GetProperty("IncludeDetails").GetBoolean();
    
    // Process the request
    var userProfile = await GetUserProfileAsync(userId, includeDetails);
    
    // Create the response
    return new RpcResponse
    {
        Result = userProfile
    };
});

Console.WriteLine($"Registered RPC method {method}");
```

### Unregistering RPC Methods

```csharp
// Unregister an RPC method
string method = "GetUserProfile";
bool unregistered = rpcSystem.UnregisterMethod(method);

if (unregistered)
{
    Console.WriteLine($"Unregistered RPC method {method}");
}
else
{
    Console.WriteLine($"Failed to unregister RPC method {method}");
}
```

### Getting Registered Methods

```csharp
// Get all registered methods
var methods = rpcSystem.GetRegisteredMethods();
Console.WriteLine($"Found {methods.Count} registered methods:");
foreach (var method in methods)
{
    Console.WriteLine($"- {method}");
}
```

## Metrics Collection

The metrics collection features provide a way to collect and report metrics for enclaves. The `MetricsCollector` class is the main entry point for collecting and reporting metrics.

### Recording Metrics

```csharp
// Create a metrics collector
var metricsCollector = new MetricsCollector(
    loggerFactory.CreateLogger<MetricsCollector>(),
    enclaveInterface,
    eventSystem,
    new MetricsCollectorOptions
    {
        MetricsDirectory = "metrics",
        EnablePeriodicReporting = true,
        ReportingIntervalMs = 60000,
        EnableFileReporting = true,
        EnableEventReporting = true,
        EnableHostReporting = true
    });

// Record a counter metric
metricsCollector.RecordCounter("requests.total");

// Record a counter metric with a value
metricsCollector.RecordCounter("requests.success", 1);

// Record a counter metric with tags
var tags = new Dictionary<string, string>
{
    { "method", "GetUserProfile" },
    { "userId", "user123" }
};
metricsCollector.RecordCounter("requests.total", 1, tags);

// Record a gauge metric
metricsCollector.RecordGauge("memory.used", 1024 * 1024);

// Record a histogram metric
metricsCollector.RecordHistogram("request.size", 1024);

// Record a timer metric
metricsCollector.RecordTimer("request.duration", 100);
```

### Getting Metrics

```csharp
// Get a metric by name
var metric = metricsCollector.GetMetric("requests.total");
if (metric != null)
{
    Console.WriteLine($"Metric {metric.Name}: {metric.Value}");
    
    if (metric.Tags.Count > 0)
    {
        Console.WriteLine("Tags:");
        foreach (var tag in metric.Tags)
        {
            Console.WriteLine($"  {tag.Key}: {tag.Value}");
        }
    }
}
else
{
    Console.WriteLine("Metric not found");
}

// Get all metrics
var metrics = metricsCollector.GetAllMetrics();
Console.WriteLine($"Found {metrics.Count} metrics");
```

### Reporting Metrics

```csharp
// Report metrics
await metricsCollector.ReportMetricsAsync();
Console.WriteLine("Metrics reported");

// Reset metrics
metricsCollector.ResetMetrics();
Console.WriteLine("Metrics reset");
```

### Sending Metrics to the Host

```csharp
// Send a metric to the host
enclaveInterface.SendMetricToHost("requests.total", "100");
Console.WriteLine("Metric sent to host");
```

## Best Practices

1. **Use Timeouts for RPC Calls**: Use timeouts for RPC calls to prevent them from hanging indefinitely.

2. **Limit Concurrent RPC Requests**: Limit the number of concurrent RPC requests to prevent resource exhaustion.

3. **Use Authentication and Encryption**: Enable authentication and encryption for RPC calls to protect sensitive data.

4. **Handle RPC Errors Gracefully**: Handle RPC errors gracefully and provide meaningful error messages.

5. **Use Appropriate Metric Types**: Use appropriate metric types for different types of measurements.

6. **Use Tags for Metrics**: Use tags to provide additional context for metrics.

7. **Report Metrics Periodically**: Report metrics periodically to monitor the health and performance of the enclave.

8. **Reset Metrics When Appropriate**: Reset metrics when appropriate to prevent them from growing too large.

9. **Monitor Metric Growth**: Monitor the growth of metrics to prevent them from consuming too much memory.

10. **Use Structured Metrics**: Use structured metrics with tags to make it easier to search and filter metrics.

## Troubleshooting

1. **RPC Calls Timing Out**: If RPC calls are timing out, check that the method handler is not taking too long to process requests.

2. **RPC Method Not Found**: If an RPC method is not found, check that the method is registered with the correct name.

3. **RPC Authentication Failing**: If RPC authentication is failing, check that the authentication token is valid.

4. **Metrics Not Being Reported**: If metrics are not being reported, check that the reporting options are enabled.

5. **Metrics Not Being Sent to Host**: If metrics are not being sent to the host, check that the host_send_metric function is properly implemented.

## References

- [OpenEnclave SDK GitHub Repository](https://github.com/openenclave/openenclave)
- [OpenEnclave SDK Documentation](https://github.com/openenclave/openenclave/tree/master/docs)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
- [Prometheus Metrics Documentation](https://prometheus.io/docs/concepts/metric_types/)
