using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Api.Extensions
{
    /// <summary>
    /// Extension methods for configuring API rate limiting.
    /// </summary>
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Authentication endpoints - stricter limits
                options.AddPolicy("authentication", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // API endpoints - standard limits
                options.AddPolicy("api", httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 100,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            TokensPerPeriod = 20,
                            AutoReplenishment = true
                        }));

                // Heavy operations - strict limits
                options.AddPolicy("heavy", httpContext =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                        factory: partition => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 2,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        }));

                // Admin endpoints - relaxed limits for authenticated admins
                options.AddPolicy("admin", httpContext =>
                {
                    if (httpContext.User?.IsInRole("Admin") ?? false)
                    {
                        return RateLimitPartition.GetNoLimiter("admin");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // Configure rejection response
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                    }

                    await context.HttpContext.Response.WriteAsync(
                        "Too many requests. Please retry later.",
                        cancellationToken: token);
                };
            });

            return services;
        }
    }
}
