using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Caching
{
    /// <summary>
    /// Extension methods for configuring caching services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds memory caching services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Optional configuration section.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddMemoryCache(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            // Configure memory cache options
            services.AddMemoryCache(options =>
            {
                if (configuration != null)
                {
                    var cacheConfig = configuration.GetSection("MemoryCache");
                    var sizeLimit = cacheConfig.GetValue<long?>("SizeLimit");
                    var compactionPercentage = cacheConfig.GetValue<double?>("CompactionPercentage");

                    if (sizeLimit.HasValue)
                        options.SizeLimit = sizeLimit.Value;

                    if (compactionPercentage.HasValue)
                        options.CompactionPercentage = compactionPercentage.Value;
                }
                else
                {
                    // Default configuration
                    options.SizeLimit = 100 * 1024 * 1024; // 100 MB
                    options.CompactionPercentage = 0.25; // Remove 25% when full
                }
            });

            // Configure cache service options
            if (configuration != null)
            {
                services.Configure<MemoryCacheServiceOptions>(options =>
                {
                    configuration.GetSection("MemoryCacheService").Bind(options);
                });
            }
            else
            {
                services.Configure<MemoryCacheServiceOptions>(options =>
                {
                    // Use default options
                });
            }

            // Register cache service
            services.AddSingleton<ICacheService, MemoryCacheService>();

            return services;
        }

        /// <summary>
        /// Adds distributed caching services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration section for distributed cache.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddDistributedCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var cacheType = configuration.GetValue<string>("Type")?.ToLowerInvariant();

            switch (cacheType)
            {
                case "redis":
                    AddRedisCache(services, configuration);
                    break;
                
                case "sqlserver":
                    AddSqlServerCache(services, configuration);
                    break;
                
                case "memory":
                default:
                    // Fallback to memory cache
                    services.AddMemoryCache(configuration);
                    break;
            }

            return services;
        }

        /// <summary>
        /// Adds Redis distributed caching.
        /// </summary>
        private static void AddRedisCache(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Redis");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                    options.InstanceName = configuration.GetValue<string>("InstanceName") ?? "NeoServiceLayer";
                });

                // Register distributed cache service wrapper
                services.AddSingleton<ICacheService, DistributedCacheService>();
            }
            else
            {
                throw new InvalidOperationException("Redis connection string is required for Redis caching");
            }
        }

        /// <summary>
        /// Adds SQL Server distributed caching.
        /// </summary>
        private static void AddSqlServerCache(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = connectionString;
                    options.SchemaName = configuration.GetValue<string>("SchemaName") ?? "dbo";
                    options.TableName = configuration.GetValue<string>("TableName") ?? "DistributedCache";
                });

                // Register distributed cache service wrapper
                services.AddSingleton<ICacheService, DistributedCacheService>();
            }
            else
            {
                throw new InvalidOperationException("SQL Server connection string is required for SQL Server caching");
            }
        }

        /// <summary>
        /// Adds caching with automatic configuration detection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddNeoServiceLayerCaching(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var cachingSection = configuration.GetSection("Caching");
            
            if (cachingSection.Exists())
            {
                var distributedCacheSection = cachingSection.GetSection("Distributed");
                
                if (distributedCacheSection.Exists())
                {
                    services.AddDistributedCache(distributedCacheSection);
                }
                else
                {
                    services.AddMemoryCache(cachingSection.GetSection("Memory"));
                }
            }
            else
            {
                // Default to memory cache
                services.AddMemoryCache();
            }

            return services;
        }
    }
}