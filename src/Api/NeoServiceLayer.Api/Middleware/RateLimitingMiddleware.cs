using System;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Rate limiting middleware for ASP.NET Core
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _enabled;
        private readonly bool _enableIpWhitelist;
        private readonly string[] _whitelistedIps;
        private readonly string[] _excludedPaths;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            _enabled = configuration.GetValue<bool>("RateLimiting:Enabled", true);
            _enableIpWhitelist = configuration.GetValue<bool>("RateLimiting:EnableIpWhitelist", false);
            _whitelistedIps = configuration.GetSection("RateLimiting:WhitelistedIps").Get<string[]>() ?? Array.Empty<string>();
            _excludedPaths = configuration.GetSection("RateLimiting:ExcludedPaths").Get<string[]>() ?? Array.Empty<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_enabled || ShouldSkipRateLimiting(context))
            {
                await _next(context);
                return;
            }

            var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitingService>();

            // Extract client IP and user ID
            var clientIp = GetClientIpAddress(context);
            var userId = GetUserId(context);
            var endpoint = GetEndpointIdentifier(context);

            // Check if IP is whitelisted
            if (_enableIpWhitelist && IsIpWhitelisted(clientIp))
            {
                await _next(context);
                return;
            }

            // Apply rate limiting
            var result = await ApplyRateLimitingAsync(
                rateLimitService,
                clientIp,
                userId,
                endpoint);

            // Set rate limit headers
            SetRateLimitHeaders(context, result);

            if (!result.IsAllowed)
            {
                await HandleRateLimitExceededAsync(context, result);
                return;
            }

            await _next(context);
        }

        private async Task<RateLimitResult> ApplyRateLimitingAsync(
            IRateLimitingService rateLimitService,
            string clientIp,
            Guid? userId,
            string endpoint)
        {
            try
            {
                // Check if IP or user is blocked
                if (await rateLimitService.IsBlockedAsync($"ip:{clientIp}"))
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        Message = "IP address is temporarily blocked",
                        RetryAfter = 3600 // 1 hour in seconds
                    };
                }

                if (userId.HasValue && await rateLimitService.IsBlockedAsync($"user:{userId}"))
                {
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        Message = "User account is temporarily blocked",
                        RetryAfter = 3600 // 1 hour in seconds
                    };
                }

                // Apply endpoint-specific rate limiting
                var endpointAction = GetEndpointAction(endpoint);
                var serviceResult = await rateLimitService.CheckCombinedRateLimitAsync(
                    clientIp,
                    userId,
                    endpointAction);

                // Convert the result to our local type
                var result = new RateLimitResult
                {
                    IsAllowed = serviceResult.IsAllowed,
                    Limit = serviceResult.Limit,
                    Remaining = serviceResult.Remaining,
                    ResetAt = new DateTimeOffset(serviceResult.ResetsAt),
                    RetryAfter = (int)(serviceResult.RetryAfter / 1000),
                    Message = serviceResult.Message ?? string.Empty
                };

                // Log rate limit violations
                if (!result.IsAllowed)
                {
                    _logger.LogWarning(
                        "Rate limit exceeded for IP {ClientIp}, User {UserId}, Endpoint {Endpoint}",
                        clientIp, userId, endpoint);

                    // Auto-block after repeated violations
                    await CheckAutoBlockAsync(rateLimitService, clientIp, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying rate limiting");

                // On error, allow the request but log the issue
                return new RateLimitResult { IsAllowed = true };
            }
        }

        private async Task CheckAutoBlockAsync(
            IRateLimitingService rateLimitService,
            string clientIp,
            Guid? userId)
        {
            // Check violation count and auto-block if threshold exceeded
            var violationKey = $"violations:{clientIp}";
            var stats = await rateLimitService.GetStatisticsAsync(violationKey);

            if (stats.RequestsLastMinute > 50) // More than 50 violations in a minute
            {
                await rateLimitService.BlockKeyAsync(
                    $"ip:{clientIp}",
                    TimeSpan.FromHours(1),
                    "Excessive rate limit violations");
            }
        }

        private void SetRateLimitHeaders(HttpContext context, RateLimitResult result)
        {
            context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, result.Remaining).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = result.ResetAt.ToUnixTimeSeconds().ToString();

            if (!result.IsAllowed)
            {
                context.Response.Headers["Retry-After"] = result.RetryAfter.ToString();
            }
        }

        private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitResult result)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "rate_limit_exceeded",
                message = result.Message ?? "Too many requests. Please try again later.",
                retryAfter = result.RetryAfter,
                resetsAt = result.ResetsAt
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private bool ShouldSkipRateLimiting(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Skip health checks
            if (path.Contains("/health") || path.Contains("/metrics"))
            {
                return true;
            }

            // Skip excluded paths
            foreach (var excludedPath in _excludedPaths)
            {
                if (path.StartsWith(excludedPath.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIpWhitelisted(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return false;
            }

            foreach (var whitelistedIp in _whitelistedIps)
            {
                if (ipAddress.Equals(whitelistedIp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP (when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            // Check X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private Guid? GetUserId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirst("sub")
                    ?? context.User.FindFirst("user_id");

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }

        private string GetEndpointIdentifier(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Normalize path by removing IDs and query strings
            path = NormalizePath(path);

            return $"{method}:{path}";
        }

        private string NormalizePath(string path)
        {
            // Remove query string
            var questionMarkIndex = path.IndexOf('?');
            if (questionMarkIndex >= 0)
            {
                path = path.Substring(0, questionMarkIndex);
            }

            // Replace GUIDs with placeholder
            path = System.Text.RegularExpressions.Regex.Replace(
                path,
                @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
                "{id}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace numeric IDs with placeholder
            path = System.Text.RegularExpressions.Regex.Replace(
                path,
                @"/\d+",
                "/{id}");

            return path;
        }

        private string GetEndpointAction(string endpoint)
        {
            // Map endpoints to action categories
            if (endpoint.Contains("/auth/login"))
                return "login";
            if (endpoint.Contains("/auth/register"))
                return "register";
            if (endpoint.Contains("/auth/refresh"))
                return "refresh";
            if (endpoint.Contains("/auth/forgot-password"))
                return "password-reset";
            if (endpoint.Contains("/api/") && endpoint.Contains("POST"))
                return "api-write";
            if (endpoint.Contains("/api/") && endpoint.Contains("GET"))
                return "api-read";

            return "global";
        }
    }

    /// <summary>
    /// Extension methods for rate limiting middleware
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}