using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core.ConfidentialComputing;
using NeoServiceLayer.Core.ConfidentialStorage;
using NeoServiceLayer.Core.Cryptography;
using NeoServiceLayer.Core.Monitoring;
using NeoServiceLayer.Core.Caching;
using NeoServiceLayer.Core.Messaging;
using NeoServiceLayer.Core.Gateway;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Services.Oracle.Extensions;

/// <summary>
/// Service collection extensions for Oracle service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Oracle service with framework integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddOracleService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register Oracle service
        services.AddScoped<IOracleService, OracleService>();
        
        // Register Oracle service as hosted service
        services.AddHostedService<OracleServiceWorker>();
        
        // Configure Oracle-specific options
        services.Configure<OracleServiceOptions>(
            configuration.GetSection("Oracle"));

        // Add health checks for Oracle service
        services.AddHealthChecks()
            .AddCheck<OracleServiceHealthCheck>("oracle-service")
            .AddCheck<OracleEnclaveHealthCheck>("oracle-enclave")
            .AddCheck<OracleDataSourceHealthCheck>("oracle-datasources");

        return services;
    }

    /// <summary>
    /// Adds Oracle service with full framework integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddOracleServiceWithFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add core framework services if not already added
        if (!services.Any(x => x.ServiceType == typeof(IConfidentialComputingService)))
        {
            services.AddScoped<IConfidentialComputingService, ConfidentialComputingService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IConfidentialStorageService)))
        {
            services.AddScoped<IConfidentialStorageService, ConfidentialStorageService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(ICryptographicService)))
        {
            services.AddScoped<ICryptographicService, CryptographicService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IMonitoringService)))
        {
            services.AddScoped<IMonitoringService, MonitoringService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IDistributedCachingService)))
        {
            services.AddScoped<IDistributedCachingService, DistributedCachingService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IMessageQueueService)))
        {
            services.AddScoped<IMessageQueueService, MessageQueueService>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IApiGatewayService)))
        {
            services.AddScoped<IApiGatewayService, ApiGatewayService>();
        }

        // Add Oracle service with framework dependencies
        return services.AddOracleService(configuration);
    }
}

/// <summary>
/// Oracle service configuration options.
/// </summary>
public class OracleServiceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests per batch.
    /// </summary>
    public int MaxRequestsPerBatch { get; set; } = 10;

    /// <summary>
    /// Gets or sets the default request timeout in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to enable enclave-based operations.
    /// </summary>
    public bool EnableEnclaveOperations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable privacy-preserving features.
    /// </summary>
    public bool EnablePrivacyFeatures { get; set; } = true;

    /// <summary>
    /// Gets or sets the data source validation timeout in seconds.
    /// </summary>
    public int DataSourceValidationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
}