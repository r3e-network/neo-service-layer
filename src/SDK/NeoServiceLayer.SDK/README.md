# Neo Service Layer SDK

[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue)](https://www.nuget.org/packages/NeoServiceLayer.SDK/)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

> **🚀 Production-Ready SDK** - Official .NET client library for the Neo Service Layer microservices platform

## 🌟 Features

- **🏗️ Microservices Architecture** - Built for distributed service communication
- **🔍 Service Discovery** - Automatic service location via Consul
- **🔄 Resilience Patterns** - Circuit breakers, retries, and timeouts
- **🔐 Security** - JWT authentication and secure communication
- **📊 Observability** - Built-in tracing and metrics
- **⚡ Performance** - HTTP/2 support and connection pooling
- **🧪 Testing** - Comprehensive mocking and testing support

## 📦 Installation

```bash
# Install via NuGet
dotnet add package NeoServiceLayer.SDK

# Or via Package Manager
Install-Package NeoServiceLayer.SDK
```

## 🚀 Quick Start

### Basic Setup

```csharp
using NeoServiceLayer.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create host with SDK services
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Neo Service Layer client
        services.AddNeoServiceLayerClient(options =>
        {
            options.GatewayUrl = "http://localhost:7000";
            options.ApiKey = "your-api-key"; // For development
            options.Timeout = TimeSpan.FromSeconds(30);
        });
    })
    .Build();

// Get client from DI container
var client = host.Services.GetRequiredService<INeoServiceLayerClient>();

// Authenticate (production)
await client.AuthenticateAsync("username", "password");

// Or use API key (development)
client.SetApiKey("dev-api-key");
```

### Configuration-Based Setup

```csharp
// appsettings.json
{
  "NeoServiceLayer": {
    "GatewayUrl": "http://localhost:7000",
    "ApiKey": "dev-api-key",
    "Timeout": "00:00:30",
    "RetryCount": 3,
    "EnableCircuitBreaker": true,
    "ServiceDiscovery": {
      "ConsulUrl": "http://localhost:8500",
      "Datacenter": "dc1"
    }
  }
}

// Program.cs
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

services.AddNeoServiceLayerClient(configuration.GetSection("NeoServiceLayer"));
```

## 🎯 Service Usage Examples

### 📧 Notification Service

```csharp
// Send email notification
var notificationResult = await client.Notification.SendAsync(new SendNotificationRequest
{
    Channel = NotificationChannel.Email,
    Recipient = "user@example.com",
    Subject = "Transaction Confirmed",
    Message = "Your transaction #12345 has been confirmed on the blockchain.",
    Priority = NotificationPriority.Normal,
    Metadata = new Dictionary<string, object>
    {
        ["transaction_id"] = "tx-12345",
        ["block_height"] = 1234567
    }
});

Console.WriteLine($"Notification sent: {notificationResult.NotificationId}");

// Check notification status
var status = await client.Notification.GetStatusAsync(notificationResult.NotificationId);
Console.WriteLine($"Status: {status.Status}, Delivered: {status.DeliveredAt}");

// Send using template
var templateResult = await client.Notification.SendFromTemplateAsync(new TemplateNotificationRequest
{
    TemplateName = "transaction-alert",
    Recipient = "user@example.com",
    Variables = new Dictionary<string, object>
    {
        ["transaction_type"] = "Transfer",
        ["amount"] = "100 GAS",
        ["recipient"] = "0x742d35cc..."
    }
});
```

### 🗄️ Storage Service

```csharp
// Store document with encryption
var document = new Document
{
    Name = "smart-contract-v2.json",
    Content = Convert.ToBase64String(contractBytes),
    ContentType = "application/json",
    Metadata = new Dictionary<string, object>
    {
        ["contract_version"] = "2.0",
        ["deployment_network"] = "neo-n3",
        ["classification"] = "public"
    }
};

var storeResult = await client.Storage.StoreDocumentAsync(document);
Console.WriteLine($"Document stored: {storeResult.DocumentId}");

// Retrieve document
var retrievedDoc = await client.Storage.GetDocumentAsync(storeResult.DocumentId);
var contractData = Convert.FromBase64String(retrievedDoc.Content);

// Query documents
var searchResults = await client.Storage.SearchDocumentsAsync(new DocumentSearchRequest
{
    Query = "smart-contract",
    Filters = new Dictionary<string, object>
    {
        ["contract_version"] = "2.0"
    },
    PageSize = 20,
    PageNumber = 1
});

foreach (var doc in searchResults.Documents)
{
    Console.WriteLine($"Found: {doc.Name} (ID: {doc.Id})");
}
```

### 🔑 Key Management Service

```csharp
// Generate new cryptographic key
var keyRequest = new KeyGenerationRequest
{
    Algorithm = KeyAlgorithm.ECDSA_P256,
    Usage = new[] { KeyUsage.Sign, KeyUsage.Verify },
    Metadata = new Dictionary<string, object>
    {
        ["purpose"] = "smart-contract-signing",
        ["environment"] = "production"
    }
};

var keyResult = await client.KeyManagement.GenerateKeyAsync(keyRequest);
Console.WriteLine($"Key generated: {keyResult.KeyId}");

// Sign data
var signRequest = new SignRequest
{
    KeyId = keyResult.KeyId,
    Data = "Hello, Neo blockchain!",
    Algorithm = SigningAlgorithm.SHA256withECDSA
};

var signature = await client.KeyManagement.SignAsync(signRequest);
Console.WriteLine($"Signature: {signature.SignatureValue}");

// Verify signature
var verifyResult = await client.KeyManagement.VerifyAsync(new VerifyRequest
{
    KeyId = keyResult.KeyId,
    Data = "Hello, Neo blockchain!",
    Signature = signature.SignatureValue,
    Algorithm = SigningAlgorithm.SHA256withECDSA
});

Console.WriteLine($"Signature valid: {verifyResult.IsValid}");
```

### 🤖 AI Pattern Recognition

```csharp
// Analyze transaction patterns
var analysisRequest = new PatternAnalysisRequest
{
    DataSource = "blockchain-transactions",
    TimeRange = new TimeRange
    {
        Start = DateTime.UtcNow.AddDays(-30),
        End = DateTime.UtcNow
    },
    AnalysisType = AnalysisType.FraudDetection,
    Parameters = new Dictionary<string, object>
    {
        ["min_transaction_amount"] = 1000,
        ["suspicious_patterns"] = new[] { "rapid_succession", "round_amounts" }
    }
};

var analysis = await client.AI.AnalyzePatternAsync(analysisRequest);

Console.WriteLine($"Analysis completed: {analysis.AnalysisId}");
Console.WriteLine($"Risk score: {analysis.RiskScore}");
Console.WriteLine($"Patterns found: {analysis.PatternsDetected.Count}");

foreach (var pattern in analysis.PatternsDetected)
{
    Console.WriteLine($"- {pattern.Type}: {pattern.Confidence}% confidence");
}
```

### ⛓️ Cross-Chain Bridge

```csharp
// Bridge tokens between Neo N3 and NeoX
var bridgeRequest = new CrossChainBridgeRequest
{
    SourceNetwork = "neo-n3",
    DestinationNetwork = "neo-x",
    Asset = "GAS",
    Amount = 50.0m,
    SourceAddress = "NX8GreRFGFK5wpGMWetpX93HmtrezGogzk",
    DestinationAddress = "0x742d35cc6ab4b16c56b27a8a3cb5db1d3ec0e4a1",
    Metadata = new Dictionary<string, object>
    {
        ["priority"] = "normal",
        ["max_fee"] = 1.0
    }
};

var bridgeResult = await client.CrossChain.InitiateBridgeAsync(bridgeRequest);
Console.WriteLine($"Bridge initiated: {bridgeResult.TransactionId}");

// Monitor bridge progress
var status = await client.CrossChain.GetTransactionStatusAsync(bridgeResult.TransactionId);
while (status.Status == BridgeStatus.Processing)
{
    await Task.Delay(5000);
    status = await client.CrossChain.GetTransactionStatusAsync(bridgeResult.TransactionId);
    Console.WriteLine($"Status: {status.Status}, Progress: {status.ProgressPercentage}%");
}

Console.WriteLine($"Bridge completed: {status.Status}");
```

### 🔮 Oracle Service

```csharp
// Request external data
var oracleRequest = new OracleDataRequest
{
    Source = "coinmarketcap",
    Query = new Dictionary<string, object>
    {
        ["symbol"] = "NEO",
        ["convert"] = "USD",
        ["amount"] = 1
    },
    CallbackUrl = "https://your-app.com/oracle-callback"
};

var oracleResult = await client.Oracle.RequestDataAsync(oracleRequest);
Console.WriteLine($"Oracle request: {oracleResult.RequestId}");

// Subscribe to price feeds
var subscription = await client.Oracle.SubscribeToFeedAsync(new FeedSubscriptionRequest
{
    FeedId = "crypto-prices",
    Symbols = new[] { "NEO", "GAS", "BTC", "ETH" },
    CallbackUrl = "https://your-app.com/price-updates",
    UpdateInterval = TimeSpan.FromMinutes(1)
});

Console.WriteLine($"Subscribed to feed: {subscription.SubscriptionId}");
```

## 🔧 Advanced Configuration

### Service Discovery & Load Balancing

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.GatewayUrl = "http://localhost:7000";
    
    // Enable direct service discovery
    options.EnableServiceDiscovery = true;
    options.ServiceDiscovery = new ServiceDiscoveryOptions
    {
        ConsulUrl = "http://localhost:8500",
        Datacenter = "dc1",
        HealthCheckInterval = TimeSpan.FromSeconds(10)
    };
    
    // Load balancing strategy
    options.LoadBalancing = LoadBalancingStrategy.RoundRobin;
});
```

### Resilience Configuration

```csharp
services.AddNeoServiceLayerClient(options =>
{
    // Retry policy
    options.RetryPolicy = new RetryOptions
    {
        MaxRetries = 3,
        BackoffStrategy = BackoffStrategy.ExponentialBackoff,
        BaseDelay = TimeSpan.FromMilliseconds(500)
    };
    
    // Circuit breaker
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        FailureThreshold = 5,
        RecoveryTimeout = TimeSpan.FromSeconds(30),
        HalfOpenMaxCalls = 3
    };
    
    // Timeout configuration
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.LongRunningTimeout = TimeSpan.FromMinutes(5);
});
```

### Custom HTTP Client Configuration

```csharp
services.AddHttpClient<INeoServiceLayerClient>((serviceProvider, client) =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    UseDefaultCredentials = false
});
```

## 🔐 Authentication & Security

### JWT Authentication (Production)

```csharp
// Login and get JWT token
var authResult = await client.AuthenticateAsync("username", "password");
Console.WriteLine($"Access token expires in: {authResult.ExpiresIn}");

// Refresh token when needed
if (authResult.RefreshToken != null)
{
    var refreshResult = await client.RefreshTokenAsync(authResult.RefreshToken);
    Console.WriteLine("Token refreshed successfully");
}

// Logout
await client.LogoutAsync();
```

### API Key Authentication (Development)

```csharp
// Set API key for development
client.SetApiKey("dev-api-key-12345");

// Or configure during setup
services.AddNeoServiceLayerClient(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("NEO_API_KEY");
});
```

### Custom Authentication Provider

```csharp
public class CustomAuthProvider : IAuthenticationProvider
{
    public async Task<string> GetAuthTokenAsync()
    {
        // Your custom authentication logic
        return await GetTokenFromCustomSource();
    }
    
    public async Task RefreshTokenAsync()
    {
        // Your token refresh logic
        await RefreshCustomToken();
    }
}

// Register custom provider
services.AddSingleton<IAuthenticationProvider, CustomAuthProvider>();
```

## 📊 Observability & Monitoring

### Distributed Tracing

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.Tracing = new TracingOptions
    {
        EnableTracing = true,
        ServiceName = "my-application",
        TraceIdHeader = "X-Trace-Id",
        SpanIdHeader = "X-Span-Id"
    };
});

// All SDK calls will automatically include tracing headers
var result = await client.Storage.GetDocumentAsync("doc-123");
// Trace ID: 123e4567-e89b-12d3-a456-426614174000
```

### Metrics Collection

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.Metrics = new MetricsOptions
    {
        EnableMetrics = true,
        MetricsPrefix = "neo_sdk",
        TrackRequestDuration = true,
        TrackRequestCount = true,
        TrackErrorRate = true
    };
});

// Metrics are automatically collected:
// - neo_sdk_request_duration_seconds
// - neo_sdk_request_total
// - neo_sdk_error_rate
```

### Custom Logging

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.Logging = new LoggingOptions
    {
        LogLevel = LogLevel.Information,
        LogRequestDetails = true,
        LogResponseDetails = false, // Don't log sensitive response data
        SanitizeHeaders = new[] { "Authorization", "X-API-Key" }
    };
});
```

## 🧪 Testing Support

### Unit Testing with Mocks

```csharp
[Test]
public async Task Should_Send_Notification_Successfully()
{
    // Arrange
    var mockClient = new Mock<INeoServiceLayerClient>();
    var mockNotificationService = new Mock<INotificationService>();
    
    mockClient.Setup(x => x.Notification).Returns(mockNotificationService.Object);
    mockNotificationService
        .Setup(x => x.SendAsync(It.IsAny<SendNotificationRequest>()))
        .ReturnsAsync(new NotificationResult { NotificationId = "test-123" });
    
    var service = new MyService(mockClient.Object);
    
    // Act
    var result = await service.SendWelcomeEmail("user@example.com");
    
    // Assert
    Assert.IsTrue(result.Success);
    mockNotificationService.Verify(x => x.SendAsync(It.Is<SendNotificationRequest>(
        req => req.Recipient == "user@example.com")), Times.Once);
}
```

### Integration Testing

```csharp
[Test]
public async Task Integration_Test_With_TestServer()
{
    // Create test client pointing to test server
    var client = new NeoServiceLayerClientBuilder()
        .UseGatewayUrl("http://localhost:7000")
        .UseApiKey("test-api-key")
        .EnableRetries(false) // Disable retries for faster tests
        .Build();
    
    // Test actual service calls
    var health = await client.GetHealthAsync();
    Assert.AreEqual("healthy", health.Status);
    
    var notification = await client.Notification.SendAsync(new SendNotificationRequest
    {
        Channel = NotificationChannel.Email,
        Recipient = "test@example.com",
        Subject = "Test",
        Message = "Integration test message"
    });
    
    Assert.IsNotNull(notification.NotificationId);
}
```

### Test Configuration

```csharp
// appsettings.Test.json
{
  "NeoServiceLayer": {
    "GatewayUrl": "http://localhost:7000",
    "ApiKey": "test-api-key",
    "Timeout": "00:00:05",
    "RetryCount": 0,
    "EnableCircuitBreaker": false
  }
}
```

## 🚀 Performance Optimization

### Connection Pooling

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.ConnectionPool = new ConnectionPoolOptions
    {
        MaxConnectionsPerServer = 10,
        ConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
    };
});
```

### Request Batching

```csharp
// Batch multiple operations
var batch = client.CreateBatch();
batch.Storage.StoreDocument(document1);
batch.Storage.StoreDocument(document2);
batch.Notification.Send(notification1);

var results = await batch.ExecuteAsync();
foreach (var result in results)
{
    Console.WriteLine($"Operation {result.OperationId}: {result.Status}");
}
```

### Caching

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.Caching = new CachingOptions
    {
        EnableResponseCaching = true,
        DefaultCacheDuration = TimeSpan.FromMinutes(5),
        CacheKeyPrefix = "neo_sdk"
    };
});

// Some operations automatically cached:
var config = await client.Configuration.GetAsync("app-setting");
// Subsequent calls within 5 minutes return cached value
```

## 📚 Error Handling

### Standard Error Types

```csharp
try
{
    var result = await client.Storage.GetDocumentAsync("non-existent");
}
catch (NeoServiceNotFoundException ex)
{
    Console.WriteLine($"Document not found: {ex.Message}");
}
catch (NeoServiceAuthenticationException ex)
{
    Console.WriteLine($"Authentication failed: {ex.Message}");
    // Maybe refresh token or re-authenticate
}
catch (NeoServiceRateLimitException ex)
{
    Console.WriteLine($"Rate limited. Retry after: {ex.RetryAfter}");
    await Task.Delay(ex.RetryAfter);
}
catch (NeoServiceException ex)
{
    Console.WriteLine($"Service error: {ex.ErrorCode} - {ex.Message}");
    if (ex.Details != null)
    {
        foreach (var detail in ex.Details)
        {
            Console.WriteLine($"  {detail.Key}: {detail.Value}");
        }
    }
}
```

### Custom Error Handling

```csharp
services.AddNeoServiceLayerClient(options =>
{
    options.ErrorHandling = new ErrorHandlingOptions
    {
        OnError = (exception, context) =>
        {
            // Custom error logging/handling
            Console.WriteLine($"SDK Error: {exception.Message}");
            return Task.CompletedTask;
        },
        OnRetry = (exception, retryCount, context) =>
        {
            Console.WriteLine($"Retrying operation {retryCount} due to: {exception.Message}");
            return Task.CompletedTask;
        }
    };
});
```

## 🔄 Migration Guide

### From Version 1.x to 2.x

```csharp
// Old way (v1.x)
var client = new NeoServiceClient(httpClient, serviceRegistry, logger);
var result = await client.CallServiceAsync<NotificationResult>(
    "Notification", "/api/notification/send", HttpMethod.Post, request);

// New way (v2.x)
services.AddNeoServiceLayerClient(options => { /* config */ });
var client = serviceProvider.GetRequiredService<INeoServiceLayerClient>();
var result = await client.Notification.SendAsync(request);
```

### Configuration Changes

```csharp
// Old configuration (v1.x)
{
  "NeoServiceClient": {
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5
  }
}

// New configuration (v2.x)
{
  "NeoServiceLayer": {
    "GatewayUrl": "http://localhost:7000",
    "RetryPolicy": {
      "MaxRetries": 3
    },
    "CircuitBreaker": {
      "FailureThreshold": 5
    }
  }
}
```

## 🤝 Contributing

We welcome contributions to the Neo Service Layer SDK! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone repository
git clone https://github.com/r3e-network/neo-service-layer.git
cd neo-service-layer

# Build SDK
dotnet build src/SDK/NeoServiceLayer.SDK/

# Run SDK tests
dotnet test tests/Unit/NeoServiceLayer.SDK.Tests/
```

### SDK Guidelines

- Follow established coding patterns
- Add comprehensive tests for new features
- Update documentation for API changes
- Ensure backward compatibility when possible

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## 🙋‍♂️ Support

- **📖 API Documentation**: [API Reference](../../docs/api/README.md)
- **🐛 Issues**: [GitHub Issues](https://github.com/r3e-network/neo-service-layer/issues)
- **💬 SDK Support**: [GitHub Discussions](https://github.com/r3e-network/neo-service-layer/discussions)
- **📧 Contact**: sdk-support@r3e.network

---

**🚀 Ready to build with Neo Service Layer? [Get started with our Quick Start Guide](../../docs/deployment/QUICK_START.md)!**