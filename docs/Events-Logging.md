# Event System and Secure Logging Guide

This document provides a comprehensive guide to the event system and secure logging features in the Neo Confidential Serverless Layer (NCSL).

## Table of Contents

1. [Overview](#overview)
2. [Event System](#event-system)
3. [Secure Logging](#secure-logging)
4. [Best Practices](#best-practices)
5. [Troubleshooting](#troubleshooting)
6. [References](#references)

## Overview

The Neo Confidential Serverless Layer (NCSL) provides a secure execution environment for JavaScript functions using Intel SGX and the OpenEnclave SDK. The event system and secure logging features enhance the security, reliability, and usability of the NCSL.

## Event System

The event system provides a way for enclaves to communicate with each other and with the host application. The `EnclaveEventSystem` class is the main entry point for publishing and subscribing to events.

### Publishing Events

```csharp
// Create an event system
var eventSystem = new EnclaveEventSystem(
    loggerFactory.CreateLogger<EnclaveEventSystem>(),
    enclaveInterface,
    new EnclaveEventOptions
    {
        MaxQueueSize = 1000,
        EnqueueTimeoutMs = 1000,
        EnablePersistence = true,
        PersistenceDirectory = "events"
    });

// Publish an event
string eventType = "UserCreated";
var eventData = new
{
    UserId = "user123",
    Username = "johndoe",
    Email = "john.doe@example.com",
    CreatedAt = DateTime.UtcNow
};
await eventSystem.PublishAsync(eventType, eventData);

Console.WriteLine($"Event of type {eventType} published");
```

### Subscribing to Events

```csharp
// Subscribe to events
string eventType = "UserCreated";
var subscription = eventSystem.Subscribe(eventType, async (enclaveEvent) =>
{
    Console.WriteLine($"Received event of type {enclaveEvent.Type}");
    Console.WriteLine($"Event data: {enclaveEvent.Data}");
    
    // Process the event
    // ...
    
    return Task.CompletedTask;
});

Console.WriteLine($"Subscribed to events of type {eventType}");
```

### Unsubscribing from Events

```csharp
// Unsubscribe from events
bool unsubscribed = eventSystem.Unsubscribe(subscription);

if (unsubscribed)
{
    Console.WriteLine("Unsubscribed from events");
}
else
{
    Console.WriteLine("Failed to unsubscribe from events");
}

// Alternatively, dispose the subscription
subscription.Dispose();
```

### Getting Subscriptions

```csharp
// Get all subscriptions
var allSubscriptions = eventSystem.GetSubscriptions();
Console.WriteLine($"Found {allSubscriptions.Count} subscriptions");

// Get subscriptions for a specific event type
string eventType = "UserCreated";
var eventTypeSubscriptions = eventSystem.GetSubscriptions(eventType);
Console.WriteLine($"Found {eventTypeSubscriptions.Count} subscriptions for event type {eventType}");
```

## Secure Logging

The secure logging features provide a way to log messages securely within an enclave. The `SecureLogger` class is the main entry point for secure logging operations.

### Logging Messages

```csharp
// Create a secure logger
var secureLogger = new SecureLogger(
    loggerFactory.CreateLogger<SecureLogger>(),
    enclaveInterface,
    eventSystem,
    new SecureLoggerOptions
    {
        LogDirectory = "logs",
        MinimumLevel = LogLevel.Information,
        EnableFileLogging = true,
        EnableEventLogging = true,
        EnableSealing = true
    });

// Log a message
secureLogger.Log(LogLevel.Information, "Hello, world!");

// Log a message with arguments
secureLogger.Log(LogLevel.Information, "Hello, {Name}!", "John");

// Log a message with properties
var properties = new Dictionary<string, string>
{
    { "UserId", "user123" },
    { "RequestId", "req456" }
};
secureLogger.LogWithProperties(LogLevel.Information, "User logged in", properties);

// Log an exception
try
{
    // Some code that might throw an exception
    throw new InvalidOperationException("Something went wrong");
}
catch (Exception ex)
{
    secureLogger.LogException(LogLevel.Error, ex, "An error occurred");
}
```

### Getting Log Entries

```csharp
// Get log entries
var logEntries = await secureLogger.GetLogEntriesAsync(
    count: 100,
    level: LogLevel.Information,
    source: "Enclave");

Console.WriteLine($"Found {logEntries.Count} log entries");

foreach (var logEntry in logEntries)
{
    Console.WriteLine($"[{logEntry.Timestamp}] {logEntry.Level}: {logEntry.Message}");
    
    if (logEntry.Exception != null)
    {
        Console.WriteLine($"Exception: {logEntry.Exception}");
    }
    
    if (logEntry.Properties.Count > 0)
    {
        Console.WriteLine("Properties:");
        foreach (var property in logEntry.Properties)
        {
            Console.WriteLine($"  {property.Key}: {property.Value}");
        }
    }
}
```

### Clearing Log Entries

```csharp
// Clear log entries
await secureLogger.ClearLogEntriesAsync();
Console.WriteLine("Log entries cleared");
```

### Checking if a Log Level is Enabled

```csharp
// Check if a log level is enabled
bool isDebugEnabled = secureLogger.IsEnabled(LogLevel.Debug);
bool isInfoEnabled = secureLogger.IsEnabled(LogLevel.Information);

Console.WriteLine($"Debug logging is {(isDebugEnabled ? "enabled" : "disabled")}");
Console.WriteLine($"Information logging is {(isInfoEnabled ? "enabled" : "disabled")}");
```

## Best Practices

1. **Use Event Types Consistently**: Use consistent event types across your application to make it easier to subscribe to events.

2. **Handle Event Processing Errors**: Handle errors that occur during event processing to prevent them from affecting other events.

3. **Use Appropriate Log Levels**: Use appropriate log levels for different types of messages to make it easier to filter logs.

4. **Include Relevant Properties**: Include relevant properties in log entries to provide context for troubleshooting.

5. **Enable Sealing for Sensitive Logs**: Enable sealing for logs that contain sensitive information to protect them from unauthorized access.

6. **Limit Log File Size**: Limit the size of log files to prevent them from consuming too much disk space.

7. **Rotate Log Files**: Rotate log files regularly to prevent them from growing too large.

8. **Monitor Event Queue Size**: Monitor the size of the event queue to prevent it from growing too large.

9. **Dispose Event Subscriptions**: Dispose event subscriptions when they are no longer needed to prevent memory leaks.

10. **Use Structured Logging**: Use structured logging with properties to make it easier to search and filter logs.

## Troubleshooting

1. **Events Not Being Processed**: If events are not being processed, check that the event system is not disposed and that there are subscribers for the event type.

2. **Log Entries Not Being Written**: If log entries are not being written, check that the log directory exists and is writable.

3. **Log Entries Not Being Sealed**: If log entries are not being sealed, check that sealing is enabled and that the enclave interface is properly initialized.

4. **Event Queue Full**: If the event queue is full, increase the maximum queue size or reduce the number of events being published.

5. **Log Queue Full**: If the log queue is full, increase the maximum queue size or reduce the number of log entries being written.

## References

- [OpenEnclave SDK GitHub Repository](https://github.com/openenclave/openenclave)
- [OpenEnclave SDK Documentation](https://github.com/openenclave/openenclave/tree/master/docs)
- [Intel SGX Documentation](https://software.intel.com/content/www/us/en/develop/topics/software-guard-extensions.html)
- [Microsoft.Extensions.Logging Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
