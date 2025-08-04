using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Api.Middleware;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimiting"));

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var rateLimitOptions = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                // Get client identifier (IP, API key, or user ID)
                var clientId = GetClientIdentifier(context);

                // Get rate limit policy based on endpoint and client
                var policy = GetRateLimitPolicy(context, rateLimitOptions);

                return RateLimitPartition.GetTokenBucketLimiter(
                    clientId,
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = policy.TokenLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = policy.QueueLimit,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds),
                        TokensPerPeriod = policy.TokensPerPeriod,
                        AutoReplenishment = true
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RateLimitingMiddleware>>();
                var clientId = GetClientIdentifier(context.HttpContext);

                logger.LogWarning(
                    "Rate limit exceeded for client {ClientId} on endpoint {Endpoint}",
                    clientId,
                    context.HttpContext.Request.Path);

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.HttpContext.Response.Headers.Add("Retry-After", "60");

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = 60
                });
            };
        });

        // Add per-endpoint rate limiting policies
        services.AddRateLimiter(options =>
        {
            // Key Management endpoints - more restrictive
            options.AddPolicy("KeyManagement", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetClientIdentifier(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 20,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 20,
                        AutoReplenishment = true
                    }));

            // Oracle endpoints - moderate restriction
            options.AddPolicy("Oracle", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetClientIdentifier(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 50,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 50,
                        AutoReplenishment = true
                    }));

            // General API endpoints
            options.AddPolicy("General", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetClientIdentifier(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 100,
                        AutoReplenishment = true
                    }));

            // Health check endpoints - no rate limiting
            options.AddPolicy("HealthCheck", context =>
                RateLimitPartition.GetNoLimiter(GetClientIdentifier(context)));
        });

        return services;
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Priority: API Key > User ID > IP Address

        // Check for API key
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return $"apikey:{apiKey}";
        }

        // Check for authenticated user
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Handle X-Forwarded-For header for proxy scenarios
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim() ?? ipAddress;
        }

        return $"ip:{ipAddress}";
    }

    private static RateLimitPolicy GetRateLimitPolicy(HttpContext context, RateLimitingOptions options)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Check for endpoint-specific policies
        foreach (var policy in options.EndpointPolicies)
        {
            if (path.StartsWith(policy.Key.ToLower()))
            {
                return policy.Value;
            }
        }

        // Check if user has custom rate limits
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tier = context.User.FindFirst("subscription_tier")?.Value;
            if (!string.IsNullOrEmpty(tier) && options.TierPolicies.TryGetValue(tier, out var tierPolicy))
            {
                return tierPolicy;
            }
        }

        // Return default policy
        return options.DefaultPolicy;
    }
}

public class RateLimitingOptions
{
    public RateLimitPolicy DefaultPolicy { get; set; } = new()
    {
        TokenLimit = 100,
        QueueLimit = 20,
        ReplenishmentPeriodSeconds = 60,
        TokensPerPeriod = 100
    };

    public Dictionary<string, RateLimitPolicy> EndpointPolicies { get; set; } = new()
    {
        { "/api/keymanagement", new RateLimitPolicy { TokenLimit = 20, QueueLimit = 5, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 20 } },
        { "/api/oracle", new RateLimitPolicy { TokenLimit = 50, QueueLimit = 10, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 50 } },
        { "/api/compute", new RateLimitPolicy { TokenLimit = 30, QueueLimit = 5, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 30 } }
    };

    public Dictionary<string, RateLimitPolicy> TierPolicies { get; set; } = new()
    {
        { "basic", new RateLimitPolicy { TokenLimit = 100, QueueLimit = 20, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 100 } },
        { "premium", new RateLimitPolicy { TokenLimit = 500, QueueLimit = 50, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 500 } },
        { "enterprise", new RateLimitPolicy { TokenLimit = 2000, QueueLimit = 100, ReplenishmentPeriodSeconds = 60, TokensPerPeriod = 2000 } }
    };
}

public class RateLimitPolicy
{
    public int TokenLimit { get; set; }
    public int QueueLimit { get; set; }
    public int ReplenishmentPeriodSeconds { get; set; }
    public int TokensPerPeriod { get; set; }
}

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add rate limit headers to response
        if (context.Response.Headers.ContainsKey("X-RateLimit-Limit"))
        {
            var limit = context.Response.Headers["X-RateLimit-Limit"];
            var remaining = context.Response.Headers["X-RateLimit-Remaining"];
            var reset = context.Response.Headers["X-RateLimit-Reset"];

            _logger.LogDebug(
                "Rate limit headers - Limit: {Limit}, Remaining: {Remaining}, Reset: {Reset}",
                limit, remaining, reset);
        }

        await _next(context);
    }
}
