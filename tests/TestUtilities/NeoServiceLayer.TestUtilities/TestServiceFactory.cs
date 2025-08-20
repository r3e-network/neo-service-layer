using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.Infrastructure.Security;
using NeoServiceLayer.Infrastructure.Resilience;
using NeoServiceLayer.Infrastructure.Monitoring;
using NeoServiceLayer.Tee.Enclave;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.TestUtilities;

/// <summary>
/// Factory for creating test instances of core services with proper configuration.
/// Provides common test utilities and fixtures for comprehensive service testing.
/// </summary>
public static class TestServiceFactory
{
    /// <summary>
    /// Creates a configured SecurityService for testing.
    /// </summary>
    public static SecurityService CreateSecurityService(ILogger<SecurityService>? logger = null)
    {
        return new SecurityService(logger ?? new NullLogger<SecurityService>());
    }

    /// <summary>
    /// Creates a configured ResilienceService for testing.
    /// </summary>
    public static ResilienceService CreateResilienceService(ILogger<ResilienceService>? logger = null)
    {
        return new ResilienceService(logger ?? new NullLogger<ResilienceService>());
    }

    /// <summary>
    /// Creates a configured ObservabilityService for testing.
    /// </summary>
    public static ObservabilityService CreateObservabilityService(ILogger<ObservabilityService>? logger = null)
    {
        return new ObservabilityService(logger ?? new NullLogger<ObservabilityService>());
    }

    /// <summary>
    /// Creates a ProductionSGXEnclaveWrapper for testing (with simulation mode).
    /// </summary>
    public static ProductionSGXEnclaveWrapper CreateEnclaveWrapper(ILogger<ProductionSGXEnclaveWrapper>? logger = null)
    {
        var wrapper = new ProductionSGXEnclaveWrapper(logger ?? new NullLogger<ProductionSGXEnclaveWrapper>());
        wrapper.Initialize(); // Initialize in simulation mode for tests
        return wrapper;
    }

    /// <summary>
    /// Creates a test configuration with security settings.
    /// </summary>
    public static IConfiguration CreateTestConfiguration(Dictionary<string, string>? customSettings = null)
    {
        var settings = new Dictionary<string, string>
        {
            ["Security:EncryptionAlgorithm"] = "AES-256-GCM",
            ["Security:KeyRotationIntervalHours"] = "24",
            ["Security:MaxInputSizeMB"] = "10",
            ["Security:EnableRateLimiting"] = "true",
            ["Security:DefaultRateLimitRequests"] = "100",
            ["Security:RateLimitWindowMinutes"] = "1",
            
            ["Resilience:DefaultMaxRetries"] = "3",
            ["Resilience:DefaultBackoffMs"] = "1000",
            ["Resilience:CircuitBreakerFailureThreshold"] = "5",
            ["Resilience:CircuitBreakerTimeoutMinutes"] = "1",
            ["Resilience:CircuitBreakerResetTimeoutMinutes"] = "5",
            
            ["Observability:EnableTracing"] = "true",
            ["Observability:EnableMetrics"] = "true",
            ["Observability:EnableHealthChecks"] = "true",
            ["Observability:ServiceName"] = "NeoServiceLayer.Test",
            ["Observability:ServiceVersion"] = "1.0.0-test",
            
            ["Tee:EnclaveType"] = "SGX",
            ["Tee:EnclavePath"] = "./test-enclave",
            ["Tee:DebugMode"] = "true",
            ["Tee:MaxEnclaveMemoryMB"] = "256",
            ["Tee:EnableAttestation"] = "false" // Disable for tests
        };

        // Add or override with custom settings
        if (customSettings != null)
        {
            foreach (var kvp in customSettings)
            {
                settings[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    /// <summary>
    /// Creates a service collection with all Neo Service Layer services configured for testing.
    /// </summary>
    public static IServiceCollection CreateTestServiceCollection(IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        var config = configuration ?? CreateTestConfiguration();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add Neo Service Layer services
        services.AddNeoServiceLayer(config);

        return services;
    }

    /// <summary>
    /// Creates a configured service provider for testing.
    /// </summary>
    public static IServiceProvider CreateTestServiceProvider(IConfiguration? configuration = null)
    {
        var services = CreateTestServiceCollection(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates test data for various testing scenarios.
    /// </summary>
    public static class TestData
    {
        public static readonly byte[] SmallTestData = System.Text.Encoding.UTF8.GetBytes("Small test data");
        public static readonly byte[] MediumTestData = System.Text.Encoding.UTF8.GetBytes(new string('A', 1024)); // 1KB
        public static readonly byte[] LargeTestData = System.Text.Encoding.UTF8.GetBytes(new string('B', 1024 * 1024)); // 1MB

        public static readonly string[] SqlInjectionAttempts = 
        {
            "'; DROP TABLE users; --",
            "1' OR '1'='1",
            "UNION SELECT password FROM admin_users",
            "'; INSERT INTO logs VALUES ('attack'); --"
        };

        public static readonly string[] XssAttempts = 
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert(1)>",
            "javascript:alert('xss')",
            "<iframe src='javascript:alert(1)'></iframe>",
            "onclick=\"alert('xss')\""
        };

        public static readonly string[] CodeInjectionAttempts = 
        {
            "System.IO.File.Delete('important.txt')",
            "Process.Start('cmd.exe')",
            "Assembly.Load",
            "typeof(System.Reflection.Assembly)",
            "Environment.Exit(0)"
        };

        public static readonly string[] SafeInputs = 
        {
            "Normal text input",
            "user@example.com",
            "John Doe",
            "A regular sentence with punctuation.",
            "12345",
            "Valid HTML: <p>Hello World</p>"
        };

        public static readonly string[] TestPasswords = 
        {
            "SecurePassword123!",
            "AnotherStrongP@ssw0rd",
            "Compl3x$ecur1ty",
            "MyV3ryStr0ng!P@ssw0rd"
        };

        public static readonly string[] TestJavaScriptCode = 
        {
            "const result = 2 + 2; result;",
            "Math.sqrt(16)",
            "JSON.parse(data).value * 2",
            "const x = 10; const y = 5; x + y;",
            "new Date().getTime()"
        };
    }
}

/// <summary>
/// Test fixture providing configured services for integration tests.
/// </summary>
public class TestServiceFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public SecurityService SecurityService { get; }
    public ResilienceService ResilienceService { get; }
    public ObservabilityService ObservabilityService { get; }
    public ProductionSGXEnclaveWrapper EnclaveWrapper { get; }

    public TestServiceFixture(Dictionary<string, string>? customSettings = null)
    {
        Configuration = TestServiceFactory.CreateTestConfiguration(customSettings);
        ServiceProvider = TestServiceFactory.CreateTestServiceProvider(Configuration);

        // Get services from provider
        SecurityService = ServiceProvider.GetRequiredService<ISecurityService>() as SecurityService 
            ?? throw new InvalidOperationException("SecurityService not found");
        ResilienceService = ServiceProvider.GetRequiredService<IResilienceService>() as ResilienceService 
            ?? throw new InvalidOperationException("ResilienceService not found");
        ObservabilityService = ServiceProvider.GetRequiredService<IObservabilityService>() as ObservabilityService 
            ?? throw new InvalidOperationException("ObservabilityService not found");

        // Create enclave wrapper separately as it may not be in DI
        EnclaveWrapper = TestServiceFactory.CreateEnclaveWrapper();
    }

    public void Dispose()
    {
        SecurityService?.Dispose();
        ResilienceService?.Dispose();
        ObservabilityService?.Dispose();
        EnclaveWrapper?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Helper class for testing async operations with timeouts.
/// </summary>
public static class AsyncTestHelpers
{
    /// <summary>
    /// Executes an async operation with a timeout.
    /// </summary>
    public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout, string operationName = "Operation")
    {
        
        try
        {
            return await task;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"{operationName} timed out after {timeout}");
        }
    }

    /// <summary>
    /// Executes an async operation with a timeout.
    /// </summary>
    public static async Task WithTimeout(Task task, TimeSpan timeout, string operationName = "Operation")
    {
        
        try
        {
            await task;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"{operationName} timed out after {timeout}");
        }
    }

    /// <summary>
    /// Runs multiple async operations concurrently and waits for all to complete.
    /// </summary>
    public static async Task<T[]> ConcurrentExecution<T>(IEnumerable<Func<Task<T>>> operations, int maxConcurrency = 10)
    {
        var tasks = operations.Select(async operation =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await operation();
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Mock implementation of external dependencies for testing.
/// </summary>
public static class TestMocks
{
    /// <summary>
    /// Creates a mock configuration with test values.
    /// </summary>
    public static IConfiguration CreateMockConfiguration()
    {
        return TestServiceFactory.CreateTestConfiguration();
    }

    /// <summary>
    /// Creates a test logger that captures log entries.
    /// </summary>
    public static TestLogger<T> CreateTestLogger<T>()
    {
        return new TestLogger<T>();
    }
}

/// <summary>
/// Test logger implementation that captures log entries for verification.
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logEntries = new();

    public IReadOnlyList<LogEntry> LogEntries => _logEntries;

    public IDisposable BeginScope<TState>(TState state) => new TestScope();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logEntries.Add(new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Clear()
    {
        _logEntries.Clear();
    }

    private class TestScope : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Represents a captured log entry for verification in tests.
/// </summary>
public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}