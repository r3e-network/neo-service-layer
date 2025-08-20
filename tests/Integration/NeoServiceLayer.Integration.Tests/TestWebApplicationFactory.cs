using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Test web application factory for integration tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly ITestOutputHelper? _testOutputHelper;

    public TestWebApplicationFactory(ITestOutputHelper? testOutputHelper = null)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration
            config.Sources.Clear();

            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
                
                // Test database configuration (use in-memory or test database)
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=nsl_test;Username=test;Password=test",
                
                // Test Redis configuration
                ["Redis:Configuration"] = "localhost:6379",
                ["Redis:InstanceName"] = "NSL_Test",
                
                // Test JWT configuration
                ["Jwt:Key"] = "SuperSecretTestKeyThatIsLongEnoughForJWTTokenGenerationAndSigningInTestEnvironment2024!",
                ["Jwt:Issuer"] = "NeoServiceLayerTest",
                ["Jwt:Audience"] = "NeoServiceLayerTestClients",
                
                // Disable external dependencies for testing
                ["ServiceDiscovery:Enabled"] = "false",
                ["RabbitMQ:Enabled"] = "false",
                
                // Test health check configuration
                ["HealthChecks:Enabled"] = "true",
                ["HealthChecks:DetailedErrors"] = "true",
                
                // Cache configuration for testing
                ["Caching:Provider"] = "Memory",
                ["Caching:DefaultExpiration"] = "00:05:00",
                
                // Rate limiting configuration for testing
                ["RateLimiting:Enabled"] = "false", // Disable for tests
                
                // Circuit breaker configuration
                ["CircuitBreaker:Enabled"] = "false", // Disable for tests
            });

            // Add environment variables
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // Configure test logging
            if (_testOutputHelper != null)
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new XunitLoggerProvider(_testOutputHelper));
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            }

            // Replace production services with test doubles
            ConfigureTestServices(services);
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Configure test-specific services.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override any services that need test-specific implementations
        // For example, replace external API clients with mocks
        
        // Configure test cache (use in-memory cache for tests)
        services.Configure<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.2;
        });

        // Add test-specific health checks
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            options.Registrations.Clear(); // Remove production health checks that require external dependencies
        });
    }

    /// <summary>
    /// Get a required service from the test service provider.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Get a service from the test service provider.
    /// </summary>
    public T? GetService<T>()
    {
        return Services.GetService<T>();
    }

    /// <summary>
    /// Initialize the test application factory.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Perform any async initialization needed
        // For example, seed test data, setup test databases, etc.
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clean up the test application factory.
    /// </summary>
    public async Task DisposeAsync()
    {
        // Perform cleanup
        await Task.CompletedTask;
    }
}

/// <summary>
/// Xunit logger provider for test output.
/// </summary>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_testOutputHelper, categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Xunit logger implementation.
/// </summary>
public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new NoOpDisposable();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }

            _testOutputHelper.WriteLine(logEntry);
        }
        catch
        {
            // Ignore any exceptions in test logging
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            // No-op
        }
    }
}

/// <summary>
/// Base class for integration tests.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly ITestOutputHelper Output;
    protected readonly TestWebApplicationFactory Factory;
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        Output = output;
        Factory = new TestWebApplicationFactory(output);
    }

    public virtual async Task InitializeAsync()
    {
        await Factory.InitializeAsync();
        Client = Factory.CreateClient();
    }

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Get a required service from the test container.
    /// </summary>
    protected T GetRequiredService<T>() where T : notnull
    {
        return Factory.GetRequiredService<T>();
    }

    /// <summary>
    /// Get a service from the test container.
    /// </summary>
    protected T? GetService<T>()
    {
        return Factory.GetService<T>();
    }

    /// <summary>
    /// Create a new HTTP client for testing.
    /// </summary>
    protected HttpClient CreateClient()
    {
        return Factory.CreateClient();
    }
}

/// <summary>
/// Collection of test utilities for integration tests.
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Generate a unique test identifier.
    /// </summary>
    public static string GenerateTestId()
    {
        return $"test_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Wait for a condition to be true with timeout.
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition, 
        TimeSpan timeout, 
        TimeSpan interval = default)
    {
        if (interval == default)
        {
            interval = TimeSpan.FromMilliseconds(100);
        }

        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (condition())
            {
                return true;
            }
            
            await Task.Delay(interval);
        }
        
        return false;
    }

    /// <summary>
    /// Wait for an async condition to be true with timeout.
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition, 
        TimeSpan timeout, 
        TimeSpan interval = default)
    {
        if (interval == default)
        {
            interval = TimeSpan.FromMilliseconds(100);
        }

        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (await condition())
            {
                return true;
            }
            
            await Task.Delay(interval);
        }
        
        return false;
    }
}