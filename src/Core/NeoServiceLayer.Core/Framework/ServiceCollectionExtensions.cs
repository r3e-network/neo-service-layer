using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Caching;
using NeoServiceLayer.Core.ConfidentialComputing;
using NeoServiceLayer.Core.Cryptography;
using NeoServiceLayer.Core.Gateway;
using NeoServiceLayer.Core.Messaging;
using NeoServiceLayer.Core.Monitoring;
using NeoServiceLayer.Core.Resilience;
using NeoServiceLayer.Core.EventSourcing;
using NeoServiceLayer.Core.ServiceMesh;
using NeoServiceLayer.Core.MultiTenant;
using System;

namespace NeoServiceLayer.Core.Framework
{
    /// <summary>
    /// Extension methods for registering all Neo Service Layer framework services
    /// Provides a unified way to configure and integrate all critical services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the complete Neo Service Layer framework with all services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNeoServiceLayerFramework(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add confidential computing and SGX services
            services.AddConfidentialComputing(configuration);

            // Add cryptographic services
            services.AddCryptographicServices(configuration);

            // Add distributed caching
            services.AddDistributedCachingService(configuration);

            // Add message queuing
            services.AddMessageQueueService(configuration);

            // Add monitoring and observability
            services.AddMonitoringService(configuration);

            // Add API gateway
            services.AddApiGatewayService(configuration);

            // Add event sourcing
            services.AddEventSourcingService(configuration);

            // Add service mesh integration
            services.AddServiceMeshIntegration(configuration);

            // Add resilience patterns
            services.AddResiliencePatterns(configuration);

            // Add multi-tenancy support
            services.AddMultiTenantServices(configuration);

            // Add framework health checks
            services.AddFrameworkHealthChecks();

            // Add framework hosted services
            services.AddFrameworkHostedServices();

            return services;
        }

        /// <summary>
        /// Adds cryptographic services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCryptographicServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<CryptographicServiceConfiguration>(
                configuration.GetSection("CryptographicService"));

            services.AddSingleton<ICryptographicService, CryptographicService>();

            services.AddHealthChecks()
                .AddCheck<CryptographicServiceHealthCheck>("cryptographic-service");

            return services;
        }

        /// <summary>
        /// Adds distributed caching services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddDistributedCachingService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<DistributedCachingConfiguration>(
                configuration.GetSection("DistributedCaching"));

            services.AddSingleton<IDistributedCachingService, DistributedCachingService>();

            services.AddHealthChecks()
                .AddCheck<DistributedCachingHealthCheck>("distributed-caching");

            return services;
        }

        /// <summary>
        /// Adds message queue services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMessageQueueService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MessageQueueConfiguration>(
                configuration.GetSection("MessageQueue"));

            services.AddSingleton<IMessageQueueService, MessageQueueService>();

            services.AddHealthChecks()
                .AddCheck<MessageQueueHealthCheck>("message-queue");

            services.AddHostedService<MessageQueueMaintenanceService>();

            return services;
        }

        /// <summary>
        /// Adds monitoring services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMonitoringService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MonitoringConfiguration>(
                configuration.GetSection("Monitoring"));

            services.AddSingleton<IMonitoringService, MonitoringService>();

            services.AddHealthChecks()
                .AddCheck<MonitoringServiceHealthCheck>("monitoring-service");

            services.AddHostedService<MonitoringMaintenanceService>();

            return services;
        }

        /// <summary>
        /// Adds API gateway services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddApiGatewayService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ApiGatewayConfiguration>(
                configuration.GetSection("ApiGateway"));

            services.AddSingleton<IApiGatewayService, ApiGatewayService>();

            services.AddHealthChecks()
                .AddCheck<ApiGatewayHealthCheck>("api-gateway");

            services.AddHostedService<ApiGatewayMaintenanceService>();

            return services;
        }

        /// <summary>
        /// Adds event sourcing services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventSourcingService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EventSourcingConfiguration>(
                configuration.GetSection("EventSourcing"));

            services.AddSingleton<IEventSourcingService, EventSourcingService>();

            services.AddHealthChecks()
                .AddCheck<EventSourcingHealthCheck>("event-sourcing");

            return services;
        }

        /// <summary>
        /// Adds service mesh integration to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddServiceMeshIntegration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ServiceMeshConfiguration>(
                configuration.GetSection("ServiceMesh"));

            services.AddSingleton<IServiceMeshService, ServiceMeshService>();

            services.AddHealthChecks()
                .AddCheck<ServiceMeshHealthCheck>("service-mesh");

            return services;
        }

        /// <summary>
        /// Adds resilience patterns to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddResiliencePatterns(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ResilienceConfiguration>(
                configuration.GetSection("Resilience"));

            services.AddSingleton<IResilienceService, ResilienceService>();
            services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
            services.AddSingleton<IRetryService, RetryService>();
            services.AddSingleton<IBulkheadService, BulkheadService>();
            services.AddSingleton<ITimeoutService, TimeoutService>();

            services.AddHealthChecks()
                .AddCheck<ResilienceServiceHealthCheck>("resilience-patterns");

            return services;
        }

        /// <summary>
        /// Adds multi-tenant services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMultiTenantServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MultiTenantConfiguration>(
                configuration.GetSection("MultiTenant"));

            services.AddSingleton<ITenantService, TenantService>();
            services.AddSingleton<ITenantResolver, TenantResolver>();
            services.AddSingleton<ITenantStore, TenantStore>();
            services.AddScoped<ITenantContext, TenantContext>();

            services.AddHealthChecks()
                .AddCheck<MultiTenantHealthCheck>("multi-tenant");

            return services;
        }

        /// <summary>
        /// Adds framework-wide health checks
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        private static IServiceCollection AddFrameworkHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<FrameworkHealthCheck>("neo-service-layer-framework");

            return services;
        }

        /// <summary>
        /// Adds framework hosted services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        private static IServiceCollection AddFrameworkHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<FrameworkMaintenanceService>();
            services.AddHostedService<ServiceDiscoveryService>();
            services.AddHostedService<ConfigurationSyncService>();

            return services;
        }

        /// <summary>
        /// Adds minimal Neo Service Layer framework for development/testing
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNeoServiceLayerMinimal(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add only essential services for development
            services.AddCryptographicServices(configuration);
            services.AddDistributedCachingService(configuration);
            services.AddMonitoringService(configuration);

            services.AddHealthChecks()
                .AddCheck<FrameworkHealthCheck>("neo-service-layer-minimal");

            return services;
        }

        /// <summary>
        /// Adds Neo Service Layer framework with custom service selection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <param name="configureServices">Service selection configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNeoServiceLayerCustom(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<FrameworkServicesBuilder> configureServices)
        {
            var builder = new FrameworkServicesBuilder(services, configuration);
            configureServices(builder);
            return services;
        }
    }

    /// <summary>
    /// Builder for custom service selection
    /// </summary>
    public class FrameworkServicesBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public FrameworkServicesBuilder(IServiceCollection services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public FrameworkServicesBuilder AddConfidentialComputing()
        {
            _services.AddConfidentialComputing(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddCryptography()
        {
            _services.AddCryptographicServices(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddCaching()
        {
            _services.AddDistributedCachingService(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddMessaging()
        {
            _services.AddMessageQueueService(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddMonitoring()
        {
            _services.AddMonitoringService(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddApiGateway()
        {
            _services.AddApiGatewayService(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddEventSourcing()
        {
            _services.AddEventSourcingService(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddServiceMesh()
        {
            _services.AddServiceMeshIntegration(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddResilience()
        {
            _services.AddResiliencePatterns(_configuration);
            return this;
        }

        public FrameworkServicesBuilder AddMultiTenancy()
        {
            _services.AddMultiTenantServices(_configuration);
            return this;
        }
    }

    /// <summary>
    /// Configuration classes for various services
    /// </summary>
    public class CryptographicServiceConfiguration
    {
        public bool UseHardwareBacking { get; set; } = true;
        public string DefaultKeyAlgorithm { get; set; } = "ECDSA_P256";
        public string DefaultEncryptionAlgorithm { get; set; } = "AES256GCM";
        public string DefaultHashAlgorithm { get; set; } = "SHA256";
        public bool EnableMetrics { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    }

    public class DistributedCachingConfiguration
    {
        public string Provider { get; set; } = "Redis";
        public string ConnectionString { get; set; } = string.Empty;
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = false;
        public int MaxConcurrentConnections { get; set; } = 10;
    }

    public class MessageQueueConfiguration
    {
        public string Provider { get; set; } = "RabbitMQ";
        public string ConnectionString { get; set; } = string.Empty;
        public bool EnablePersistence { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan MessageTtl { get; set; } = TimeSpan.FromHours(24);
        public bool EnableDeadLetterQueue { get; set; } = true;
    }

    public class MonitoringConfiguration
    {
        public string Provider { get; set; } = "Prometheus";
        public string MetricsEndpoint { get; set; } = "/metrics";
        public TimeSpan MetricsScrapeInterval { get; set; } = TimeSpan.FromSeconds(15);
        public bool EnableTracing { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Information";
    }

    public class ApiGatewayConfiguration
    {
        public string ListenAddress { get; set; } = "0.0.0.0";
        public int ListenPort { get; set; } = 8080;
        public bool EnableTls { get; set; } = true;
        public bool EnableRateLimiting { get; set; } = true;
        public bool EnableAuthentication { get; set; } = true;
        public bool EnableLoadBalancing { get; set; } = true;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    public class EventSourcingConfiguration
    {
        public string EventStore { get; set; } = "EventStoreDB";
        public string ConnectionString { get; set; } = string.Empty;
        public bool EnableSnapshots { get; set; } = true;
        public int SnapshotFrequency { get; set; } = 100;
        public bool EnableProjections { get; set; } = true;
        public bool EnableEventUpgrading { get; set; } = true;
    }

    public class ServiceMeshConfiguration
    {
        public string Provider { get; set; } = "Istio";
        public bool EnableMtls { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;
        public bool EnableCircuitBreaker { get; set; } = true;
        public string Namespace { get; set; } = "neo-service-layer";
    }

    public class ResilienceConfiguration
    {
        public bool EnableCircuitBreaker { get; set; } = true;
        public bool EnableRetry { get; set; } = true;
        public bool EnableTimeout { get; set; } = true;
        public bool EnableBulkhead { get; set; } = true;
        public bool EnableRateLimiting { get; set; } = true;
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    public class MultiTenantConfiguration
    {
        public string IdentificationStrategy { get; set; } = "Header";
        public string TenantHeader { get; set; } = "X-Tenant-Id";
        public bool EnableTenantIsolation { get; set; } = true;
        public bool EnablePerTenantConfiguration { get; set; } = true;
        public string DefaultTenant { get; set; } = "default";
    }

    /// <summary>
    /// Health check implementations
    /// </summary>
    public class FrameworkHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement overall framework health check
            return Task.FromResult(HealthCheckResult.Healthy("Neo Service Layer Framework is healthy"));
        }
    }

    public class CryptographicServiceHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement cryptographic service health check
            return Task.FromResult(HealthCheckResult.Healthy("Cryptographic service is healthy"));
        }
    }

    public class DistributedCachingHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement caching service health check
            return Task.FromResult(HealthCheckResult.Healthy("Distributed caching is healthy"));
        }
    }

    public class MessageQueueHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement message queue health check
            return Task.FromResult(HealthCheckResult.Healthy("Message queue is healthy"));
        }
    }

    public class MonitoringServiceHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement monitoring service health check
            return Task.FromResult(HealthCheckResult.Healthy("Monitoring service is healthy"));
        }
    }

    public class ApiGatewayHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement API gateway health check
            return Task.FromResult(HealthCheckResult.Healthy("API gateway is healthy"));
        }
    }

    public class EventSourcingHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement event sourcing health check
            return Task.FromResult(HealthCheckResult.Healthy("Event sourcing is healthy"));
        }
    }

    public class ServiceMeshHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement service mesh health check
            return Task.FromResult(HealthCheckResult.Healthy("Service mesh is healthy"));
        }
    }

    public class ResilienceServiceHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement resilience patterns health check
            return Task.FromResult(HealthCheckResult.Healthy("Resilience patterns are healthy"));
        }
    }

    public class MultiTenantHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Implement multi-tenant health check
            return Task.FromResult(HealthCheckResult.Healthy("Multi-tenant service is healthy"));
        }
    }

    /// <summary>
    /// Background services
    /// </summary>
    public class FrameworkMaintenanceService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform framework-wide maintenance tasks
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    public class ServiceDiscoveryService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform service discovery and registration
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    public class ConfigurationSyncService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Synchronize configuration across services
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // Additional service maintenance classes
    public class MessageQueueMaintenanceService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform message queue maintenance
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }

    public class MonitoringMaintenanceService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform monitoring service maintenance
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    public class ApiGatewayMaintenanceService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Perform API gateway maintenance
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}