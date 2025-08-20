using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Services;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Configuration
{
    /// <summary>
    /// Rate limiting configuration and service registration
    /// </summary>
    public static class RateLimitingConfiguration
    {
        /// <summary>
        /// Add rate limiting services to DI container
        /// </summary>
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register Redis connection manager
            services.AddSingleton<RedisConnectionManager>();
            services.AddHostedService(provider => provider.GetRequiredService<RedisConnectionManager>());

            // Register Redis connection
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var connectionManager = provider.GetRequiredService<RedisConnectionManager>();
                return connectionManager.Connection;
            });

            // Register Redis connection factory
            services.AddSingleton<RedisConnectionFactory>();

            // Register rate limiting service
            services.AddSingleton<IRateLimitingService, RateLimitingService>();

            // Register distributed cache (Redis)
            services.AddStackExchangeRedisCache(options =>
            {
                var redisConfig = configuration.GetSection("Redis");

                if (redisConfig.Exists())
                {
                    var host = redisConfig["Host"] ?? "localhost";
                    var port = redisConfig["Port"] ?? "6379";
                    var password = redisConfig["Password"];
                    var ssl = redisConfig.GetValue<bool>("Ssl", false);
                    var database = redisConfig.GetValue<int>("Database", 0);

                    options.Configuration = $"{host}:{port},abortConnect=false,defaultDatabase={database}";

                    if (!string.IsNullOrEmpty(password))
                    {
                        options.Configuration += $",password={password}";
                    }

                    if (ssl)
                    {
                        options.Configuration += ",ssl=true";
                    }

                    options.InstanceName = configuration["Redis:InstanceName"] ?? "NeoServiceLayer";
                }
                else
                {
                    options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                    options.InstanceName = "NeoServiceLayer";
                }
            });

            // Configure rate limiting options
            services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimiting"));

            return services;
        }
    }

    /// <summary>
    /// Rate limiting configuration options
    /// </summary>
    public class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;
        public bool EnableDistributed { get; set; } = true;
        public bool EnableIpWhitelist { get; set; } = false;
        public string[] WhitelistedIps { get; set; } = Array.Empty<string>();
        public string[] ExcludedPaths { get; set; } = Array.Empty<string>();

        // Default limits
        public int RequestsPerMinute { get; set; } = 60;
        public int RequestsPerHour { get; set; } = 1000;
        public int RequestsPerDay { get; set; } = 10000;
        public int BurstSize { get; set; } = 10;

        // IP-based policies
        public Dictionary<string, RateLimitPolicyOptions> IpPolicies { get; set; } = new();

        // User-based policies
        public Dictionary<string, RateLimitPolicyOptions> UserPolicies { get; set; } = new();

        // Endpoint-specific policies
        public Dictionary<string, RateLimitPolicyOptions> EndpointPolicies { get; set; } = new();

        // Auto-blocking configuration
        public bool EnableAutoBlocking { get; set; } = true;
        public int ViolationThreshold { get; set; } = 50;
        public int BlockDurationMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Rate limit policy options
    /// </summary>
    public class RateLimitPolicyOptions
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
        public int RequestsPerDay { get; set; }
        public int BurstSize { get; set; }
        public bool AllowBurst { get; set; } = true;
        public string Description { get; set; }
    }

    /// <summary>
    /// Default rate limiting policies
    /// </summary>
    public static class DefaultRateLimitPolicies
    {
        public static Dictionary<string, RateLimitPolicyOptions> GetDefaultIpPolicies()
        {
            return new Dictionary<string, RateLimitPolicyOptions>
            {
                ["default"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 30,
                    RequestsPerHour = 500,
                    RequestsPerDay = 5000,
                    BurstSize = 5,
                    Description = "Default IP rate limit"
                },
                ["login"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 5,
                    RequestsPerHour = 20,
                    RequestsPerDay = 100,
                    BurstSize = 2,
                    Description = "Login attempts rate limit"
                },
                ["register"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 2,
                    RequestsPerHour = 10,
                    RequestsPerDay = 20,
                    BurstSize = 1,
                    Description = "Registration rate limit"
                },
                ["password-reset"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 2,
                    RequestsPerHour = 5,
                    RequestsPerDay = 10,
                    BurstSize = 1,
                    Description = "Password reset rate limit"
                },
                ["api-read"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 60,
                    RequestsPerHour = 1000,
                    RequestsPerDay = 10000,
                    BurstSize = 10,
                    Description = "API read operations rate limit"
                },
                ["api-write"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 30,
                    RequestsPerHour = 500,
                    RequestsPerDay = 5000,
                    BurstSize = 5,
                    Description = "API write operations rate limit"
                }
            };
        }

        public static Dictionary<string, RateLimitPolicyOptions> GetDefaultUserPolicies()
        {
            return new Dictionary<string, RateLimitPolicyOptions>
            {
                ["default"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 60,
                    RequestsPerHour = 1000,
                    RequestsPerDay = 10000,
                    BurstSize = 10,
                    Description = "Default authenticated user rate limit"
                },
                ["premium"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 120,
                    RequestsPerHour = 2000,
                    RequestsPerDay = 20000,
                    BurstSize = 20,
                    Description = "Premium user rate limit"
                },
                ["admin"] = new RateLimitPolicyOptions
                {
                    RequestsPerMinute = 300,
                    RequestsPerHour = 5000,
                    RequestsPerDay = 50000,
                    BurstSize = 50,
                    Description = "Admin user rate limit"
                }
            };
        }
    }
}